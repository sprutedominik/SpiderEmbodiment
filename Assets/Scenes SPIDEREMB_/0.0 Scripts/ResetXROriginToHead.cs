using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class ResetOriginZToAnchor : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;          // dein XR Origin (VR)
    public Transform anchor;           // Bone29 (z.B. "Bone029")

    [Header("Z Settings (world space)")]
    public float zOffset = 0f;         // optionaler Offset zu anchor.position.z

    [Header("Input")]
    public XRNode inputNode = XRNode.RightHand; // rechter Controller
    InputDevice device;
    bool prevSecondary;

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();
    }

    void OnEnable()
    {
        device = InputDevices.GetDeviceAtXRNode(inputNode);
    }

    void Update()
    {
        if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(inputNode);

        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool sec))
        {
            if (sec && !prevSecondary) SnapZ();
            prevSecondary = sec;
        }
    }

    public void SnapZ()
    {
        if (xrOrigin == null || xrOrigin.Camera == null || anchor == null) return;

        Transform cam = xrOrigin.Camera.transform;

        // Ziel-Z (Weltkoordinate) vom Anchor übernehmen
        float targetZ = anchor.position.z + zOffset;

        // benötigtes Delta nur auf Z
        float deltaZ = targetZ - cam.position.z;

        // XR Origin nur entlang Welt-Z verschieben; Rotation & X/Y bleiben unverändert
        Vector3 originPos = xrOrigin.transform.position;
        originPos.z += deltaZ;
        xrOrigin.transform.position = originPos;
    }
}
