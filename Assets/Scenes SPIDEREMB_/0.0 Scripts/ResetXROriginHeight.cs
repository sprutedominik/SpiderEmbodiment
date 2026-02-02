using UnityEngine;
using UnityEngine.XR;

public class ResetXROriginHeight : MonoBehaviour
{
    public Transform xrOrigin;

    void Start()
    {
        // optional: nur bei aktiviertem Headset
        if (XRSettings.isDeviceActive)
        {
            Vector3 newPos = xrOrigin.position;
            newPos.y = 0; // oder eine Wunschhöhe
            xrOrigin.position = newPos;
        }
    }
}
