using UnityEngine;

[AddComponentMenu("Debug/Simple XR Camera Debugger")]
public class SimpleXRCameraDebugger : MonoBehaviour
{
    [Header("Referenzen (Inspector)")]
    public Transform xrOrigin;       // Dein XROrigin-Objekt
    public Transform cameraOffset;   // Child „Camera Offset“
    public Transform mainCamera;     // Main Camera (Transform)

    [Header("Logging")]
    public bool logPosition = true;
    public bool logRotation = false;
    public float logInterval = 0f;   // Sekunde(n) zwischen Logs (0 = jeden Frame)
    private float _nextLogTime = 0f;

    void Update()
    {
        if (Time.time < _nextLogTime) return;
        _nextLogTime = Time.time + logInterval;

        var sb = new System.Text.StringBuilder();
        sb.Append($"[XRDbg {Time.time:F2}s] ");

        if (logPosition)
            sb.Append($"pos: Origin={xrOrigin.position:F3}, Offset={cameraOffset.position:F3}, Cam={mainCamera.position:F3} ");

        if (logRotation)
            sb.Append($"rot: Origin={xrOrigin.eulerAngles:F1}, Offset={cameraOffset.eulerAngles:F1}, Cam={mainCamera.eulerAngles:F1}");

        Debug.Log(sb.ToString());
    }
}
