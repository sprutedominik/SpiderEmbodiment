using UnityEngine;

// sehr spät ausführen, damit es Animator/IK überstimmt
[DefaultExecutionOrder(10000)]
public class FreezeRotation : MonoBehaviour
{
    // Bisheriger Modus (vollständig einfrieren)
    public enum SpaceMode { Local, World }
    [Tooltip("Nur für 'Vollständig einfrieren': Lokale Rotation beibehalten oder absolute Weltrotation")]
    public SpaceMode mode = SpaceMode.Local;

    [Header("NEU: Nur Twist/Roll sperren (empfohlen)")]
    [Tooltip("Wenn an, wird NUR die Drehung um die Längsachse des Bones gesperrt/begrenzet.")]
    public bool lockTwistOnly = true;

    public enum Axis { X, Y, Z }
    [Tooltip("Welche LOKALE Achse ist die Längs-/Twist-Achse des Bones? (oft X, manchmal Z)")]
    public Axis twistAxis = Axis.X;

    [Tooltip("Twist vollständig sperren. Wenn aus, darf der Bone innerhalb eines Bereichs rollen.")]
    public bool lockCompletely = true;

    [Range(0f, 180f)]
    [Tooltip("Maximaler Twist relativ zur Startpose (nur wirksam, wenn 'lockCompletely' aus).")]
    public float maxTwistDegrees = 15f;

    // gespeicherte Ausgangswerte
    Quaternion _initialLocalRot;
    Quaternion _initialWorldRot;
    float _initialTwistDeg; // signierter Startwinkel um die Längsachse

    void Awake()
    {
        _initialLocalRot = transform.localRotation;
        _initialWorldRot = transform.rotation;

        // Start-Twist ermitteln (um die gewählte lokale Achse)
        Vector3 axis = AxisVector(twistAxis);
        Quaternion swing0, twist0;
        DecomposeSwingTwist(_initialLocalRot, axis, out swing0, out twist0);
        _initialTwistDeg = SignedTwistDegrees(twist0, axis);
    }

    void LateUpdate()
    {
        if (lockTwistOnly)
        {
            // nur Roll/Twist begrenzen
            Vector3 axis = AxisVector(twistAxis);

            // aktuelle lokale Rotation in Swing * Twist zerlegen
            Quaternion q = transform.localRotation;
            Quaternion swing, twist;
            DecomposeSwingTwist(q, axis, out swing, out twist);

            float currentTwist = SignedTwistDegrees(twist, axis);
            float targetTwist;

            if (lockCompletely)
            {
                targetTwist = _initialTwistDeg; // exakt wie Start
            }
            else
            {
                // um Startwinkel herum begrenzen
                float delta = Mathf.DeltaAngle(currentTwist, _initialTwistDeg);
                delta = Mathf.Clamp(delta, -maxTwistDegrees, maxTwistDegrees);
                targetTwist = _initialTwistDeg + delta;
            }

            Quaternion desiredTwist = Quaternion.AngleAxis(targetTwist, axis);
            transform.localRotation = swing * desiredTwist;
            return;
        }

        // ALTER MODUS: vollständig einfrieren (kompatibel zum alten Verhalten)
        if (mode == SpaceMode.Local)
            transform.localRotation = _initialLocalRot;   // bleibt relativ zum Körper fix
        else
            transform.rotation = _initialWorldRot;        // bleibt absolut fix im Raum
    }

    // ---- Hilfsfunktionen ------------------------------------------------------

    static Vector3 AxisVector(Axis a)
    {
        switch (a)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default:     return Vector3.forward;
        }
    }

    // Zerlegt q = swing * twist, wobei 'axis' in LOKALER Raumrichtung angegeben ist
    static void DecomposeSwingTwist(Quaternion q, Vector3 axis, out Quaternion swing, out Quaternion twist)
    {
        axis = axis.normalized;

        // Projektion des Vektorteils von q auf die Achse -> Twist
        Vector3 qvec = new Vector3(q.x, q.y, q.z);
        Vector3 proj = Vector3.Project(qvec, axis);
        twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
        twist = NormalizeSafe(twist);

        swing = q * Quaternion.Inverse(twist);
    }

    static float SignedTwistDegrees(Quaternion twist, Vector3 axis)
    {
        axis = axis.normalized;
        twist = NormalizeSafe(twist);

        // Winkel (0..180), Vorzeichen über Projektion des Vektorteils
        float angleRad = 2f * Mathf.Acos(Mathf.Clamp(twist.w, -1f, 1f));
        float angleDeg = angleRad * Mathf.Rad2Deg;

        float sign = Mathf.Sign(Vector3.Dot(new Vector3(twist.x, twist.y, twist.z), axis));
        if (sign == 0f) sign = 1f;
        float signed = angleDeg * sign;

        // auf -180..180 normalisieren
        if (signed > 180f) signed -= 360f;
        if (signed < -180f) signed += 360f;
        return signed;
    }

    static Quaternion NormalizeSafe(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
        if (mag > 1e-8f) { float inv = 1f / mag; q.x*=inv; q.y*=inv; q.z*=inv; q.w*=inv; }
        else q = Quaternion.identity;
        return q;
    }
}
