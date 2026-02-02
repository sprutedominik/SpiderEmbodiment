using UnityEngine;
using UnityEngine.UI;

using UnityEngine.InputSystem;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor))]
public class RayClickFallback : MonoBehaviour
{
    [Tooltip("Referenz auf euer Submit-InputAction-Asset (XRI Right Interaction / Activate oder UI Press).")]
    public InputActionReference submitAction;

    UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor _rayInteractor;

    void Awake()
    {
        _rayInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
    }

    void OnEnable()
    {
        submitAction.action.Enable();
        submitAction.action.performed += OnSubmit;
    }

    void OnDisable()
    {
        submitAction.action.performed -= OnSubmit;
        submitAction.action.Disable();
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        // Wir fragen den aktuellen UI‑Hit ab
        if (_rayInteractor.TryGetCurrentUIRaycastResult(out var uiResult))
        {
            var go = uiResult.gameObject;
            var btn = go.GetComponent<Button>();
            if (btn != null && btn.interactable)
            {
                btn.onClick.Invoke();
            }
        }
    }
}
