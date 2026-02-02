using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FinalSpiderController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 1000f;
    public float animDampTime = 0.1f;

    [Header("Gravity Settings")]
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController cc;
    private Animator anim;
    private Vector3 velocity;

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        velocity = Vector3.zero;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;

        if (inputDir != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        if (cc.isGrounded)
        {
            velocity.y = -1f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 totalMove = moveDir * moveSpeed + velocity;
        cc.Move(totalMove * Time.deltaTime);

        if (anim != null)
        {
            anim.SetFloat("Speed", inputMagnitude, animDampTime, Time.deltaTime);
        }
        Debug.Log("Move velocity: " + cc.velocity.magnitude);

    }
}
