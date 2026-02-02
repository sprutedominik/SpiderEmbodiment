using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CanOutOfBoundsRespawner : MonoBehaviour
{
    [Header("Respawn-Fläche (wähle mindestens eines)")]
    [Tooltip("Transform deines Planes/Bodens (nimmt dessen Y-Höhe)")]
    public Transform areaPlane;                 // z. B. "Plane" oder "Ground"
    [Tooltip("Optional: Collider auf der Fläche (nimmt Größe aus Bounds)")]
    public Collider areaCollider;               // z. B. BoxCollider auf Ground
    [Tooltip("Optional: Renderer auf der Fläche (nimmt Größe aus Bounds)")]
    public Renderer areaRenderer;               // z. B. MeshRenderer des Plane

    [Tooltip("Falls weder Collider noch Renderer gesetzt sind: halbe Breite/Tiefe des Bereichs (m)")]
    public Vector2 rectHalfExtents = new Vector2(1.5f, 1.0f);

    [Header("Respawn-Einstellungen")]
    [Tooltip("Sekunden warten, bevor die Dose neu erscheint")]
    public float respawnDelay = 1.0f;
    [Tooltip("Höhe über der Fläche, auf der gespawnt wird")]
    public float spawnHeight = 0.20f;
    [Tooltip("Seitlicher Sicherheitsrand außerhalb der Fläche, der 'Out of Bounds' auslöst")]
    public float margin = 0.05f;
    [Tooltip("Optional zufällige Yaw-Drehung beim Spawn")]
    public bool randomYaw = false;

    [Header("Manueller Reset (Input System)")]
    [Tooltip("Button-Action binden: <XRController>{LeftHand}/secondaryButton  (Quest: Y)")]
    public InputActionReference resetAction;
    [Tooltip("Editor-Shortcut zum Testen")]
    public KeyCode editorKey = KeyCode.C;

    Rigidbody _rb;
    bool _busy;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (resetAction != null)
        {
            resetAction.action.performed += OnResetPressed;
            resetAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (resetAction != null)
        {
            resetAction.action.performed -= OnResetPressed;
            resetAction.action.Disable();
        }
    }

    void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(editorKey))
            ForceRespawn();

        if (!_busy && IsOutOfBounds())
            StartCoroutine(RespawnAfterDelay(respawnDelay));
    }

    void OnResetPressed(InputAction.CallbackContext _)
    {
        ForceRespawn();
    }

    // ───────────────────────────── helpers ─────────────────────────────

    bool IsOutOfBounds()
    {
        GetRect(out Vector2 ctr, out Vector2 half, out float baseY);

        Vector3 p = transform.position;

        // unter der Fläche?
        if (p.y < baseY - 0.02f) return true;

        // seitlich außerhalb?
        if (Mathf.Abs(p.x - ctr.x) > half.x + margin) return true;
        if (Mathf.Abs(p.z - ctr.y) > half.y + margin) return true;

        return false;
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        _busy = true;
        yield return new WaitForSeconds(delay);
        DoRespawn();
        _busy = false;
    }

    void ForceRespawn()
    {
        if (!_busy)
        {
            StopAllCoroutines();
            DoRespawn();
        }
    }

    void DoRespawn()
    {
        GetRect(out Vector2 ctr, out Vector2 half, out float baseY);

        float x = Random.Range(ctr.x - half.x, ctr.x + half.x);
        float z = Random.Range(ctr.y - half.y, ctr.y + half.y);
        float y = baseY + spawnHeight;

        // Physik zurücksetzen
        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.MovePosition(new Vector3(x, y, z));
        }
        else
        {
            transform.position = new Vector3(x, y, z);
        }

        if (randomYaw)
        {
            var rot = transform.rotation.eulerAngles;
            rot.y = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(rot);
        }
    }

    // Ermittelt Mittelpunkt/Größe (X/Z) und Basis-Y der Fläche
    void GetRect(out Vector2 centerXZ, out Vector2 halfExtentsXZ, out float baseY)
    {
        // Basis-Y
        baseY = areaPlane ? areaPlane.position.y : 0f;

        if (areaCollider != null)
        {
            Bounds b = areaCollider.bounds;
            centerXZ = new Vector2(b.center.x, b.center.z);
            halfExtentsXZ = new Vector2(b.extents.x, b.extents.z);
            if (!areaPlane) baseY = b.center.y; // Fallback, falls plane nicht gesetzt ist
            return;
        }

        if (areaRenderer != null)
        {
            Bounds b = areaRenderer.bounds;
            centerXZ = new Vector2(b.center.x, b.center.z);
            halfExtentsXZ = new Vector2(b.extents.x, b.extents.z);
            if (!areaPlane) baseY = b.center.y;
            return;
        }

        // Fallback nur mit Transform + manuellen Extents
        Vector3 c = areaPlane ? areaPlane.position : Vector3.zero;
        centerXZ = new Vector2(c.x, c.z);
        halfExtentsXZ = rectHalfExtents;
    }
}
