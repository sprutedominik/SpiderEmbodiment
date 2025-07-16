using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TriggerDoneUI : MonoBehaviour
{
    [Tooltip("Text, der eingeblendet wird, wenn das Ziel erreicht wurde")]
    public TextMeshProUGUI doneText;

    [Tooltip("Zeit in Sekunden, bis die Szene neu geladen wird")]
    public float reloadDelay = 2f;

    private void Start()
    {
        if (doneText != null)
        {
            doneText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Spider"))
        {
            if (doneText != null)
            {
                doneText.gameObject.SetActive(true);
            }

            Invoke(nameof(ReloadScene), reloadDelay);
        }
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
