using UnityEngine;

using UnityEngine.InputSystem;

public class RayDebugger : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    public InputActionProperty xButtonAction; // Für X-Button (Right Controller)
    private bool wasPressedLastFrame = false;

    void Update()
    {
        if (rayInteractor == null)
        {
            Debug.LogWarning("[RayDebugger] Kein XRRayInteractor zugewiesen!");
            return;
        }

        // --- Raycast-Debug ---
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Debug.Log($"[RayDebugger] Physics Hit: {hit.collider.name} @ {hit.point}");
        }
        else if (rayInteractor.TryGetCurrentUIRaycastResult(out var uiHit))
        {
            Debug.Log($"[RayDebugger] UI Hit: {uiHit.gameObject.name}");
        }
        else
        {
            Debug.Log("[RayDebugger] No hit");
        }

        // --- X-Button-Debug ---
        if (xButtonAction != null && xButtonAction.action != null)
        {
            bool isPressed = xButtonAction.action.IsPressed();

            if (isPressed && !wasPressedLastFrame)
            {
                Debug.Log("[RayDebugger] X-Button WURDE gedrückt!");
            }
            wasPressedLastFrame = isPressed;
        }
        else
        {
            Debug.LogWarning("[RayDebugger] Kein X-Button InputAction verlinkt!");
        }

        // --- Interactor Status ---
        bool hasValidTarget = rayInteractor.TryGetCurrent3DRaycastHit(out _) ||
                              rayInteractor.TryGetCurrentUIRaycastResult(out _);

        Debug.Log($"[RayDebugger] XRRayInteractor enabled = {rayInteractor.enabled}, Hat Target = {hasValidTarget}");
    }
}
