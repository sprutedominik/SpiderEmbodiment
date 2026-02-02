using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;  // falls du das Action-basierte XR-Toolkit nutzt

public class SpiderArmFollower : MonoBehaviour
{
    [Header("IK Targets")]
    public Transform frontLeftTarget;
    public Transform frontRightTarget;

    [Header("Controller References")]
    public ActionBasedController leftXRController;
    public ActionBasedController rightXRController;

    [Header("Smoothing")]
    [Range(0, 1f)] public float followSpeed = 0.2f;

    void LateUpdate()
    {
        // Positions-Update
        UpdateTarget(frontLeftTarget,  leftXRController.transform);
        UpdateTarget(frontRightTarget, rightXRController.transform);
    }

    void UpdateTarget(Transform ikTarget, Transform controller)
    {
        if (ikTarget == null || controller == null) return;

        // 1:1-Tracking mit Lerp-Glättung
        ikTarget.position = Vector3.Lerp(
            ikTarget.position,
            controller.position,
            followSpeed
        );
        ikTarget.rotation = Quaternion.Slerp(
            ikTarget.rotation,
            controller.rotation,
            followSpeed
        );
    }
}
