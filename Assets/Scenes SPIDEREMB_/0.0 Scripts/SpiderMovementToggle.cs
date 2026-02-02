using UnityEngine;
using UnityEngine.SceneManagement;

public class SpiderMovementToggle : MonoBehaviour
{
    // Wir nehmen den generischen Typ und prüfen nur, ob er existiert
    private MonoBehaviour controller;

    void Awake()
    {
        // Versuche, den ParticipantSpiderController zu finden
        controller = GetComponent<MonoBehaviour>();

        // Nur deaktivieren, wenn der Controller existiert und wir in der HandlingScene sind
        if (controller != null && SceneManager.GetActiveScene().name == "HandlingScene")
        {
            controller.enabled = false;
        }
    }
}
