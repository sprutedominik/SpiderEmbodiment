using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(CharacterController))]
public class XRSpiderMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Grundgeschwindigkeit in m/s")]
    public float moveSpeed = 2f;
    [Tooltip("Geschwindigkeits-Faktor rückwärts")]
    public float backwardSpeedFactor = 0.8f;

    [Header("Rotation Settings")]
    [Tooltip("Rotationsgeschwindigkeit in Grad/Sekunde")]
    public float rotationSpeed = 200f;

    [Header("Gravity Settings")]
    [Tooltip("Schwerkraft-Beschleunigung")]
    public float gravity = -9.81f;

    [Header("Input Actions")]
    [Tooltip("Zieh hier deine XRI Default Input Actions → LeftHand/Move rein")]
    public InputActionReference moveAction;

    private CharacterController cc;
    private float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        moveAction?.action.Enable();
    }

    void OnDisable()
    {
        moveAction?.action.Disable();
    }

    void Update()
    {
        // 1) Joystick auslesen
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // 2) Rotation: Stick X rotiert um Y-Achse
        if (Mathf.Abs(input.x) > 0.01f)
        {
            float yRot = input.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, yRot, 0f, Space.Self);
        }

        // 3) Vor-/Rückwärts-Bewegung: Stick Y bewegt vorwärts/rückwärts
        float speedFactor = input.y < 0f ? backwardSpeedFactor : 1f;
        Vector3 move = transform.forward * input.y * moveSpeed * speedFactor;

        // 4) Schwerkraft
        if (cc.isGrounded)
            verticalVelocity = -0.5f;
        else
            verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        // 5) Bewegung anwenden
        cc.Move(move * Time.deltaTime);
    }
}