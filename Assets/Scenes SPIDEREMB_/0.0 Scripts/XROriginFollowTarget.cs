using UnityEngine;

[DefaultExecutionOrder(100)] // nach Character/AI-Updates laufen
public class XROriginFollowTarget : MonoBehaviour
{
    [Header("Ziel (z. B. Spider Root)")]
    public Transform target;

    [Header("Versatz relativ zur Spinne (lokaler Raum)")]
    [Tooltip("x = seitlicher Offset, y wird i. d. R. ignoriert (siehe keepHeadHeight), z negativ = hinter der Spinne")]
    public Vector3 localOffset = new Vector3(0f, 0f, -2.5f);

    [Header("Glättung")]
    [Range(0.1f, 20f)] public float positionLerpSpeed = 6f;
    [Range(0.1f, 20f)] public float rotationLerpSpeed = 4f;

    [Header("Optionen")]
    [Tooltip("Rig-Yaw sanft an die Blickrichtung der Spinne angleichen (nur um Y).")]
    public bool alignYawToTarget = true;

    [Tooltip("Y-Höhe des Kopfes beibehalten, damit es nicht pumpt/bobbt.")]
    public bool keepHeadHeight = true;

    [Tooltip("Wenn Rig zu weit weg ist, sofort versetzen (z. B. nach Teleport). 0 = aus.")]
    public float teleportIfFartherThan = 6f;

    private CharacterController cc;
    private Transform rig;   // XR Origin Transform
    private Transform cam;   // XR Camera (optional, nur für Höhe)

    void Awake()
    {
        rig = transform;
        cc = GetComponent<CharacterController>();
        var foundCam = GetComponentInChildren<Camera>();
        if (foundCam) cam = foundCam.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Zielposition im Welt-Raum: Offset relativ zur Spinnen-Orientierung
        Vector3 desired = target.TransformPoint(localOffset);

        // Höhe stabil halten (nur horizontales Folgen), damit der HMD nicht „bounced“
        if (keepHeadHeight && cam != null)
            desired.y = rig.position.y;

        // Bewegung (bevorzugt über CharacterController)
        Vector3 delta = desired - rig.position;
        float dist = delta.magnitude;

        if (teleportIfFartherThan > 0f && dist > teleportIfFartherThan)
        {
            if (cc) cc.enabled = false;
            rig.position = desired;
            if (cc) cc.enabled = true;
        }
        else
        {
            Vector3 step = Vector3.Lerp(Vector3.zero, delta, Time.deltaTime * positionLerpSpeed);
            if (cc) cc.Move(step);
            else rig.position += step;
        }

        // Nur um Y drehen (Yaw). HMD-Headlook bleibt frei.
        if (alignYawToTarget)
        {
            Vector3 flatFwd = target.forward; flatFwd.y = 0f;
            if (flatFwd.sqrMagnitude > 0.0001f)
            {
                Quaternion targetYaw = Quaternion.LookRotation(flatFwd, Vector3.up);
                rig.rotation = Quaternion.Slerp(rig.rotation, targetYaw, Time.deltaTime * rotationLerpSpeed);
            }
        }
    }
}
