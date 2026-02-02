using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SteuerungsskriptDummy : MonoBehaviour
{
    [Header("Bewegung (linker Stick)")]
    [Tooltip("Max. Laufgeschwindigkeit in m/s.")]
    public float moveSpeed = 2.5f;

    [Tooltip("Wie schnell der Dummy in die Bewegungsrichtung dreht (0 = keine automatische Drehung).")]
    public float turnSpeed = 12f; // 0 => keine Drehung

    [Tooltip("Optional: Animationsparameter setzen (Speed, Horizontal, Vertical).")]
    public Animator animator; // leer = ignorieren

    [Header("Gravity")]
    [Tooltip("Schwerkraft (negativ).")]
    public float gravity = -9.81f;

    [Tooltip("Kleiner Down-Push am Boden, damit der Controller stabil grounded bleibt.")]
    public float groundedDownforce = -2f;

    [Header("Input (optional zuweisen)")]
    [Tooltip("Move-Action (Vector2) vom linken Controller. Wenn leer, wird zur Laufzeit automatisch eine Action erstellt.")]
    public InputActionReference moveAction;

    // ---- intern ----
    private CharacterController _cc;
    private Vector3 _velocity;       // nur y-Komponente relevant (Gravity)
    private InputAction _runtimeMove; // falls wir selbst eine Action bauen

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        EnsureMoveActionEnabled(true);
    }

    void OnDisable()
    {
        EnsureMoveActionEnabled(false);
    }

    void Update()
    {
        // 1) Input lesen (Vector2 vom linken Stick)
        var act = GetMoveAction();
        Vector2 in2 = act != null ? act.ReadValue<Vector2>() : Vector2.zero;

        // 2) Bewegungsrichtung in Weltkoordinaten
        Vector3 moveDir = new Vector3(in2.x, 0f, in2.y);
        if (moveDir.sqrMagnitude > 1e-4f)
            moveDir.Normalize();

        // 3) automatische Körperdrehung (nur Y) in Bewegungsrichtung
        if (turnSpeed > 0f && moveDir.sqrMagnitude > 0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRot, 
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime)
            );
        }

        // 4) Planare Bewegung
        Vector3 worldMove = moveDir * moveSpeed;

        // 5) Gravity & Grounding
        bool grounded = _cc.isGrounded;
        if (grounded && _velocity.y < 0f)
            _velocity.y = groundedDownforce;

        _velocity.y += gravity * Time.deltaTime;

        // 6) Anwenden
        Vector3 delta = (worldMove * Time.deltaTime) + new Vector3(0f, _velocity.y * Time.deltaTime, 0f);
        _cc.Move(delta);

        // 7) Animator-Parameter (optional)
        if (animator)
        {
            Vector3 localVel = transform.InverseTransformDirection(worldMove);
            float speed01 = Mathf.Clamp01(localVel.magnitude / Mathf.Max(0.0001f, moveSpeed));
            animator.SetFloat("Speed", speed01);
            animator.SetFloat("Horizontal", localVel.x / Mathf.Max(0.0001f, moveSpeed));
            animator.SetFloat("Vertical",   localVel.z / Mathf.Max(0.0001f, moveSpeed));
            animator.SetBool("Grounded", grounded);
        }
    }

    // --------- Helpers ---------

    private InputAction GetMoveAction()
    {
        if (moveAction != null && moveAction.action != null)
            return moveAction.action;

        return _runtimeMove;
    }

    private void EnsureMoveActionEnabled(bool enable)
    {
        if (moveAction != null && moveAction.action != null)
        {
            if (enable) moveAction.action.Enable();
            else moveAction.action.Disable();
            return;
        }

        if (_runtimeMove == null)
        {
            _runtimeMove = new InputAction("AutoMove", InputActionType.Value);
            try { _runtimeMove.expectedControlType = "Vector2"; } catch { }

            // generische OpenXR-Bindings – funktionieren auf Quest 3/3s
            _runtimeMove.AddBinding("<XRController>{LeftHand}/thumbstick");
            _runtimeMove.AddBinding("<XRController>{LeftHand}/primary2DAxis");
        }

        if (enable && !_runtimeMove.enabled) _runtimeMove.Enable();
        if (!enable && _runtimeMove.enabled) _runtimeMove.Disable();
    }
}
