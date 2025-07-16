using UnityEngine;
using System.Collections;

public class SpiderTaskManager : MonoBehaviour
{
    [Header("🎯 Zielobjekt (Moving Light)")]
    [Tooltip("Zielobjekt, das sich innerhalb eines Radius bewegt")]
    public Transform movingLightTarget;

    [Header("🕒 Task-Einstellungen")]
    [Tooltip("Wie lange die Aufgabe läuft (in Sekunden)")]
    public float taskDuration = 60f;

    [Header("📏 Bewegungsbereich (Zentriert)")]
    [Tooltip("Zentrum, um das sich das Licht bewegen darf")]
    [SerializeField] private Vector3 movementCenter = new Vector3(6f, 1.5f, 6f);

    [Tooltip("Maximale Bewegung in X-Richtung")]
    [SerializeField] private float moveRangeX = 10f;

    [Tooltip("Maximale Bewegung in Z-Richtung")]
    [SerializeField] private float moveRangeZ = 10f;

    private float timer = 0f;
    private bool isPaused = false;
    private float pauseTime = 0.5f;

    void Start()
    {
        if (movingLightTarget == null)
        {
            Debug.LogWarning("⚠️ Moving Light Target ist nicht zugewiesen!");
            return;
        }

        StartCoroutine(RunSpiderTask());
    }

    IEnumerator RunSpiderTask()
    {
        while (timer < taskDuration)
        {
            timer += Time.deltaTime;

            if (!isPaused)
                MoveLightTarget();

            yield return null;
        }

        Debug.Log("✅ Spider Task abgeschlossen!");
    }

    void MoveLightTarget()
    {
        float x = Mathf.Sin(Time.time * 0.5f) * (moveRangeX * 0.5f);
        float z = Mathf.Cos(Time.time * 0.3f) * (moveRangeZ * 0.5f);

        Vector3 newPosition = new Vector3(
            movementCenter.x + x,
            movementCenter.y,
            movementCenter.z + z
        );

        movingLightTarget.position = newPosition;
    }

    public Vector3 GetRandomTargetPosition()
    {
        float halfX = moveRangeX * 0.5f;
        float halfZ = moveRangeZ * 0.5f;

        Vector3 newPos;
        int attempts = 0;
        do
        {
            float x = Random.Range(-halfX, halfX);
            float z = Random.Range(-halfZ, halfZ);
            newPos = movementCenter + new Vector3(x, 0, z);  // ✅ KORREKT HIER
            attempts++;
        }
        while (Physics.CheckSphere(newPos, 0.5f) && attempts < 10);

        return newPos;
    }

    public void RestartMovement()
    {
        StopAllCoroutines();
        timer = 0f;
        StartCoroutine(RunSpiderTask());
    }

    public void PauseMovement()
    {
        StartCoroutine(PauseRoutine());
    }

    IEnumerator PauseRoutine()
    {
        isPaused = true;
        yield return new WaitForSeconds(pauseTime);
        isPaused = false;
    }
}
