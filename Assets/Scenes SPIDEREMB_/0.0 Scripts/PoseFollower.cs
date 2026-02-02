using UnityEngine;

/// <summary>
/// Make this GameObject follow a target Transform (e.g., XR Left/RightHand Controller).
/// Works great for quickly binding spider arms or IK targets to controller poses.
/// Unity 6 / XR Toolkit friendly. Use LateUpdate to follow after XR has updated tracking.
/// </summary>
public class PoseFollower : MonoBehaviour
{
    [Header("Target to follow (e.g., LeftHand Controller or a child anchor)")]
    public Transform target;

    [Header("Optional local offsets relative to target pose")]
    public Vector3 positionOffset;    // in target local space
    public Vector3 rotationOffset;    // euler degrees, applied after target rotation

    [Header("Smoothing (optional)")]
    public bool smooth = false;
    [Range(1f, 60f)] public float positionLerp = 20f;
    [Range(1f, 60f)] public float rotationLerp = 20f;

    void LateUpdate()
    {
        if (!target) return;

        // desired pose
        Vector3 desiredPos = target.position + target.TransformVector(positionOffset);
        Quaternion desiredRot = target.rotation * Quaternion.Euler(rotationOffset);

        if (!smooth)
        {
            transform.SetPositionAndRotation(desiredPos, desiredRot);
        }
        else
        {
            float tPos = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
            float tRot = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPos, tPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, tRot);
        }
    }
}
