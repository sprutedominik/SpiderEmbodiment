using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class CanRespawnerLegOnly : MonoBehaviour
{
    public enum AreaType { RectFromRenderer, RectFromCollider, Circle }

    [Header("Auslöser (nur bei Bein-Treffer)")]
    [Tooltip("Tag, das deine Spinnenbein-Proxys haben (Colliders an den Proxys)")]
    public string spiderLegTag = "SpiderLeg";

    [Header("Wann respawnen?")]
    [Tooltip("Wartezeit NACH dem Umkippen (Sekunden)")]
    public float respawnDelay = 1f;

    [Tooltip("Ab diesem Neigungswinkel (Grad) gilt die Dose als umgefallen")]
    [Range(5f, 85f)] public float tippedAngleDeg = 25f;

    [Tooltip("So lange muss der Winkel überschritten bleiben, bevor der Timer startet (Sekunden)")]
    public float tippedStableTime = 0.20f;

    [Tooltip("Sicherheits-Timeout falls sie doch nicht kippt (0 = aus)")]
    public float fallTimeout = 5f;

    [Header("Respawn-Fläche")]
    public AreaType areaType = AreaType.RectFromRenderer;

    [Tooltip("Für RectFromRenderer: z.B. dein Plane mit MeshRenderer")]
    public Renderer areaRenderer;

    [Tooltip("Für RectFromCollider: z.B. ein BoxCollider auf der Fläche")]
    public Collider areaCollider;

    [Tooltip("Für Circle: Mittelpunkt (Empty o.ä.)")]
    public Transform areaCenter;

    [Tooltip("Für Circle: Radius in Metern")]
    public float areaRadius = 0.6f;

    [Header("Spawnhöhe/Rotation")]
    [Tooltip("Höhe ÜBER der Fläche, aus der die Dose wieder 'fällt'")]
    public float spawnHeight = 0.2f;

    [Tooltip("Zufällige Yaw-Drehung beim Respawn")]
    public bool randomYaw = true;

    // intern
    Rigidbody _rb;
    bool _waiting;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // sinnvolle Defaults für eine „normale“ Dose
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnCollisionEnter(Collision col)
    {
        if (_waiting) return; // schon geplant?

        // Nur Bein-Treffer zählen
        if (!string.IsNullOrEmpty(spiderLegTag) && col.collider.CompareTag(spiderLegTag))
        {
            StartCoroutine(Co_WaitUntilTippedThenRespawn());
            _waiting = true;
        }
    }

    IEnumerator Co_WaitUntilTippedThenRespawn()
    {
        float stable = 0f;
        float t = 0f;

        // 1) Warten bis die Dose wirklich gekippt ist (Winkel > Schwellwert „stabil“)
        while (true)
        {
            t += Time.deltaTime;

            float angle = Vector3.Angle(transform.up, Vector3.up); // 0° = steht, 90° = liegt
            if (angle >= tippedAngleDeg)
                stable += Time.deltaTime;
            else
                stable = 0f;

            if (stable >= tippedStableTime) break; // genug „liegend“
            if (fallTimeout > 0f && t >= fallTimeout) break; // Sicherheit

            yield return null;
        }

        // 2) Noch X Sekunden liegen lassen (sichtbar)
        yield return new WaitForSeconds(respawnDelay);

        // 3) Respawn
        Respawn();

        _waiting = false;
    }

    void Respawn()
    {
        // Physik stoppen & aufrecht hinstellen
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Zielpunkt zufällig auf/über der Fläche
        Vector3 p = PickSpawnPoint();
        transform.position = p;

        // aufrecht + wahlweise zufällige Yaw
        var e = transform.eulerAngles;
        e.x = 0f; e.z = 0f;
        if (randomYaw) e.y = Random.Range(0f, 360f);
        transform.eulerAngles = e;
    }

    Vector3 PickSpawnPoint()
    {
        switch (areaType)
        {
            case AreaType.RectFromCollider:
                if (areaCollider)
                {
                    Bounds b = areaCollider.bounds;
                    float x = Random.Range(b.min.x, b.max.x);
                    float z = Random.Range(b.min.z, b.max.z);
                    float y = b.max.y + spawnHeight;
                    return new Vector3(x, y, z);
                }
                break;

            case AreaType.RectFromRenderer:
                if (areaRenderer)
                {
                    Bounds b = areaRenderer.bounds;
                    float x = Random.Range(b.min.x, b.max.x);
                    float z = Random.Range(b.min.z, b.max.z);
                    float y = b.max.y + spawnHeight;
                    return new Vector3(x, y, z);
                }
                break;

            case AreaType.Circle:
                Vector3 c = areaCenter ? areaCenter.position : transform.position;
                Vector2 r = Random.insideUnitCircle * Mathf.Max(0f, areaRadius);
                return new Vector3(c.x + r.x, c.y + spawnHeight, c.z + r.y);
        }

        // Fallback
        return transform.position + Vector3.up * spawnHeight;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        if (areaType == AreaType.RectFromRenderer && areaRenderer)
            Gizmos.DrawWireCube(areaRenderer.bounds.center, areaRenderer.bounds.size);
        else if (areaType == AreaType.RectFromCollider && areaCollider)
            Gizmos.DrawWireCube(areaCollider.bounds.center, areaCollider.bounds.size);
        else if (areaType == AreaType.Circle && areaCenter)
            UnityEditor.Handles.DrawWireDisc(areaCenter.position, Vector3.up, areaRadius);
    }
#endif
}
