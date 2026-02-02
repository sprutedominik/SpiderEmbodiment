using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// Minimaler, build-sicherer Logger für alle Bones unter skeletonRoot.
/// Schreibt pro Frame Zeit, Position (px,py,pz) und Rotation (qx,qy,qz,qw) in eine CSV.
/// Datei: Application.persistentDataPath/Logs/BoneLog_<timestamp>.csv
[AddComponentMenu("Spider/Spider Bone Logger")]
[DisallowMultipleComponent]
public class SpiderBoneLogger : MonoBehaviour
{
    [Tooltip("Root deines Skeletts – alle Kinder werden geloggt.")]
    public Transform skeletonRoot;

    [Tooltip("Zusätzliche Marker (optional), z. B. Controller-Anker.")]
    public Transform[] extraMarkers;

    [Tooltip("WorldSpace (true) oder LocalSpace (false) loggen.")]
    public bool recordWorldSpace = true;

    [Tooltip("Automatisch stoppen nach X Sekunden (0 = nie).")]
    public float autoStopAfterSeconds = 0f;

    // NICHT serialisierte Felder (keine Layout-Differenzen)
    private StreamWriter _writer;
    private List<Transform> _targets = new List<Transform>();
    private List<string> _names = new List<string>();
    private float _t0;
    private float _lastFlush;
    private CultureInfo _inv = CultureInfo.InvariantCulture;
    private string _filePath;

    // Flush alle N Frames (konstante, nicht-serialisierte Einstellung)
    const int FLUSH_EVERY_N_FRAMES = 60;
    int _frameCounter = 0;

    void Start()
    {
        if (skeletonRoot == null)
        {
            Debug.LogError("[SpiderBoneLogger] Bitte 'skeletonRoot' zuweisen.");
            enabled = false;
            return;
        }

        // Bones sammeln
        _targets.Clear();
        _targets.AddRange(skeletonRoot.GetComponentsInChildren<Transform>(true));

        // Namen als Pfade (eindeutig)
        _names.Clear();
        foreach (var t in _targets) _names.Add(GetRelativePath(t, skeletonRoot));

        // Extra Marker anhängen
        if (extraMarkers != null)
        {
            foreach (var m in extraMarkers)
            {
                if (m == null) continue;
                _targets.Add(m);
                _names.Add("EXTRA/" + m.name);
            }
        }

        // Datei
        string dir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(dir);
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _filePath = Path.Combine(dir, $"BoneLog_{ts}.csv");
        _writer = new StreamWriter(_filePath, false) { NewLine = "\n" };

        // Header
        var cols = new List<string> { "time" };
        foreach (var n in _names)
        {
            string b = Sanitize(n);
            cols.Add(b + ".px"); cols.Add(b + ".py"); cols.Add(b + ".pz");
            cols.Add(b + ".qx"); cols.Add(b + ".qy"); cols.Add(b + ".qz"); cols.Add(b + ".qw");
        }
        _writer.WriteLine(string.Join(",", cols));

        _t0 = Time.time;
        _lastFlush = _t0;

        Debug.Log("[SpiderBoneLogger] Logging to: " + _filePath);
    }

    void Update()
    {
        if (_writer == null) return;

        float t = Time.time - _t0;
        if (autoStopAfterSeconds > 0f && t >= autoStopAfterSeconds)
        {
            StopRecording();
            return;
        }

        // Sample
        var values = new List<string>(1 + _targets.Count * 7);
        values.Add(t.ToString(_inv));

        foreach (var tr in _targets)
        {
            Vector3 p = recordWorldSpace ? tr.position : tr.localPosition;
            Quaternion q = recordWorldSpace ? tr.rotation : tr.localRotation;

            values.Add(p.x.ToString(_inv));
            values.Add(p.y.ToString(_inv));
            values.Add(p.z.ToString(_inv));
            values.Add(q.x.ToString(_inv));
            values.Add(q.y.ToString(_inv));
            values.Add(q.z.ToString(_inv));
            values.Add(q.w.ToString(_inv));
        }

        _writer.WriteLine(string.Join(",", values));

        // periodisch flushen (nicht serialisiert, daher build-sicher)
        _frameCounter++;
        if (_frameCounter % FLUSH_EVERY_N_FRAMES == 0)
        {
            _writer.Flush();
            _lastFlush = Time.time;
        }
    }

    void OnDestroy()         { StopRecording(); }
    void OnApplicationQuit() { StopRecording(); }

    void StopRecording()
    {
        if (_writer != null)
        {
            try { _writer.Flush(); _writer.Close(); } catch {}
            _writer = null;
            Debug.Log("[SpiderBoneLogger] Stopped. File: " + _filePath);
        }
    }

    static string GetRelativePath(Transform t, Transform root)
    {
        if (t == null) return "NULL";
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null && cur != root)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        if (cur == root) stack.Push(root.name);
        return string.Join("/", stack.ToArray());
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "NA";
        return s.Replace(",", "_").Replace("\n", " ").Replace("\r", " ");
    }
}
