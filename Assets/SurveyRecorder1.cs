using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SurveyRecorder1 : MonoBehaviour
{
    public Slider embodimentSlider;    // 0–100
    public Slider fearSlider;          // 0–100
    public Slider presenceSlider;      // 0–100
    public Button nextButton;          // Next-Button in Questionnaire-Szenen

    private string folderPath;
    private string filePath;

    private const string OLD_HEADER =
        "Date,TimestampUTC,RatedScene,Embodiment,Fear,Presence,QuestionnaireScene";

    private const string NEW_HEADER =
        "Date,TimestampUTC,RatedScene,Embodiment,Fear,Presence,QuestionnaireScene,SpiderSceneInfo";

    private bool hasLoggedThisScene = false;

    void Awake()
    {
        folderPath = Path.Combine(Application.persistentDataPath, "StudyVRResults");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        filePath = Path.Combine(folderPath, "survey_results.csv");

        TryMigrateCsvHeaderInPlace();
    }

    void OnEnable() => AttachListenerSafely();
    void Start() => AttachListenerSafely();

    void LateUpdate()
    {
        AttachListenerSafely();
    }

    private void AttachListenerSafely()
    {
        if (!IsQuestionnaireScene()) return;
        if (nextButton == null) return;

        nextButton.onClick.RemoveListener(RecordNow);
        nextButton.onClick.AddListener(RecordNow);
    }

    public void RecordNow()
    {
        if (hasLoggedThisScene) return;
        if (!IsQuestionnaireScene()) return;

        if (embodimentSlider == null || fearSlider == null || presenceSlider == null)
        {
            Debug.LogError($"[SurveyRecorder] Missing slider references in scene '{SceneManager.GetActiveScene().name}'.");
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        string questionnaireSceneName = activeScene.name;
        int questionnaireBuildIndex = activeScene.buildIndex;

        string ratedSceneName = "";
        int ratedBuildIndex = questionnaireBuildIndex - 1;

        if (ratedBuildIndex >= 0)
        {
            string ratedPath = SceneUtility.GetScenePathByBuildIndex(ratedBuildIndex);
            ratedSceneName = Path.GetFileNameWithoutExtension(ratedPath);
        }

        string spiderSceneInfo = GetSpiderSceneInfo(ratedSceneName);

        float embodiment = embodimentSlider.value;
        float fear = fearSlider.value;
        float presence = presenceSlider.value;

        string date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        string timestampUtc = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        bool writeHeader = !File.Exists(filePath);

        using (var writer = new StreamWriter(filePath, true))
        {
            if (writeHeader)
                writer.WriteLine(NEW_HEADER);

            writer.WriteLine(string.Join(",",
                date,
                timestampUtc,
                CsvEscape(ratedSceneName),
                embodiment.ToString(CultureInfo.InvariantCulture),
                fear.ToString(CultureInfo.InvariantCulture),
                presence.ToString(CultureInfo.InvariantCulture),
                CsvEscape(questionnaireSceneName),
                CsvEscape(spiderSceneInfo)
            ));
        }

        hasLoggedThisScene = true;
        Debug.Log($"[SurveyRecorder] Logged: {ratedSceneName} -> {spiderSceneInfo}");
    }

    private string GetSpiderSceneInfo(string ratedSceneName)
    {
        if (string.IsNullOrEmpty(ratedSceneName)) return "Unknown|Unknown";

        string s = ratedSceneName.ToLower();

        if (s.Contains("0.1")) return "Labyrinth|3PP";
        if (s.Contains("0.4")) return "Labyrinth|1PP";
        if (s.Contains("0.7")) return "Mirror|3PP";
        if (s.Contains("1.0")) return "Mirror|1PP";
        if (s.Contains("1.3")) return "LightTarget|3PP";
        if (s.Contains("1.6")) return "LightTarget|1PP";

        return "Unknown|Unknown";
    }

    private bool IsQuestionnaireScene()
    {
        return SceneManager.GetActiveScene().name.ToLower().Contains("question");
    }

    static string CsvEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return (s.Contains(",") || s.Contains("\""))
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
    }

    // ✅ FIX: migriert nur "saubere" alte Zeilen (7 Spalten, Zahlenfelder sind wirklich Zahlen)
    // und überspringt kaputte "verschobene" Zeilen aus der Zwischenphase.
    private void TryMigrateCsvHeaderInPlace()
    {
        if (!File.Exists(filePath)) return;

        string[] lines;
        try { lines = File.ReadAllLines(filePath); }
        catch { return; }

        if (lines.Length == 0) return;

        string header = lines[0].Trim();

        if (header == NEW_HEADER) return;
        if (header != OLD_HEADER) return;

        var tmpPath = filePath + ".tmp";

        using (var writer = new StreamWriter(tmpPath, false))
        {
            writer.WriteLine(NEW_HEADER);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');

                // Wir migrieren NUR Zeilen, die exakt dem alten Format entsprechen:
                // 7 Spalten: Date,TimestampUTC,RatedScene,Embodiment,Fear,Presence,QuestionnaireScene
                if (parts.Length != 7)
                {
                    // Kaputte Zeilen (z.B. 9 Spalten oder verschoben) überspringen
                    continue;
                }

                // Validierung: Embodiment/Fear/Presence müssen Zahlen sein
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
                    !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
                    !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    // Wenn da Text wie "Labyrinth" drin steht -> kaputt -> skip
                    continue;
                }

                string rated = parts[2].Trim().Trim('"');
                string info = GetSpiderSceneInfo(rated);

                writer.WriteLine(line + "," + CsvEscape(info));
            }
        }

        try
        {
            File.Delete(filePath);
            File.Move(tmpPath, filePath);
            Debug.Log("[SurveyRecorder] Migrated CSV to include SpiderSceneInfo and removed malformed rows.");
        }
        catch
        {
            // nichts crashen
        }
    }
}
