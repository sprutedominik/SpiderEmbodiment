using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class SpiderController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed     = 2f;   // Laufgeschwindigkeit
    public float rotationSpeed = 10f;  // Drehgeschwindigkeit
    public float animDampTime  = 0.1f; // Für sanftes Blend

    private Animator anim;
    private CharacterController cc;

    void Awake()
    {
        anim = GetComponent<Animator>();
        cc   = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Sicherheitscheck: Hauptkamera vorhanden?
        if (Camera.main == null)
            return;

        // 1) Achsen statt Keys für gleitende Werte
        float h = Input.GetAxis("Horizontal"); // ← = –1 … +1 = →
        float v = Input.GetAxis("Vertical");   // ↓ = –1 … +1 = ↑

        // 2) Bewegungsrichtung (XZ-Plane, kamerabezogen)
        Vector3 camF = Camera.main.transform.forward;
        camF.y = 0f;
        Vector3 camR = Camera.main.transform.right;
        camR.y = 0f;
        Vector3 camMove = (camF * v + camR * h).normalized;

        // Gesamt-Eingabemagnitude 0…1
        float inputMag = Mathf.Clamp01(new Vector2(h, v).magnitude);

        // 3) Animation: Speed –1…+1 (Rückwärts/Vorwärts) mit Dämpfung
        float targetSpeed = (v < 0f ? -1f : 1f) * inputMag;
        anim.SetFloat("Speed", targetSpeed, animDampTime, Time.deltaTime);

        // 4) Rotation nur bei Bewegung nach vorne oder seitwärts
        if (inputMag > 0.01f && v >= 0f)
        {
            Quaternion tgt = Quaternion.LookRotation(camMove);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                tgt,
                rotationSpeed * Time.deltaTime
            );
        }

        // 5) Physische Bewegung (Vorwärts + Rückwärts)
        if (inputMag > 0.01f)
        {
            Vector3 moveDir;
            if (v < 0f)
            {
                // Rückwärts: entgegen der aktuellen Vorwärtsrichtung
                moveDir = -transform.forward;
            }
            else
            {
                // Vorwärts/seitwärts: immer in aktuelle Vorwärtsrichtung
                moveDir = transform.forward;
            }
            cc.Move(moveDir * moveSpeed * inputMag * Time.deltaTime);
        }
    }
}
