using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent), typeof(Collider))]
public class MovingLightTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 movementCenter = Vector3.zero;
    public float moveRangeX = 20f;
    public float moveRangeZ = 20f;
    public float repathDelay = 2f;
    public float baseOffset = 1.2f;
    // Geschwindigkeit des NavMeshAgent im Inspector einstellbar
    public float speed = 3.5f;

    [Header("Hit-Feedback")]
    [Tooltip("Drag hier dein TextMeshProUGUI ein")]
    public TextMeshProUGUI hitText;

    private NavMeshAgent _agent;
    private Collider _collider;
    private Coroutine _wanderRoutine;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();

        if (_agent == null)
        {
            Debug.LogError("[MLT] Kein NavMeshAgent gefunden!");
        }
        else
        {
            // Sicherheitshalber die Agenten-Werte setzen
            _agent.updateRotation = true;
            _agent.updatePosition = true;
            _agent.speed = speed;
            _agent.baseOffset = baseOffset;
        }

        if (_collider == null)
        {
            Debug.LogError("[MLT] Kein Collider gefunden!");
        }
        else
        {
            // Trigger, damit die Fragebogen-Auslösung etc. funktioniert
            _collider.isTrigger = true;
        }

        if (hitText != null)
        {
            hitText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[MLT] Kein hitText zugewiesen – Hit-Feedback bleibt aus.");
        }
    }

    private void OnEnable()
    {
        // Bewegung starten
        _wanderRoutine = StartCoroutine(WanderLoop());
    }

    private void OnDisable()
    {
        if (_wanderRoutine != null)
        {
            StopCoroutine(_wanderRoutine);
            _wanderRoutine = null;
        }
    }

    private IEnumerator WanderLoop()
    {
        while (true)
        {
            Vector3 targetPos = GetRandomNavMeshPosition();
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = speed;
                _agent.SetDestination(targetPos);
                Debug.Log($"[MLT] Neuer Zielpunkt: {targetPos}");
            }
            else
            {
                Debug.LogWarning("[MLT] Agent nicht auf NavMesh oder nicht vorhanden.");
            }

            yield return new WaitForSeconds(repathDelay);
        }
    }

    private Vector3 GetRandomNavMeshPosition()
    {
        if (movementCenter == Vector3.zero)
        {
            movementCenter = transform.position;
        }

        for (int i = 0; i < 20; i++)
        {
            // Zufälligen Punkt im Rechteck um movementCenter
            float randX = Random.Range(-moveRangeX, moveRangeX);
            float randZ = Random.Range(-moveRangeZ, moveRangeZ);

            Vector3 rnd = new Vector3(
                movementCenter.x + randX,
                movementCenter.y,
                movementCenter.z + randZ
            );

            // Projizieren auf NavMesh
            if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Vector3 p = hit.position;
                p.y = baseOffset;
                return p;
            }
        }

        // Fallback
        Debug.LogWarning("[MLT] Kein gültiger NavMesh-Punkt gefunden, benutze aktuelle Position.");
        Vector3 fallback = transform.position;
        fallback.y = baseOffset;
        return fallback;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Hier kannst du z.B. prüfen, ob der Spieler den Ball "trifft"
        if (other.CompareTag("Player"))
        {
            Debug.Log("[MLT] Player hat MovingLightTarget getroffen!");

            if (hitText != null)
            {
                StartCoroutine(ShowHitText());
            }

            // Falls du nach einem Treffer das Target pausieren oder respawnen willst,
            // kannst du hier z.B. die Coroutine stoppen:
            if (_wanderRoutine != null)
            {
                StopCoroutine(_wanderRoutine);
                _wanderRoutine = null;
            }

            // Alternativ: kurz stehen bleiben und danach weiterwandern
            Invoke(nameof(RespawnAndContinue), 3f);
        }
    }

    private IEnumerator ShowHitText()
    {
        if (hitText != null)
        {
            hitText.gameObject.SetActive(true);
            hitText.text = "HIT!";
            Debug.Log("[MLT] HIT!-Text angezeigt");

            yield return new WaitForSeconds(3f);
            if (hitText != null)
            {
                hitText.gameObject.SetActive(false);
                Debug.Log("[MLT] HIT!-Text wieder deaktiviert");
            }
        }
    }

    private void RespawnAndContinue()
    {
        Vector3 respawn = GetRandomNavMeshPosition();
        _agent.Warp(respawn);
        Debug.Log($"[MLT] Respawn auf {respawn}");

        _wanderRoutine = StartCoroutine(WanderLoop());
    }
}
