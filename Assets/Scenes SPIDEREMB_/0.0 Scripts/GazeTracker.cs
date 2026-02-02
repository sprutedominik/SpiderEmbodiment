using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GazeTracker : MonoBehaviour
{
    private float lookStartTime;
    private float totalLookTime = 0f;
    private int gazeCount = 0;

    private void OnEnable()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnGazeEnter);
        interactable.hoverExited.AddListener(OnGazeExit);
    }

    private void OnDisable()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        interactable.hoverEntered.RemoveListener(OnGazeEnter);
        interactable.hoverExited.RemoveListener(OnGazeExit);
    }

    private void OnGazeEnter(HoverEnterEventArgs args)
    {
        lookStartTime = Time.time;
        gazeCount++;
        Debug.Log("👁️ Gaze started on spider");
    }

    private void OnGazeExit(HoverExitEventArgs args)
    {
        float duration = Time.time - lookStartTime;
        totalLookTime += duration;
        Debug.Log($"👁️ Gaze ended. Duration: {duration:F2}s | Total: {totalLookTime:F2}s | Times looked: {gazeCount}");
    }
}
