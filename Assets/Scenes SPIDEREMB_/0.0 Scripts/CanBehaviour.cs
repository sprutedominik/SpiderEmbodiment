// CanBehavior.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CanBehavior : MonoBehaviour
{
    [Tooltip("BoxCollider des Tisches für Bounds")]
    public BoxCollider tableCollider;
    public float respawnDelay = 2f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;  // Physik aktivieren
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("SpiderLeg"))
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    void Update()
    {
        if (transform.position.y < tableCollider.bounds.min.y - 1f)
            Respawn();
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetParent(tableCollider.transform, false);
        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }
}
