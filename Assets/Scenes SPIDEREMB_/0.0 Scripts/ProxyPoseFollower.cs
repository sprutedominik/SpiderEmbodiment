using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProxyPoseFollower : MonoBehaviour
{
    [Header("Source to follow (e.g., IK target at arm tip)")]
    public Transform source;

    public bool offsetInSourceLocal = true;
    public Vector3 positionOffset = Vector3.zero;

    public bool copyRotation = false;     // für SphereCollider meist false
    public Vector3 eulerRotationOffset = Vector3.zero;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (!source) return;

        Vector3 targetPos = offsetInSourceLocal
            ? source.TransformPoint(positionOffset)
            : source.position + positionOffset;

        rb.MovePosition(targetPos);

        if (copyRotation)
        {
            Quaternion rot = source.rotation * Quaternion.Euler(eulerRotationOffset);
            rb.MoveRotation(rot);
        }
    }
}
