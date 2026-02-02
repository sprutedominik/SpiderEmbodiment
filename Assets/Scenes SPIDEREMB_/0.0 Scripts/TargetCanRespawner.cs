using UnityEngine;

public class TargetCanRespawner : MonoBehaviour
{
    public Transform tableTop;  // Die Fläche, auf der sich die Dose bewegen soll
    public string spiderTag = "Spider"; // Stelle sicher, dass deine Spinne diesen Tag hat

    private BoxCollider tableCollider;

    private void Start()
    {
        if (tableTop != null)
        {
            tableCollider = tableTop.GetComponent<BoxCollider>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(spiderTag) && tableCollider != null)
        {
            Vector3 newPos = GetRandomPositionOnTable();
            transform.position = newPos;
        }
    }

    private Vector3 GetRandomPositionOnTable()
    {
        Vector3 center = tableCollider.bounds.center;
        Vector3 size = tableCollider.bounds.size;

        float x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
        float z = Random.Range(center.z - size.z / 2f, center.z + size.z / 2f);
        float y = tableTop.position.y + 0.15f; // etwas über der Platte

        return new Vector3(x, y, z);
    }
}
