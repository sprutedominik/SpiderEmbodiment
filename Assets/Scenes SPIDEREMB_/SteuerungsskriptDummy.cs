using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SteuerungsskriptDummy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float turnSpeed = 360f;

    [Header("Head Bobbing")]
    public Transform headBone; // Zuweisen im Inspector, z. B. B-head
    public float headBobAmount = 5f;      // Maximaler Winkel in Grad
    public float headBobSpeed = 8f;       // Geschwindigkeit der Bobbewegung

    private CharacterController cc;
    private Animator animator;
    private float headBobTimer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // ————— INPUT —————
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) h =  1f;
        if (Input.GetKey(KeyCode.UpArrow))    v =  1f;
        if (Input.GetKey(KeyCode.DownArrow))  v = -1f;

        // ————— ROTATION —————
        if (Mathf.Abs(h) > 0.01f)
        {
            float rotation = h * turnSpeed * Time.deltaTime;
            transform.Rotate(0f, rotation, 0f);
        }

        // ————— BEWEGUNG —————
        Vector3 moveDir = transform.forward * v;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // ————— ANIMATION —————
        animator.SetFloat("Horizontal", h, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical",   v, 0.1f, Time.deltaTime);

        // ————— HEADBOBBING —————
        if (headBone != null && Mathf.Abs(v) > 0.01f)
        {
            headBobTimer += Time.deltaTime * headBobSpeed;
            float bobAngle = Mathf.Sin(headBobTimer) * headBobAmount;
            headBone.localRotation = Quaternion.Euler(bobAngle, 0f, 0f);
        }
        else if (headBone != null)
        {
            headBone.localRotation = Quaternion.identity;
            headBobTimer = 0f;
        }
    }
}
