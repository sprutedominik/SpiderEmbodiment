using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SpiderArmResetter : MonoBehaviour
{
    public enum ResetMode { DeltaOnly, FullToCurrentAnchor, FullToInitialRest }

    [Header("Welche Targets zurücksetzen? (IK Chain001 & IK Chain008)")]
    public FollowPositionOnly[] targets;

    [Header("Reset-Modus")]
    public ResetMode resetMode = ResetMode.DeltaOnly;

    [Header("Auto-Reset beim Start (optional)")]
    public bool resetOnStart = false;
    public float resetOnStartDelay = 0f;

    [Header("Editor-Shortcut (zum Testen)")]
    public KeyCode editorKey = KeyCode.R;

    [Header("Input System (Controller-Knopf)")]
    [Tooltip("Button-Action (z. B. Left {primaryButton} = X)")]
    public InputActionReference resetAction;

    IEnumerator Start()
    {
        if (resetOnStart)
        {
            if (resetOnStartDelay > 0f)
                yield return new WaitForSeconds(resetOnStartDelay);
            DoReset();
        }
    }

    void OnEnable()
    {
        if (resetAction != null)
        {
            resetAction.action.performed += OnResetPerformed;
            resetAction.action.Enable();
        }
    }
    void OnDisable()
    {
        if (resetAction != null)
        {
            resetAction.action.performed -= OnResetPerformed;
            resetAction.action.Disable();
        }
    }

    void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(editorKey))
            DoReset();
    }

    void OnResetPerformed(InputAction.CallbackContext ctx) => DoReset();

    public void DoReset()
    {
        if (targets == null) return;

        foreach (var t in targets)
        {
            if (!t) continue;

            switch (resetMode)
            {
                case ResetMode.DeltaOnly:
                    t.RecenterDelta();
                    break;
                case ResetMode.FullToCurrentAnchor:
                    t.FullResetNow();
                    break;
                case ResetMode.FullToInitialRest:
                    t.ResetToInitialRest();
                    t.RecenterDelta();
                    break;
            }
        }
    }
}
