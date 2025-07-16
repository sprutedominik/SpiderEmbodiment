using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;

public class MovingLightTarget : MonoBehaviour
{
    public SpiderTaskManager taskManager;
    public TextMeshProUGUI hitText; // Zuweisbar im Inspector

    [SerializeField] private float moveSpeed = 1.5f; // Im Inspector einstellbar

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        MoveToNewPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spider") || other.CompareTag("Player"))
        {
            Debug.Log("🕸️ Berührung durch: " + other.name);
            taskManager.PauseMovement();
            ShowHitMessage();
            MoveToNewPosition();
        }
    }

    void MoveToNewPosition()
    {
        Vector3 newTarget = taskManager.GetRandomTargetPosition();
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newTarget, out hit, 1.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning("⚠️ Kein gültiger Punkt auf dem NavMesh gefunden!");
        }
    }

    void ShowHitMessage()
    {
        if (hitText == null) return;
        StartCoroutine(FlashHitText());
    }

    IEnumerator FlashHitText()
    {
        hitText.alpha = 1;
        yield return new WaitForSeconds(1.5f);
        hitText.alpha = 0;
    }
}
