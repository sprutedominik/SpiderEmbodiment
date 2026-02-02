using UnityEngine;

/// <summary>
/// Hält das IK-Target sicher über dem Boden.
/// Läuft NACH FollowPositionOnly, damit dessen Ergebnis ggf. nach oben geklemmt wird.
/// </summary>
[DefaultExecutionOrder(200)]
public class GroundClamp : MonoBehaviour
{
    public enum Mode
    {
        FixedFloorY,       // feste Bodenhöhe (z. B. dein Plane/Ground Transform)
        RaycastToGround    // Boden mittels Raycast ermitteln (für unebene Flächen)
    }

    [Header("Modus")]
    [Tooltip("FixedFloorY = feste Höhe, RaycastToGround = Boden via Raycast suchen")]
    public Mode mode = Mode.FixedFloorY;

    [Header("FixedFloorY")]
    [Tooltip("Referenz-Transform deines Bodens (z. B. 'Ground' oder 'Plane'). Wenn leer, wird fixedFloorY benutzt.")]
    public Transform floorRef;
    [Tooltip("Fallback-Bodenhöhe (nur genutzt, wenn floorRef leer ist)")]
    public float fixedFloorY = 0f;
    [Tooltip("Abstand über dem Boden")]
    public float hover = 0.02f;

    [Header("RaycastToGround")]
    [Tooltip("Welche Layer gelten als Boden (z. B. nur 'Ground')")]
    public LayerMask groundMask = ~0;
    [Tooltip("Um diesen Betrag über der aktuellen Position startet der Abwärtsstrahl")]
    public float rayUp = 0.5f;
    [Tooltip("Maximale Ray-Länge nach unten")]
    public float maxRay = 3f;

    void LateUpdate()
    {
        var pos = transform.position;

        float minY = float.NegativeInfinity;

        if (mode == Mode.FixedFloorY)
        {
            float floorY = floorRef ? floorRef.position.y : fixedFloorY;
            minY = floorY + hover;
        }
        else // RaycastToGround
        {
            Vector3 origin = pos + Vector3.up * rayUp;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRay, groundMask))
                minY = hit.point.y + hover;
        }

        if (!float.IsNegativeInfinity(minY) && pos.y < minY)
        {
            pos.y = minY;
            transform.position = pos;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (mode == Mode.RaycastToGround)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Vector3 origin = transform.position + Vector3.up * rayUp;
            Gizmos.DrawLine(origin, origin + Vector3.down * maxRay);
        }
    }
#endif
}
