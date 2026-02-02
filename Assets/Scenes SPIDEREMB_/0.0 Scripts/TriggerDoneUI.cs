using UnityEngine;
using TMPro;

public class TriggerDoneUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI doneText;

    [Header("Respawn Settings")]
    [Tooltip("Die Spinne (oder der Player) der zurückgesetzt werden soll")]
    public Transform spider;

    [Tooltip("Optional: Expliziter Spawnpunkt. Wenn leer, wird die Startpose der Spinne benutzt.")]
    public Transform spawnPoint;

    [Tooltip("Nach dem Berühren kurz sperren, damit der Trigger nicht spammt")]
    public float cooldown = 0.2f;

    Vector3 _initialPos;
    Quaternion _initialRot;
    bool _coolingDown;

    void Start()
    {
        if (doneText) doneText.gameObject.SetActive(false);

        // Falls Spider nicht im Inspector zugewiesen ist, versuchen wir sie per Tag zu finden
        if (!spider)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) spider = go.transform;
        }

        // Startpose merken (falls kein expliziter Spawnpunkt gesetzt ist)
        if (spider)
        {
            _initialPos = spider.position;
            _initialRot = spider.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_coolingDown) return;

        // Reagieren nur auf die Spinne/den Player
        if (spider && other.transform == spider ||
            other.CompareTag("Player") || other.name.Contains("Spider"))
        {
            if (doneText) doneText.gameObject.SetActive(true);

            RespawnSpider();
            StartCoroutine(Cooldown());
        }
    }

    void RespawnSpider()
    {
        if (!spider) return;

        // Zielpose ermitteln
        Vector3 pos = spawnPoint ? spawnPoint.position : _initialPos;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : _initialRot;

        // Bewegungen stoppen, dann versetzen
        var rb = spider.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
            rb.MovePosition(pos);   // sauberes Versetzen für Physik
            rb.MoveRotation(rot);
        }
        else
        {
            spider.SetPositionAndRotation(pos, rot);
        }

        // Optional: CharacterController kurz toggeln (falls vorhanden)
        var cc = spider.GetComponent<CharacterController>();
        if (cc)
        {
            cc.enabled = false;
            spider.SetPositionAndRotation(pos, rot);
            cc.enabled = true;
        }
    }

    System.Collections.IEnumerator Cooldown()
    {
        _coolingDown = true;
        yield return new WaitForSeconds(cooldown);
        if (doneText) doneText.gameObject.SetActive(false);
        _coolingDown = false;
    }
}
