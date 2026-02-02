using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10000)]                 // sehr spät ausführen
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class SpiderControllerGuard : MonoBehaviour
{
    [Header("Bewegungs-Quelle (optional)")]
    [Tooltip("Wenn ein Child (z.B. Body/RootBone) die Ziel-Position setzt, hier zuweisen.")]
    public Transform driver;

    [Header("Physik")]
    public bool useGravity = false;
    public float gravity = 9.81f;
    [Tooltip("Welche Layer sind solide (Wände/Boden)?")]
    public LayerMask solidMask = ~0;

    [Header("Anti-Tunneling / Kanten-Schutz")]
    [Tooltip("Max. Länge eines Unter-Schritts. Große Bewegungen werden in Teilstücke zerlegt.")]
    public float maxSubstepDistance = 0.25f;
    [Tooltip("Verkürzung zur Wand (Sicherheitsabstand).")]
    public float wallPadding = 0.01f;
    [Tooltip("Mikro-Deltas ignorieren, um Jitter zu vermeiden.")]
    public float ignoreDeltaBelow = 0.00002f;

    CharacterController cc;
    Vector3 lastRootPos;
    Vector3 rootToDriver;
    float yVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        lastRootPos = transform.position;
        if (driver) rootToDriver = driver.position - transform.position;

        // sinnvolle Defaults (kannst du im Inspector ändern)
        cc.minMoveDistance = 0f;
        cc.skinWidth = Mathf.Clamp(cc.skinWidth, 0.02f, 0.05f);
    }

    void Update()     { ApplyMove(); }
    void LateUpdate() { ApplyMove(); }

    void ApplyMove()
    {
        // gewünschte Root-Position aus Driver (falls gesetzt) oder eigener Transform
        Vector3 desiredRoot = driver ? (driver.position - rootToDriver) : transform.position;
        Vector3 delta = desiredRoot - lastRootPos;
        if (delta.sqrMagnitude < ignoreDeltaBelow) return;

        // harte Setzung rückgängig machen → wir bewegen kollisionsgeprüft
        transform.position = lastRootPos;

        if (useGravity)
        {
            yVel = cc.isGrounded ? -0.1f : yVel - gravity * Time.deltaTime;
            delta.y += yVel * Time.deltaTime;
        }

        MoveWithWallProtection(delta);
        lastRootPos = transform.position;
    }

    void MoveWithWallProtection(Vector3 totalDelta)
    {
        float remaining = totalDelta.magnitude;
        if (remaining <= 0f) return;

        Vector3 dir = totalDelta / remaining;
        int steps = Mathf.Max(1, Mathf.CeilToInt(remaining / Mathf.Max(0.001f, maxSubstepDistance)));

        Vector3 step = totalDelta / steps;
        for (int i = 0; i < steps; i++)
        {
            // vor jedem Unterschritt Horizontalkollision prüfen/verkürzen/gleiten
            Vector3 stepAdjusted = AdjustForWalls(step);
            cc.Move(stepAdjusted);
        }
    }

    Vector3 AdjustForWalls(Vector3 step)
    {
        // horizontale Wandprüfung (Y separat – oft ist nur XZ relevant)
        Vector3 horiz = new Vector3(step.x, 0f, step.z);
        float dist = horiz.magnitude;
        if (dist <= 1e-6f) return step;

        Vector3 dir = horiz / dist;

        // aktuelle Kapsel-Endpunkte des CC
        GetCapsule(out Vector3 p1, out Vector3 p2, out float radius);

        // Kapsel-Cast nach vorne
        if (Physics.CapsuleCast(p1, p2, Mathf.Max(0.001f, radius * 0.95f), dir, out RaycastHit hit,
                                dist + wallPadding, solidMask, QueryTriggerInteraction.Ignore))
        {
            // bis kurz vor die Wand gehen
            float allowed = Mathf.Max(0f, hit.distance - wallPadding);
            Vector3 slide = Vector3.ProjectOnPlane(horiz, hit.normal); // an Wand entlang
            slide = slide.normalized * Mathf.Max(0f, dist - allowed);

            // Y wieder dazu
            return dir * allowed + new Vector3(0, step.y, 0) + new Vector3(slide.x, 0, slide.z);
        }

        return step; // freie Bahn
    }

    void GetCapsule(out Vector3 p1, out Vector3 p2, out float radius)
    {
        // CC lokal → Welt
        Vector3 center = transform.TransformPoint(cc.center);
        float r = cc.radius;
        float halfH = Mathf.Max(cc.height * 0.5f, r);
        p1 = center + Vector3.up * (halfH - r);  // oben
        p2 = center - Vector3.up * (halfH - r);  // unten
        radius = r;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Vector3 center = transform.TransformPoint(cc.center);
        float r = cc.radius;
        float halfH = Mathf.Max(cc.height * 0.5f, r);
        Gizmos.DrawWireSphere(center + Vector3.up * (halfH - r), r);
        Gizmos.DrawWireSphere(center - Vector3.up * (halfH - r), r);
    }
#endif
}
