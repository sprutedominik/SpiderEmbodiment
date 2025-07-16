using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HeadBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobFrequency = 1.5f;       // Wie schnell der Bob
    public float bobAmplitude = 0.05f;      // Wie stark nach oben und unten
    public float bobSmoothing = 10f;        // Wie sanft der Übergang

    private Vector3 _startPos;
    private float _timer = 0f;
    private CharacterController _cc;

    void Start()
    {
        _startPos = transform.localPosition;
        _cc = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        float speed = _cc ? _cc.velocity.magnitude : 0f;

        if (speed > 0.1f)
        {
            // Timer mit Frequenz hochzählen
            _timer += Time.deltaTime * bobFrequency;
            // Ziel-Y via Sinus
            float yOffset = Mathf.Sin(_timer) * bobAmplitude;
            Vector3 targetPos = _startPos + Vector3.up * yOffset;
            // sanftes Lerp
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            // Reset, wenn nicht bewegt wird
            _timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _startPos, Time.deltaTime * bobSmoothing);
        }
    }
}
