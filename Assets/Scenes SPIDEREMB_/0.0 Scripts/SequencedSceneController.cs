using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SequencedSceneController : MonoBehaviour
{
    [Header("Inspector-Zuweisungen")]
    public Button actionButton;              // Welcome, Dummy & Questionnaire
    public TextMeshProUGUI timerText;        // Dummy & Spider

    [Header("Timer (Sekunden)")]
    public float dummyTimerDuration = 5f;
    public float spiderTimerDuration = 90f;

    // ---------------- STATIC STATE ----------------
    private static List<string> buildSceneNames;

    private static bool sequenceBuilt = false;
    private static List<Block> blocksOrdered = new List<Block>();
    private static bool[] blockCompleted;
    private static int currentBlockPos = 0;
    private static int completedCount = 0;

    // Hints
    private const string FINAL_WASHOUT_HINT = "1.8";
    private const string WELCOME_HINT = "welcome"; // case-insensitive

    private float timer;
    private bool timerRunning = false;

    private struct Block
    {
        public int dummy;
        public int spider;
        public int question;
    }

    // ---------------- UTILITIES ----------------

    private static void CacheBuildScenes()
    {
        if (buildSceneNames != null) return;

        buildSceneNames = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            buildSceneNames.Add(Path.GetFileNameWithoutExtension(path));
        }
    }

    private static bool IsDummy(string s)
        => s.IndexOf("dummy", StringComparison.OrdinalIgnoreCase) >= 0
        || s.IndexOf("washout", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsQuestion(string s)
        => s.IndexOf("question", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsSpider(string s)
        => !IsDummy(s) && !IsQuestion(s);

    private static bool IsFinalWashoutName(string s)
        => s.IndexOf(FINAL_WASHOUT_HINT, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsWelcomeName(string s)
        => s.IndexOf(WELCOME_HINT, StringComparison.OrdinalIgnoreCase) >= 0;

    private static int FindFinalWashoutIndex()
    {
        CacheBuildScenes();
        for (int i = 0; i < buildSceneNames.Count; i++)
        {
            if (IsFinalWashoutName(buildSceneNames[i]) && IsDummy(buildSceneNames[i]))
                return i;
        }
        return -1;
    }

    private static void LoadByBuildIndex(int idx)
    {
        if (idx < 0 || idx >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("SequencedSceneController: Target buildIndex out of range.");
            return;
        }

        SceneManager.LoadScene(idx); // robust bei doppelten Scene-Namen
    }

    private static void ResetSequenceState()
    {
        sequenceBuilt = false;
        blocksOrdered.Clear();
        blockCompleted = null;
        currentBlockPos = 0;
        completedCount = 0;
    }

    // ---------------- BLOCK BUILD + RANDOMIZE (ROBUST) ----------------

    private static void BuildAndRandomizeBlocksOnce()
    {
        if (sequenceBuilt) return;

        CacheBuildScenes();

        // Robust: scanne Build-Liste nach Pattern Dummy -> Spider -> Questionnaire
        // Welcome & 1.8 dürfen niemals als Blockteil gezählt werden.
        List<Block> allBlocks = new List<Block>();

        for (int i = 0; i + 2 < buildSceneNames.Count; i++)
        {
            string a = buildSceneNames[i];
            string b = buildSceneNames[i + 1];
            string c = buildSceneNames[i + 2];

            if (IsWelcomeName(a) || IsWelcomeName(b) || IsWelcomeName(c)) continue;
            if (IsFinalWashoutName(a) || IsFinalWashoutName(b) || IsFinalWashoutName(c)) continue;

            if (IsDummy(a) && IsSpider(b) && IsQuestion(c))
            {
                allBlocks.Add(new Block { dummy = i, spider = i + 1, question = i + 2 });
            }
        }

        if (allBlocks.Count < 6)
        {
            Debug.LogError($"SequencedSceneController: Zu wenige Blöcke gefunden ({allBlocks.Count}). Erwartet: 6. Prüfe Build-Order Dummy->Spider->Question.");
        }

        // Wenn aus irgendeinem Grund mehr als 6 gefunden werden (z.B. Duplikate/Lücken),
        // nehmen wir die ersten 6 (dein Design hat exakt 6).
        if (allBlocks.Count > 6)
            allBlocks = allBlocks.GetRange(0, 6);

        // Shuffle
        System.Random rng = new System.Random();
        for (int i = allBlocks.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (allBlocks[i], allBlocks[j]) = (allBlocks[j], allBlocks[i]);
        }

        blocksOrdered.Clear();
        blocksOrdered.AddRange(allBlocks);

        blockCompleted = new bool[blocksOrdered.Count];
        currentBlockPos = 0;
        completedCount = 0;

        sequenceBuilt = true;
    }

    private static int FindBlockPosContainingBuildIndex(int buildIdx)
    {
        if (blocksOrdered == null) return -1;

        for (int k = 0; k < blocksOrdered.Count; k++)
        {
            var b = blocksOrdered[k];
            if (buildIdx == b.dummy || buildIdx == b.spider || buildIdx == b.question)
                return k;
        }
        return -1;
    }

    private static int NextUncompletedBlockPosFrom(int startPos)
    {
        if (blocksOrdered == null || blocksOrdered.Count == 0) return -1;

        for (int k = startPos; k < blocksOrdered.Count; k++)
            if (!blockCompleted[k]) return k;

        return -1;
    }

    // ---------------- UNITY LIFECYCLE ----------------

    void Start()
    {
        CacheBuildScenes();

        string currentName = SceneManager.GetActiveScene().name;

        // Welcome: nur Button, startet Experiment (baut Randomisierung und springt zum ersten random Dummy)
        if (IsWelcomeName(currentName))
        {
            // Falls du nochmal "zurück" in Welcome kommst und neu starten willst:
            ResetSequenceState();

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            if (actionButton != null)
                SetupButton(StartExperimentFromWelcome);
            else
                Debug.LogError("SequencedSceneController: Welcome braucht einen Button (actionButton).");

            return;
        }

        // Dummy: Button + Timer (Button startet Dummy-Timer)
        if (actionButton != null && timerText != null)
        {
            // Fix: keinen "0"-Text anzeigen, bevor Timer startet
            timerText.gameObject.SetActive(false);
            SetupButton(() => StartTimer(dummyTimerDuration));
            return;
        }

        // Spider: Timer only
        if (timerText != null && actionButton == null)
        {
            StartTimer(spiderTimerDuration);
            return;
        }

        // Questionnaire: Button only
        if (actionButton != null && timerText == null)
        {
            SetupButton(LoadNextScene);
            return;
        }

        Debug.LogError("SequencedSceneController: Falsche Inspector-Zuweisung! Dummy: Button+TimerText, Spider: nur TimerText, Frage: nur Button, Welcome: nur Button.");
    }

    void Update()
    {
        if (!timerRunning) return;

        timer -= Time.deltaTime;

        if (timerText != null)
            timerText.text = Mathf.Ceil(timer).ToString();

        if (timer <= 0f)
        {
            timerRunning = false;
            LoadNextScene();
        }
    }

    private void SetupButton(Action onClick)
    {
        actionButton.onClick.RemoveAllListeners();
        actionButton.gameObject.SetActive(true);
        actionButton.interactable = true;

        actionButton.onClick.AddListener(() =>
        {
            actionButton.interactable = false;
            actionButton.gameObject.SetActive(false);
            onClick();
        });
    }

    private void StartTimer(float duration)
    {
        timer = duration;
        timerRunning = true;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = Mathf.Ceil(timer).ToString();
        }
    }

    // ---------------- WELCOME START ----------------

    private void StartExperimentFromWelcome()
    {
        BuildAndRandomizeBlocksOnce();

        if (!sequenceBuilt || blocksOrdered == null || blocksOrdered.Count == 0)
        {
            Debug.LogError("SequencedSceneController: Konnte keine Blöcke bauen – Start abgebrochen.");
            return;
        }

        // starte mit Dummy des ersten randomisierten Blocks
        currentBlockPos = 0;
        LoadByBuildIndex(blocksOrdered[currentBlockPos].dummy);
    }

    // ---------------- CORE SEQUENCING ----------------

    public void LoadNextScene()
    {
        CacheBuildScenes();

        int currentBuildIdx = SceneManager.GetActiveScene().buildIndex;
        string currentName = SceneManager.GetActiveScene().name;

        // Welcome wird ausschließlich über StartExperimentFromWelcome gehandhabt
        if (IsWelcomeName(currentName))
            return;

        // 1.8 ist final
        if (IsFinalWashoutName(currentName))
            return;

        if (!sequenceBuilt)
            BuildAndRandomizeBlocksOnce();

        if (blocksOrdered == null || blocksOrdered.Count == 0)
        {
            Debug.LogError("SequencedSceneController: Keine Blöcke vorhanden.");
            return;
        }

        // Cursor auf Block setzen, in dem wir gerade sind
        int pos = FindBlockPosContainingBuildIndex(currentBuildIdx);
        if (pos >= 0)
            currentBlockPos = pos;

        Block bl = blocksOrdered[currentBlockPos];

        // Dummy -> Spider (nach Dummy-Countdown)
        if (currentBuildIdx == bl.dummy)
        {
            LoadByBuildIndex(bl.spider);
            return;
        }

        // Spider -> Questionnaire (nach Spider-Timer)
        if (currentBuildIdx == bl.spider)
        {
            LoadByBuildIndex(bl.question);
            return;
        }

        // Questionnaire -> nächster Block Dummy (nach Button)
        if (currentBuildIdx == bl.question)
        {
            if (!blockCompleted[currentBlockPos])
            {
                blockCompleted[currentBlockPos] = true;
                completedCount++;
            }

            // alle 6 Blöcke fertig -> 1.8
            if (completedCount >= blocksOrdered.Count)
            {
                int endIdx = FindFinalWashoutIndex();
                if (endIdx >= 0)
                    LoadByBuildIndex(endIdx);
                else
                    Debug.LogError("SequencedSceneController: Final Washout (1.8) nicht gefunden oder nicht als Dummy/Washout erkannt.");
                return;
            }

            // nächsten uncompleted Block in random Reihenfolge
            int nextPos = NextUncompletedBlockPosFrom(currentBlockPos + 1);
            if (nextPos < 0)
                nextPos = NextUncompletedBlockPosFrom(0);

            if (nextPos < 0)
            {
                Debug.LogError("SequencedSceneController: Kein nächster Block gefunden, obwohl noch nicht alle abgeschlossen.");
                return;
            }

            currentBlockPos = nextPos;
            LoadByBuildIndex(blocksOrdered[currentBlockPos].dummy);
            return;
        }

        Debug.LogError("SequencedSceneController: Unklarer Szenenzustand (Scene gehört zu keinem Block oder falsches Pattern).");
    }
}
