using UnityEngine;

public class SpiderFrontLegSimple : MonoBehaviour
{
    [Header("Spider Root")]
    public Transform spiderRoot;

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("IK Targets (Final Targets in your TwoBoneIK)")]
    public Transform finalTargetLeft;
    public Transform finalTargetRight;

    [Header("Procedural Motion")]
    public bool useProcedural = true;
    public float stepHeight = 0.1f;
    public float stepLength = 0.2f;
    public float stepSpeed = 2f;
    public Vector3 baseOffsetLeft = new Vector3(-0.3f, 0.35f, 0.8f);
    public Vector3 baseOffsetRight = new Vector3(0.3f, 0.35f, 0.8f);

    private float time;

    void Update()
    {
        if (useProcedural)
        {
            time += Time.deltaTime * stepSpeed;

            // Left leg
            Vector3 leftLocal = baseOffsetLeft;
            leftLocal.y += Mathf.Abs(Mathf.Sin(time)) * stepHeight;
            leftLocal.z += Mathf.Cos(time) * stepLength;
            finalTargetLeft.position = spiderRoot.TransformPoint(leftLocal);
            finalTargetLeft.rotation = spiderRoot.rotation;

            // Right leg (opposite phase)
            Vector3 rightLocal = baseOffsetRight;
            rightLocal.y += Mathf.Abs(Mathf.Sin(time + Mathf.PI)) * stepHeight;
            rightLocal.z += Mathf.Cos(time + Mathf.PI) * stepLength;
            finalTargetRight.position = spiderRoot.TransformPoint(rightLocal);
            finalTargetRight.rotation = spiderRoot.rotation;
        }
        else
        {
            // Direct follow controllers
            if (leftController) finalTargetLeft.SetPositionAndRotation(leftController.position, leftController.rotation);
            if (rightController) finalTargetRight.SetPositionAndRotation(rightController.position, rightController.rotation);
        }
    }
}
