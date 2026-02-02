// Hänge das an dasselbe GO wie dein CharacterController
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SpiderWallBarrier : MonoBehaviour
{
    public float skin = 0.03f;          // kleiner Sicherheitsabstand
    public LayerMask solidMask;         // Wände/Boden
    CharacterController cc;

    void Awake(){ cc = GetComponent<CharacterController>(); }

    public void MoveSafely(Vector3 worldMove)
    {
        if (worldMove.sqrMagnitude < 1e-6f) return;

        // Capsule-Parameter aus CC ableiten
        float r = cc.radius - skin;
        float h = Mathf.Max(cc.height * 0.5f - r, 0f);
        Vector3 c = transform.position + cc.center;
        Vector3 p1 = c + Vector3.up * h;
        Vector3 p2 = c - Vector3.up * h;

        // Kollision prüfen
        if (Physics.CapsuleCast(p1, p2, r, worldMove.normalized, out var hit, worldMove.magnitude + skin, solidMask, QueryTriggerInteraction.Ignore))
        {
            // an Wand entlang gleiten
            Vector3 along = Vector3.ProjectOnPlane(worldMove, hit.normal);
            worldMove = along;
        }

        cc.Move(worldMove);
    }
}
