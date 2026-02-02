using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class MovingLightTargetWithoutNav : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 movementCenter = Vector3.zero;
    public float moveRangeX = 20f;
    public float moveRangeZ = 20f;
    public float repathDelay = 2f;
    public float baseOffset = 1.2f;   // Abstand über Boden
    public float speed = 3.5f;        // Bewegungsgeschwindigkeit

    [Header("Ground limit")]
    [Tooltip("Collider der Bodenfläche (z. B. dein 'Ground (1)')")]
    [SerializeField] private Collider groundCollider;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float raycastHeight = 5f;
    [SerializeField] private float targetStopDistance = 0.2f;

    [Header("Hit-Feedback")]
    [Tooltip("Drag hier dein TextMeshProUGUI ein")]
    public TextMeshProUGUI hitText;

    private Coroutine _wanderRoutine;
    private Vector3 _currentTarget;
    private bool _hasTarget;

    // NEU: feste Höhe „einmal messen, dann beibehalten“
    private float fixedY;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (hitText == null)
            Debug.LogError("[MLT-wn] Kein hitText zugewiesen!");
        if (groundCollider == null)
            Debug.LogWarning("[MLT-wn] Kein Ground Collider zugewiesen.");

        // Startposition auf Bodenhöhe einrasten
        Vector3 start = AdjustToGround(transform.position);
        transform.position = start;

        // feste Höhe merken
        fixedY = transform.position.y;
    }

    private void Start()
    {
        _wanderRoutine = StartCoroutine(WanderLoop());
    }

    private IEnumerator WanderLoop()
    {
        while (true)
        {
            if (!_hasTarget)
            {
                _currentTarget = GetRandomGroundPosition();
                _hasTarget = true;
            }

            // Ziel auf feste Höhe projizieren
            Vector3 targetFlat = new Vector3(_currentTarget.x, fixedY, _currentTarget.z);
            Vector3 next = Vector3.MoveTowards(transform.position, targetFlat, speed * Time.deltaTime);
            next.y = fixedY; // Höhe fix halten
            transform.position = next;

            // Distanzprüfung nur in XZ
            Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 b = new Vector3(targetFlat.x, 0f, targetFlat.z);
            if (Vector3.Distance(a, b) <= targetStopDistance)
            {
                yield return new WaitForSeconds(repathDelay);
                _hasTarget = false;
            }

            yield return null;
        }
    }

    // Zufällige XZ-Position (innerhalb Ground-Bounds, sonst Rechteck) + auf Boden snappen
    private Vector3 GetRandomGroundPosition()
    {
        if (groundCollider != null)
        {
            Bounds b = groundCollider.bounds;

            for (int i = 0; i < 12; i++)
            {
                float x = Random.Range(b.min.x, b.max.x);
                float z = Random.Range(b.min.z, b.max.z);
                Vector3 from = new Vector3(x, b.max.y + raycastHeight, z);

                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, b.size.y + 2f * raycastHeight, groundMask))
                {
                    Vector3 p = hit.point + Vector3.up * baseOffset;
                    return p;
                }
            }
        }

        Vector3 rnd = movementCenter + new Vector3(
            Random.Range(-moveRangeX, moveRangeX),
            0f,
            Random.Range(-moveRangeZ, moveRangeZ)
        );
        return AdjustToGround(rnd);
    }

    // Snapt auf Boden (Layer = groundMask). Fallback: y = baseOffset.
    private Vector3 AdjustToGround(Vector3 pos)
    {
        float startY = (groundCollider != null ? groundCollider.bounds.max.y : pos.y) + raycastHeight;
        Vector3 from = new Vector3(pos.x, startY, pos.z);

        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundMask))
            return hit.point + Vector3.up * baseOffset;

        pos.y = baseOffset;
        return pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spider") || other.CompareTag("Player"))
            HandleHit();
    }

    public void HandleHitViaController(Collider spiderCollider)
    {
        if (spiderCollider.CompareTag("Spider") || spiderCollider.CompareTag("Player"))
            HandleHit();
    }

    private void HandleHit()
    {
        if (hitText != null)
        {
            hitText.text = "HIT!";
            hitText.fontSize = 40;
            hitText.color = Color.white;
            hitText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hitText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hitText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hitText.rectTransform.anchoredPosition = Vector2.zero;
            hitText.gameObject.SetActive(true);
            StartCoroutine(DisableHitTextAfterDelay());
        }

        if (_wanderRoutine != null)
            StopCoroutine(_wanderRoutine);

        RespawnAndContinue();
    }

    private IEnumerator DisableHitTextAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (hitText != null) hitText.gameObject.SetActive(false);
    }

    private void RespawnAndContinue()
    {
        Vector3 respawn = GetRandomGroundPosition();
        // Y wieder fixieren
        transform.position = new Vector3(respawn.x, fixedY, respawn.z);

        _hasTarget = false;
        _wanderRoutine = StartCoroutine(WanderLoop());
    }
}
