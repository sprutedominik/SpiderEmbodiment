using UnityEngine;
using System.Collections;

public class SpiderTaskManager : MonoBehaviour
{
    [Header("Moving Light Target (Inspector setzen)")]
    public Transform movingLightTarget;

    [Header("Aufgabendauer (Sekunden)")]
    public float taskDuration = 60f;

    [Header("Bewegungsbereich (zentriert um)")]
    [SerializeField] private Vector3 movementCenter = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float moveRangeX = 20f;
    [SerializeField] private float moveRangeZ = 20f;

    private float timer = 0f;
    private bool isPaused = false;
    private float pauseTime = 0.5f;

    private void Start()
    {
        if (movingLightTarget == null)
        {
            Debug.LogError("[SpiderTaskManager] movingLightTarget nicht gesetzt!");
            enabled = false;
            return;
        }
        Debug.Log("[SpiderTaskManager] Task gestartet");
        StartCoroutine(RunTask());
    }

    private IEnumerator RunTask()
    {
        while (timer < taskDuration)
        {
            // Wir steuern hier nichts direkt, nur die Zeit
            timer += Time.deltaTime;
            yield return null;
        }
        Debug.Log("[SpiderTaskManager] Aufgabe beendet");
    }

    public void OnTargetRespawned()
    {
        StartCoroutine(PauseRoutine());
    }

    private IEnumerator PauseRoutine()
    {
        isPaused = true;
        yield return new WaitForSeconds(pauseTime);
        isPaused = false;
    }

    public Vector3 GetRandomTargetPosition()
    {
        Vector3 pos = movementCenter;
        int attempts = 0;
        do
        {
            float rx = Random.Range(-moveRangeX, moveRangeX);
            float rz = Random.Range(-moveRangeZ, moveRangeZ);
            pos = movementCenter + new Vector3(rx, 0f, rz);
            attempts++;
        }
        while (attempts < 20 && Physics.CheckSphere(pos, 0.5f));
        return pos;
    }
}
