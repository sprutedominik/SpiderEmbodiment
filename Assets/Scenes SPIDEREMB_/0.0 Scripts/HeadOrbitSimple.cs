using UnityEngine;

public class HeadOrbitSimple : MonoBehaviour
{
    public Transform xrCamera;          // HMD-Kamera (unter XR Origin / Camera Offset)
    [Range(0f,1f)] public float factor = 0.2f; // Anteil der Kopf-Yaw, der in Orbit umgesetzt wird
    public float maxDegPerSec = 45f;    // Sicherheitslimit (Komfort)
    public float deadZoneDeg = 0.2f;    // kleine Kopfzuckungen ignorieren
    public bool invert = true;          // true = "nach links schauen" -> Kamera bewegt sich nach rechts

    float lastYaw;

    void Start() { if (xrCamera) lastYaw = xrCamera.localEulerAngles.y; }

    void Update()
    {
        if (!xrCamera) return;

        float yaw = xrCamera.localEulerAngles.y;                // Kopf-Yaw relativ zum Rig
        float deltaYaw = Mathf.DeltaAngle(lastYaw, yaw);

        if (Mathf.Abs(deltaYaw) < deadZoneDeg) { lastYaw = yaw; return; }

        float step = deltaYaw * factor * (invert ? -1f : 1f);   // ggf. Richtung umkehren
        float maxStep = maxDegPerSec * Time.deltaTime;          // glätten/limitieren
        step = Mathf.Clamp(step, -maxStep, maxStep);

        transform.Rotate(0f, step, 0f, Space.World);            // Pivot um Spinnenzentrum drehen
        lastYaw = yaw;
    }
}
