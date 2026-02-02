using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeadRotationLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    public string fileName = "HeadRotationData.csv";
    public float sampleRate = 90f; // Quest 3S frame rate

    [Header("Optional Gaze Target")]
    public Transform gazeTarget;

    private Transform head;
    private float timer;
    private DateTime startTime;
    private string sceneName;

    private Quaternion lastRot;
    private Vector3 lastPos;

    private float yawTotal;
    private float pitchTotal;
    private float movementTotal;
    private float gazeMs;

    void Start()
    {
        head = Camera.main.transform;
        sceneName = SceneManager.GetActiveScene().name;
        startTime = DateTime.Now;

        string path = Path.Combine(Application.persistentDataPath, fileName);

        // ✅ Expected header (ONE new column at the end)
        string expectedHeader =
            "Date;Time;Scene;Duration(s);YawTotal(deg);PitchTotal(deg);Movement(m);Samples;GazeTime(ms);SceneDescription";

        // ✅ Ensure header is correct even if file already exists
        if (!File.Exists(path))
        {
            File.WriteAllText(path, expectedHeader + "\n");
        }
        else
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                File.WriteAllText(path, expectedHeader + "\n");
            }
            else if (lines[0].Trim() != expectedHeader)
            {
                // Upgrade existing file safely:
                // - Replace header
                // - Pad old rows to match new column count (avoid shifting in Numbers/Excel)

                string tempPath = path + ".tmp";
                using (var writer = new StreamWriter(tempPath, false))
                {
                    writer.WriteLine(expectedHeader);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].TrimEnd();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        int colCount = line.Split(';').Length;

                        // Old format had 9 columns, new has 10 -> add empty field at the end
                        if (colCount == 9)
                            line += ";";

                        writer.WriteLine(line);
                    }
                }

                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
            }
        }

        lastRot = head.rotation;
        lastPos = head.position;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f / sampleRate)
        {
            timer = 0f;
            LogFrame();
        }
    }

    void LogFrame()
    {
        Vector3 pos = head.position;
        movementTotal += Vector3.Distance(pos, lastPos);
        lastPos = pos;

        Vector3 currEuler = head.rotation.eulerAngles;
        Vector3 lastEuler = lastRot.eulerAngles;

        yawTotal += Mathf.Abs(Mathf.DeltaAngle(lastEuler.y, currEuler.y));
        pitchTotal += Mathf.Abs(Mathf.DeltaAngle(lastEuler.x, currEuler.x));

        lastRot = head.rotation;

        if (gazeTarget != null)
        {
            if (Physics.Raycast(head.position, head.forward, out RaycastHit hit, 100f))
            {
                if (hit.transform == gazeTarget)
                    gazeMs += Time.deltaTime * 1000f;
            }
        }
    }

    void OnApplicationQuit() => Save();
    void OnDisable() => Save();

    void Save()
    {
        float duration = (float)(DateTime.Now - startTime).TotalSeconds;
        int samples = Mathf.RoundToInt(duration * sampleRate);

        string sceneDescription = GetSceneDescription(sceneName);

        string line = string.Join(";",
            startTime.ToString("yyyy-MM-dd"),
            startTime.ToString("HH:mm:ss"),
            sceneName,
            duration.ToString("F2"),
            yawTotal.ToString("F2"),
            pitchTotal.ToString("F2"),
            movementTotal.ToString("F4"),
            samples.ToString(),
            gazeMs.ToString("F0"),
            sceneDescription
        );

        File.AppendAllText(
            Path.Combine(Application.persistentDataPath, fileName),
            line + "\n"
        );

        Debug.Log("[VR LOGGER] ✅ Saved clean CSV entry.");
    }

    string GetSceneDescription(string scene)
    {
        if (scene.StartsWith("0.1"))
            return "Spider – Labyrinth – Third-Person Perspective";
        if (scene.StartsWith("0.4"))
            return "Spider – Labyrinth – First-Person Perspective";
        if (scene.StartsWith("0.7"))
            return "Spider – Mirror – Third-Person Perspective";
        if (scene.StartsWith("1.0"))
            return "Spider – Mirror – First-Person Perspective";
        if (scene.StartsWith("1.3"))
            return "Spider – LightTarget – Third-Person Perspective";
        if (scene.StartsWith("1.6"))
            return "Spider – LightTarget – First-Person Perspective";

        return "";
    }
}
