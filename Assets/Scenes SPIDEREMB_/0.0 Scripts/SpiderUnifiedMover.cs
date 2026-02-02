using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils; // nur falls du head/camera referenzieren willst (optional)

[RequireComponent(typeof(CharacterController))]
public class SpiderUnifiedMover : MonoBehaviour
{
    [Header("collision / cast")]
    public LayerMask solidMask;          // layer für wände/boden (nicht-trigger)
    [Range(0.0f, 0.2f)] public float skin = 0.03f;

    // „virtuelle“ kapsel, unabhängig vom echten CC (damit kann die spinne breit + flach wirken)
    [Min(0.05f)] public float virtualRadius = 0.6f;
    [Min(0.05f)] public float virtualHeight = 0.6f;

    // substeps, um große deltas aufzuteilen
    [Min(0.02f)] public float maxStep = 0.15f;

    [Header("modes")]
    public bool useInput = false;        // an, wenn du keinen eigenen mover hast
    public bool useProxy = true;         // an, wenn fremd-skripte transform.position setzen

    [Header("input (optional)")]
    public InputActionProperty moveAction;   // z. B. <XRController>{LeftHand}/thumbstick
    public float moveSpeed = 1.6f;           // m/s
    public Transform headOrForward;          // meist XR camera transform für vorwärtsrichtung

    CharacterController cc;
    Vector3 lastPos;
    Quaternion lastRot;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        lastPos = transform.position;
        lastRot = transform.rotation;

        if (useInput && moveAction != null && moveAction.action != null)
            moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (useInput && moveAction != null && moveAction.action != null)
            moveAction.action.Disable();
    }

    void Update()
    {
        if (!useInput) return;

        // einfachen stick-input in welt-delta umrechnen
        Vector2 input = moveAction != null && moveAction.action != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (input.sqrMagnitude > 1e-6f)
        {
            if (headOrForward == null)
            {
                var cam = Camera.main;
                if (cam) headOrForward = cam.transform;
                else headOrForward = transform;
            }

            Vector3 fwd = Vector3.ProjectOnPlane(headOrForward.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(headOrForward.right,  Vector3.up).normalized;

            Vector3 delta = (fwd * input.y + right * input.x) * moveSpeed * Time.deltaTime;
            MoveSafely(delta);
        }
    }

    void LateUpdate()
    {
        if (!useProxy) { lastPos = transform.position; lastRot = transform.rotation; return; }

        // hat irgendein anderes skript die position direkt verändert?
        Vector3 desiredDelta = transform.position - lastPos;
        Quaternion desiredRotDelta = transform.rotation * Quaternion.Inverse(lastRot);

        if (desiredDelta.sqrMagnitude > 1e-8f)
        {
            // zurückrollen und stattdessen sicher bewegen
            transform.position = lastPos;
            MoveSafely(desiredDelta);
        }

        // rotation wieder anwenden (optional: hier könntest du rotation auch limitieren)
        transform.rotation = desiredRotDelta * transform.rotation;

        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    // öffentliche api: nutze das aus deinem eigenen locomotion-code
    public void MoveSafely(Vector3 worldDelta)
    {
        float remaining = worldDelta.magnitude;
        if (remaining < 1e-5f) return;

        Vector3 dir = worldDelta / remaining;

        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, maxStep);
            Vector3 tryMove = dir * step;

            // aktuelle kapsel-mitte (welt) basierend auf dem CC
            Vector3 c = transform.position + cc.center;

            // virtuelle breite/hoehe -> endpunkte der „prüfkapsel“
            float r = Mathf.Max(virtualRadius - skin, 0.001f);
            float half = Mathf.Max(virtualHeight * 0.5f - r, 0f);

            Vector3 p1 = c + Vector3.up * half;
            Vector3 p2 = c - Vector3.up * half;

            // collision-check per capsulecast
            if (Physics.CapsuleCast(p1, p2, r, tryMove.normalized, out var hit,
                                    tryMove.magnitude + skin, solidMask, QueryTriggerInteraction.Ignore))
            {
                // gleiten: bewegung auf die kollisionsfläche projizieren
                Vector3 slide = Vector3.ProjectOnPlane(tryMove, hit.normal);

                // optional: wenn sehr steile normals (über slopeLimit) → blockieren
                // if (Vector3.Angle(hit.normal, Vector3.up) > cc.slopeLimit) slide = Vector3.ProjectOnPlane(slide, hit.normal);

                tryMove = slide;
            }

            cc.Move(tryMove);
            remaining -= step;
        }
    }

    // hilfs-funktion: direktes repositionieren verbieten/ersetzen
    public void TeleportTo(Vector3 worldPosition)
    {
        // harter teleport (kein kollisionscheck) – nutze nur für echte teleports
        // setze CC so, dass die interne kollision korrekt aktualisiert wird
        cc.enabled = false;
        transform.position = worldPosition;
        cc.enabled = true;
        lastPos = transform.position;
        lastRot = transform.rotation;
    }
}
