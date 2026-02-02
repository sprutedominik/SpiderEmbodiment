using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class SpiderController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed     = 2f;
    public float rotationSpeed = 1000f; // Grad pro Sekunde
    public float animDampTime  = 0.1f;

    [Header("References")]
    public Transform cameraTransform; // optional für spätere Features

    private Animator anim;
    private CharacterController cc;

    void Awake()
    {
        anim = GetComponent<Animator>();
        cc   = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) Input sammeln
        float h = Input.GetAxis("Horizontal"); // A/D oder ← →
        float v = Input.GetAxis("Vertical");   // W/S oder ↑ ↓

        // 2) Rotation um Y-Achse steuern (nur über h)
        if (Mathf.Abs(h) > 0.01f)
        {
            float rotation = h * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, rotation, 0f);
        }

        // 3) Bewegung immer nach vorne/rückwärts aus Sicht der Spinne
        Vector3 moveDir = transform.forward * v;
        float inputMag = Mathf.Clamp01(Mathf.Abs(v));

        // 4) Animation
        float targetSpeed = v; // -1 bis +1
        anim.SetFloat("Speed", targetSpeed, animDampTime, Time.deltaTime);

        // 5) Character bewegen
        if (Mathf.Abs(v) > 0.01f)
        {
            cc.Move(moveDir * moveSpeed * Time.deltaTime);
        }
    }
}
