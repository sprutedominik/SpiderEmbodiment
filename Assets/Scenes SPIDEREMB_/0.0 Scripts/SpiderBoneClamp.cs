using UnityEngine;

/// <summary>
/// Spider bone clamp:
/// - hält den Bone oberhalb der Körperebene (gegen body.up)
/// - sperrt (oder begrenzt) Twist/Roll um die Segmentachse
/// Setze es auf den PROXIMALEN Bone (Schulter/Oberschenkel) eines Beins.
/// </summary>
[DisallowMultipleComponent]
public class SpiderBoneClamp : MonoBehaviour
{
    public enum Axis { X, Y, Z, Custom }

    [Header("References")]
    [Tooltip("Root des Körpers; dessen Up definiert die Körper-Ebene.")]
    public Transform bodyReference;
    [Tooltip("Nächster Bone im Bein (Child) – wird genutzt, um die Segmentrichtung zu bestimmen.")]
    public Transform childHint;

    [Header("Welche lokale Achse des Bones ist 'Up-like' (für Twist-Berechnung)?")]
    public Axis twistRefLocalAxis = Axis.Y;
    public Vector3 customTwistRefLocalAxis = Vector3.up;

    [Header("Elevation-Klammer gegen body.up")]
    [Tooltip("Wie viele Grad oberhalb der Körper-Ebene MUSS der Bone mindestens bleiben?")]
    [Range(0f, 30f)] public float minElevationAboveBodyDeg = 5f;

    [Header("Twist/Roll um Segmentachse")]
    [Tooltip("Wenn true: Twist komplett auf 0 festnageln (kein Rollen des Ellbogens).")]
    public bool lockTwistCompletely = true;
    [Tooltip("Falls nicht komplett gelockt: max. Twist-Absatz in Grad (+/-).")]
    [Range(0f, 180f)] public float maxTwistDeg = 35f;

    [Header("Glätten (optional)")]
    [Range(0f, 1f)] public float slerp = 0f;   // 0 = sofort, 0.15–0.25 = leicht geglättet

    [Header("Debug")]
    public bool drawGizmos = false;
    public float gizmoLen = 0.06f;

    void LateUpdate()
    {
        if (!bodyReference || !childHint) return;

        // aktuelle Weltrotation NACH deinem Mapping/IK
        Quaternion worldRot = transform.rotation;

        // Segmentrichtung (vom aktuellen Bone zum nächsten Bone)
        Vector3 segDir = childHint.position - transform.position;
        if (segDir.sqrMagnitude < 1e-8f) return;
        segDir.Normalize();

        // 1) Elevation gegen body.up clampen (nicht unter den Körper kippen)
        Vector3 bodyUp = bodyReference.up.normalized;
        float angleToUp = Vector3.Angle(segDir, bodyUp);
        float maxAllowed = 90f - minElevationAboveBodyDeg; // knapp über Ebene bleiben
        if (angleToUp > maxAllowed)
        {
            // drehe segDir um die Achse, die segDir in Richtung bodyUp kippt
            Vector3 axis = Vector3.Cross(segDir, bodyUp);
            if (axis.sqrMagnitude > 1e-10f)
            {
                axis.Normalize();
                float delta = angleToUp - maxAllowed;
                worldRot = Quaternion.AngleAxis(delta, axis) * worldRot;
                segDir = Quaternion.AngleAxis(delta, axis) * segDir;
            }
        }

        // 2) Twist/roll um die Segmentachse fixieren/begrenzen
        // aktueller „Up-like“-Vektor des Bones:
        Vector3 boneUpLikeWorld = worldRot * GetLocalAxis(twistRefLocalAxis, customTwistRefLocalAxis);

        // gewünschte Up-Referenz: radial vom Körper nach außen
        Vector3 radialOut = (transform.position - bodyReference.position);
        if (radialOut.sqrMagnitude < 1e-8f) radialOut = bodyUp; // Fallback
        radialOut.Normalize();

        // Beide auf Ebene orthogonal zu segDir projizieren
        Vector3 desiredUpOnPlane = Vector3.ProjectOnPlane(radialOut, segDir);
        Vector3 upOnPlane        = Vector3.ProjectOnPlane(boneUpLikeWorld, segDir);

        if (desiredUpOnPlane.sqrMagnitude < 1e-10f)
            desiredUpOnPlane = Vector3.ProjectOnPlane(bodyUp, segDir);
        if (upOnPlane.sqrMagnitude < 1e-10f)
            upOnPlane = desiredUpOnPlane;

        desiredUpOnPlane.Normalize();
        upOnPlane.Normalize();

        float signed = Vector3.SignedAngle(upOnPlane, desiredUpOnPlane, segDir);
        float deltaTwist = lockTwistCompletely
            ? signed                                  // bringe exakt auf Referenz
            : Mathf.Clamp(signed, -maxTwistDeg, maxTwistDeg);

        if (Mathf.Abs(deltaTwist) > 1e-3f)
            worldRot = Quaternion.AngleAxis(deltaTwist, segDir) * worldRot;

        // 3) stabil in localRotation zurückschreiben (hierarchie-sicher)
        if (transform.parent)
        {
            Quaternion targetLocal = Quaternion.Inverse(transform.parent.rotation) * worldRot;
            if (slerp > 0f)
            {
                float t = 1f - Mathf.Pow(1f - slerp, Time.deltaTime * 60f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocal, t);
            }
            else transform.localRotation = targetLocal;
        }
        else
        {
            transform.rotation = worldRot;
        }
    }

    static Vector3 GetLocalAxis(Axis a, Vector3 custom)
    {
        switch (a)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            case Axis.Z: return Vector3.forward;
            case Axis.Custom: return custom;
            default: return Vector3.up;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !childHint) return;
        Gizmos.color = Color.cyan;
        Vector3 segDir = (childHint.position - transform.position).normalized;
        Gizmos.DrawLine(transform.position, transform.position + segDir * gizmoLen);
    }
}
