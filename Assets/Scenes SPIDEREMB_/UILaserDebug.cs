using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class UILaserDebug : MonoBehaviour
{
    [Header("Laser & Input")]
    [Tooltip("Der XR Ray Interactor vom rechten Controller")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    [Tooltip("Select-Action (Trigger)")]
    public InputActionReference selectAction;

    void OnEnable()
    {
        // Trigger-Logging
        if (selectAction != null)
            selectAction.action.performed += OnSelectPerformed;
        selectAction?.action.Enable();
    }

    void OnDisable()
    {
        if (selectAction != null)
            selectAction.action.performed -= OnSelectPerformed;
        selectAction?.action.Disable();
    }

    void Update()
    {
        if (rayInteractor == null)
            return;

        // Versuche ein UI-Hit auszulesen
        if (rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiHit) 
            && uiHit.gameObject != null)
        {
            Debug.Log($"[Laser Hit] UI-Element: {uiHit.gameObject.name}");
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext ctx)
    {
        // Wenn Select gedrückt, sag uns, was gerade getroffen wurde
        if (rayInteractor != null &&
            rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiHit) &&
            uiHit.gameObject != null)
        {
            Debug.Log($"[Select] gedrückt auf UI-Element: {uiHit.gameObject.name}");
        }
        else
        {
            Debug.Log("[Select] gedrückt, trifft aber kein UI-Element");
        }
    }
}
