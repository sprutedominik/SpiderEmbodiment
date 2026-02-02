using UnityEngine;
using UnityEngine.XR;

public class XRControllerDebugger : MonoBehaviour
{
    public Transform leftController;
    public Transform rightController;
    public Transform xrCamera;

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private Vector3 lastCamPos;

    void Start()
    {
        if (leftController == null || rightController == null || xrCamera == null)
        {
            Debug.LogError("XRControllerDebugger: Bitte alle Referenzen im Inspector setzen!");
        }

        lastLeftPos = leftController.position;
        lastRightPos = rightController.position;
        lastCamPos = xrCamera.position;
    }

    void Update()
    {
        if (HasMovedSignificantly(leftController.position, lastLeftPos))
        {
            Debug.Log($"[XR DEBUG] Left Controller moved to: {leftController.position}");
            lastLeftPos = leftController.position;
        }

        if (HasMovedSignificantly(rightController.position, lastRightPos))
        {
            Debug.Log($"[XR DEBUG] Right Controller moved to: {rightController.position}");
            lastRightPos = rightController.position;
        }

        if (HasMovedSignificantly(xrCamera.position, lastCamPos))
        {
            Debug.Log($"[XR DEBUG] Camera moved to: {xrCamera.position}");
            lastCamPos = xrCamera.position;
        }
    }

    bool HasMovedSignificantly(Vector3 current, Vector3 last)
    {
        return Vector3.Distance(current, last) > 0.05f;
    }
}
