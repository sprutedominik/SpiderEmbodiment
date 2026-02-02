using UnityEngine;
using Unity.XR.CoreUtils; // nur falls du xrOrigin bequem referenzieren willst (optional)

public class HeadLeashNatural : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;   // Dein XR Origin (Parent von Main Camera)
    public Transform head;       // Main Camera Transform
    public Transform anchor;     // Start-/Sollpunkt im Raum (z. B. bone29)

    [Header("Soft Leash (sanftes Nachführen)")]
    [Tooltip("Ab hier beginnt sanftes Nachführen (nur XZ-Ebene).")]
    public float softRadius = 0.60f;      // m
    [Tooltip("Gefühl der Stärke des Nachziehens (0.5–8). Höher = straffer.")]
    public float spring = 4.0f;           // je höher, desto „strammer“
    [Tooltip("Maximale Nachführ-Geschwindigkeit (m/s), um Ruckler zu vermeiden.")]
    public float maxFollowSpeed = 3.0f;

    [Header("Hard Clamp (Failsafe)")]
    [Tooltip("Spätestens hier wird hart geklemmt/versetzt (kein Passthrough mehr möglich).")]
    public float hardRadius = 0.90f;      // m
    [Tooltip("Harten Clamp sanft einblenden (0 = sofort, 0.05–0.15 = sehr weich).")]
    public float hardClampBlend = 0.08f;

    [Header("Options")]
    [Tooltip("Nur in XZ korrigieren; Y (Höhe) bleibt unangetastet → natürlicher.")]
    public bool horizontalOnly = true;

    Vector3 followVelXZ;  // für SmoothDamp-ähnliche Begrenzung

    void Reset()
    {
        // Auto-Find, falls nicht gesetzt
        if (!xrOrigin) xrOrigin = transform;
        if (!head)
        {
            var xo = GetComponent<XROrigin>();
            if (xo && xo.Camera) head = xo.Camera.transform;
        }
    }

    void LateUpdate()
    {
        if (!xrOrigin || !head || !anchor) return;

        // Offset Kopf -> Anker (wir betrachten primär XZ)
        Vector3 headPos = head.position;
        Vector3 anchorPos = anchor.position;

        Vector3 delta = headPos - anchorPos;
        Vector2 deltaXZ = new Vector2(delta.x, delta.z);
        float d = deltaXZ.magnitude;

        if (d <= softRadius) return; // alles innerhalb der Komfortzone → nichts tun

        // Ziel: Kopf auf den Rand (soft/hard) zurückbringen, indem wir das XR Origin verschieben
        // 1) Soft-Bereich: streckenteil außerhalb softRadius
        float targetRadius = Mathf.Min(d, hardRadius); // im Soft-Fall: zurück bis soft, im Hard-Fall evtl. bis hard
        Vector2 targetXZ = deltaXZ.normalized * softRadius; // Kopf soll maximal am Soft-Rand „liegen“
        Vector2 excessXZ = deltaXZ - targetXZ;              // zu viel Abstand außerhalb soft

        // Sanfte Korrekturgeschwindigkeit (Feder)
        // v = k * excess; begrenzt durch maxFollowSpeed
        Vector2 desiredVelXZ = excessXZ * spring; // m/s
        if (desiredVelXZ.magnitude > maxFollowSpeed)
            desiredVelXZ = desiredVelXZ.normalized * maxFollowSpeed;

        // Integrationsschritt
        Vector2 stepXZ = desiredVelXZ * Time.deltaTime;

        // 2) Hard Clamp (Failsafe): falls d > hardRadius, korrigieren wir ggf. stärker,
        //    aber blenden die harte Korrektur weich ein, um Ruck zu vermeiden.
        if (d > hardRadius)
        {
            float overshoot = d - hardRadius; // wie weit über hard
            float blend = hardClampBlend > 0f ? Mathf.Clamp01(Time.deltaTime / hardClampBlend) : 1f;

            // zusätzliche Korrektur in Richtung Zentrum, proportional zum Overshoot
            Vector2 hardCorr = deltaXZ.normalized * overshoot * blend;
            stepXZ += hardCorr;
        }

        // XR Origin bewegen (nur XZ)
        Vector3 originPos = xrOrigin.position;
        originPos.x -= stepXZ.x;
        originPos.z -= stepXZ.y;
        if (!horizontalOnly)
        {
            // Optional könnte man auch Y leicht gegenkorrigieren (meist nicht nötig).
            // originPos.y += 0f;
        }
        xrOrigin.position = originPos;

        // (Optional) Velocity puffern – hier nutzen wir einfachen Clamping-Ansatz;
        // wenn du echtes SmoothDamp bevorzugst, kann man followVelXZ einbeziehen.
    }
}
