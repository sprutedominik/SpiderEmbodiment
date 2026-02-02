using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this script to an empty GameObject in your scene (e.g. "VRDebugger").
/// In the Inspector, assign:
///  • rayInteractor: Dein XR Ray Interactor (z.B. rechter Controller).
///  • selectActions:  Liste Deiner InputActionReferences, die Du testen willst (z.B. Click, Submit, Grip).
///  • targetButton:  Den World-Space UI Button (Next Sequence).
///
/// Das Skript loggt zur Laufzeit:
///  1) UI-Raycast-Status (getroffenes UI-Element & Distanz)
///  2) Werte ALLER selectActions
///  3) Perform/Canceled Events pro Action
///  4) Hover/Select Events des XRRayInteractor
///  5) PointerEnter/Exit/Click des Buttons
/// </summary>
public class VRButtonDebug : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [Tooltip("Der XR Ray Interactor (z.B. rechter Controller) für UI pointing.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;

    [Tooltip("Liste von InputActionReferences, die Du testen willst (z.B. XRI UI/Click, XRI UI/Submit, XRI Right Interaction/Select, etc.)")]
    public List<InputActionReference> selectActions = new List<InputActionReference>();

    [Tooltip("Der World-Space UI-Button (Next Sequence).")]
    public Button targetButton;

    private void OnEnable()
    {
        // Hook input callbacks for each action
        foreach (var refAction in selectActions)
        {
            if (refAction?.action != null)
            {
                refAction.action.performed += OnSelectPerformed;
                refAction.action.canceled  += OnSelectCanceled;
            }
        }

        // Hook XRRayInteractor events
        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.AddListener(OnHoverEntered);
            rayInteractor.hoverExited .AddListener(OnHoverExited);
            rayInteractor.selectEntered.AddListener(OnSelectEnter);
            rayInteractor.selectExited .AddListener(OnSelectExit);
        }

        // UI-Pointer-Logging
        if (targetButton != null)
        {
            var logger = targetButton.gameObject.AddComponent<UIButtonEventLogger>();
            logger.targetName = targetButton.gameObject.name;
        }
    }

    private void OnDisable()
    {
        foreach (var refAction in selectActions)
        {
            if (refAction?.action != null)
            {
                refAction.action.performed -= OnSelectPerformed;
                refAction.action.canceled  -= OnSelectCanceled;
            }
        }

        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            rayInteractor.hoverExited .RemoveListener(OnHoverExited);
            rayInteractor.selectEntered.RemoveListener(OnSelectEnter);
            rayInteractor.selectExited .RemoveListener(OnSelectExit);
        }
    }

    private void Update()
    {
        // 1) UI-Raycast-Status
        if (rayInteractor != null && rayInteractor.TryGetCurrentUIRaycastResult(out var uiResult))
        {
            Debug.Log($"[UI Hit] {uiResult.gameObject.name} @ {uiResult.distance:F2}");
        }
        else
        {
            Debug.Log("[UI Hit] kein UI-Element unter Pointer");
        }

        // 2) Werte aller selectActions
        foreach (var refAction in selectActions)
        {
            if (refAction?.action != null)
            {
                float v = refAction.action.ReadValue<float>();
                Debug.Log($"[Input] Action '{refAction.action.name}' value = {v:F2}");
            }
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[Input] '{ctx.action.name}' PERFORMED at {Time.time:F2}");
    }

    private void OnSelectCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[Input] '{ctx.action.name}' CANCELED  at {Time.time:F2}");
    }

    // Helper: aus IXRInteractable das GameObject extrahieren
    private string GetInteractableName(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable ixr)
    {
        var comp = ixr as Component;
        return comp != null ? comp.gameObject.name : ixr.ToString();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
        => Debug.Log($"[XRRayInteractor] Hover ENTER on {GetInteractableName(args.interactableObject)}");

    private void OnHoverExited(HoverExitEventArgs args)
        => Debug.Log($"[XRRayInteractor] Hover EXIT  from {GetInteractableName(args.interactableObject)}");

    private void OnSelectEnter(SelectEnterEventArgs args)
        => Debug.Log($"[XRRayInteractor] Select ENTER on {GetInteractableName(args.interactableObject)}");

    private void OnSelectExit(SelectExitEventArgs args)
        => Debug.Log($"[XRRayInteractor] Select EXIT  from {GetInteractableName(args.interactableObject)}");
}

/// <summary>
/// Loggt Pointer-Events auf UI-Buttons.
/// Dynamisch von VRButtonDebug hinzugefügt.
/// </summary>
class UIButtonEventLogger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public string targetName;

    public void OnPointerEnter(PointerEventData e)
        => Debug.Log($"[UI Event] Pointer ENTER on {targetName}");

    public void OnPointerExit(PointerEventData e)
        => Debug.Log($"[UI Event] Pointer EXIT  from {targetName}");

    public void OnPointerClick(PointerEventData e)
        => Debug.Log($"[UI Event] Pointer CLICK on {targetName}");
}