// Datei muss "TableSpawnTarget.cs" heißen, damit Unity den Skriptnamen korrekt erkennt
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TableSpawnTarget : MonoBehaviour
{
    [Tooltip("Collider der Tischplatte, auf dem das Target spawnen soll")]
    public Collider tableCollider;

    [Tooltip("Tag der Spinne, das den Respawn auslöst")]
    public string spiderTag = "Spider";

    [Header("Höhen-Offset über der Tischoberfläche")]
    [Tooltip("Minimale Höhe über der Tischplatte")]
    public float minHeightOffset = 0.2f;
    [Tooltip("Maximale Höhe über der Tischplatte")]
    public float maxHeightOffset = 0.5f;

    private Collider _selfCollider;

    private void Awake()
    {
        // Eigenes Collider als Trigger
        _selfCollider = GetComponent<Collider>();
        _selfCollider.isTrigger = true;

        if (tableCollider == null)
            Debug.LogWarning("[TableSpawnTarget] Tisch-Collider nicht zugewiesen!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(spiderTag) && tableCollider != null)
        {
            RespawnOnTable();
        }
    }

    private void RespawnOnTable()
    {
        Bounds b = tableCollider.bounds;
        // Zufällige X/Z Position auf dem Tisch
        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);
        // Höhe: Oberkante Tisch + zufälliger Offset
        float y = b.max.y + Random.Range(minHeightOffset, maxHeightOffset);

        Vector3 newPos = new Vector3(x, y, z);
        transform.position = newPos;

        Debug.Log($"[TableSpawnTarget] Respawned at {newPos}");
    }
}
