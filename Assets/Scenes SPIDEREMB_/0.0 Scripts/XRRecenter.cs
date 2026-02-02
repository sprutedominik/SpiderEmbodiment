using UnityEngine;
using UnityEngine.InputSystem;      // New Input System
using Unity.XR.CoreUtils;           // XROrigin

public class XRRecenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;     // Dein XR Origin
    [SerializeField] private Transform referencePoint; // z.B. bone29 (Position + Yaw sind maßgeblich)

    [Header("Input (OpenXR)")]
    // Bind hier RightHand / secondaryButton (B) hinein
    [SerializeField] private InputActionProperty recenterAction;

    private Transform Head => xrOrigin != null ? xrOrigin.Camera.transform : null;

    private void Reset()
    {
        // Auto-Find beim Hinzufügen
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();
    }

    private void OnEnable()
    {
        if (recenterAction != null && recenterAction.action != null)
        {
            recenterAction.action.performed += OnRecenter;
            recenterAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (recenterAction != null && recenterAction.action != null)
        {
            recenterAction.action.performed -= OnRecenter;
            recenterAction.action.Disable();
        }
    }

    private void OnRecenter(InputAction.CallbackContext _)
    {
        if (xrOrigin == null || referencePoint == null || Head == null) return;

        // 1) Yaw (Drehung um Up-Achse) angleichen
        float currentYaw = Head.eulerAngles.y;
        float targetYaw  = referencePoint.eulerAngles.y;
        float deltaYaw   = Mathf.DeltaAngle(currentYaw, targetYaw);

        // Drehe den Origin um die Kamera – so bleiben Abstände/Hände proportional korrekt
        xrOrigin.RotateAroundCameraUsingOriginUp(deltaYaw);

        // 2) Kamera-Position exakt auf den Referenzpunkt setzen
        xrOrigin.MoveCameraToWorldLocation(referencePoint.position);
    }
}
