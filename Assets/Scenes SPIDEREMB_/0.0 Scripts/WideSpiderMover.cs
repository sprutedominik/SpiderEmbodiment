using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class WideSpiderMover : MonoBehaviour
{
    public float virtualRadius = 0.6f;   // gewünschte „Breite“ der Spinne (größer als cc.radius)
    public float virtualHeight = 0.5f;   // gewünschte „Höhe“ (flacher als CC)
    public float skin = 0.03f;
    public LayerMask solidMask;

    CharacterController cc;

    void Awake(){ cc = GetComponent<CharacterController>(); }

    // call this instead of cc.Move
    public void MoveWide(Vector3 worldDelta)
    {
        if (worldDelta.sqrMagnitude < 1e-6f) return;

        // aktuelle CC-Mitte in Welt
        Vector3 c = transform.position + cc.center;
        float r = Mathf.Max(virtualRadius - skin, 0.001f);

        // virtuelle Kapsel-Endpunkte (flacher als CC, ausgerichtet an Welt-Y)
        float half = Mathf.Max(virtualHeight * 0.5f - r, 0f);
        Vector3 p1 = c + Vector3.up * half;
        Vector3 p2 = c - Vector3.up * half;

        Vector3 move = worldDelta;
        if (Physics.CapsuleCast(p1, p2, r, move.normalized, out var hit, move.magnitude + skin, solidMask, QueryTriggerInteraction.Ignore))
        {
            // an Oberfläche entlang gleiten
            move = Vector3.ProjectOnPlane(move, hit.normal);
        }

        cc.Move(move);
    }
}
