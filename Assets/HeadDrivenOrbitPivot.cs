using UnityEngine;
using UnityEngine.XR;

public class HeadDrivenOrbitPivot : MonoBehaviour
{
    public Transform xrOrigin;   // Child von SpiderPivot
    public Transform xrCamera;   // HMD-Camera unter XR Origin
    [Range(0f,1f)] public float factor = 0.25f; // Anteil Kopf-Yaw
    public float maxDegPerSec = 60f;
    public bool holdToOrbit = true; // nur drehen, wenn Grip gehalten

    float lastYaw;

    void Start() { if (xrCamera) lastYaw = xrCamera.localEulerAngles.y; }

    void Update()
    {
        if (!xrCamera) return;
        float yaw = xrCamera.localEulerAngles.y;
        float delta = Mathf.DeltaAngle(lastYaw, yaw) * factor;
        float maxStep = maxDegPerSec * Time.deltaTime;
        delta = Mathf.Clamp(delta, -maxStep, maxStep);

        if (!holdToOrbit || GripPressed()) transform.Rotate(0f, delta, 0f, Space.Self);
        lastYaw = yaw;
    }

    bool GripPressed()
    {
        var l = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var r = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return (l.TryGetFeatureValue(CommonUsages.gripButton, out bool lg) && lg)
            || (r.TryGetFeatureValue(CommonUsages.gripButton, out bool rg) && rg);
    }
}
