using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils; // für XROrigin

public class AlignXROriginAtStart : MonoBehaviour
{
    [Header("Target Pose")]
    [SerializeField] private Transform playerStart;   // Zielpose für die KAMERA (Augenhöhe)

    [Header("Settings")]
    [SerializeField] private bool useFloorOrigin = true;     // Floor vs Device
    [SerializeField] private bool lockYToFloorZero = false;  // Y auf 0 klemmen (nur wenn gewünscht)

    private XROrigin xrOrigin;

    private IEnumerator Start()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (!xrOrigin)
        {
            Debug.LogError("AlignXROriginAtStart: XROrigin-Komponente fehlt!");
            yield break;
        }

        // Tracking-Origin-Mode setzen
        xrOrigin.RequestedTrackingOriginMode = useFloorOrigin
            ? XROrigin.TrackingOriginMode.Floor
            : XROrigin.TrackingOriginMode.Device;

        // Warten bis XR-System läuft
        yield return new WaitUntil(IsXRRunning);
        yield return null; // ein Frame warten

        // Kamera auf PlayerStart ausrichten
        AlignOriginToMatchCameraWith(playerStart);
    }

    private bool IsXRRunning()
    {
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var s in subsystems)
        {
            if (s != null && s.running) return true;
        }
        return false;
    }

    private void AlignOriginToMatchCameraWith(Transform targetCameraPose)
    {
        if (!targetCameraPose || !xrOrigin.Camera) return;

        Transform origin = xrOrigin.Origin.transform;
        Transform cam = xrOrigin.Camera.transform;

        // --- Rotation (nur Yaw) angleichen ---
        Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 targetFwd = Vector3.ProjectOnPlane(targetCameraPose.forward, Vector3.up).normalized;
        float yawDelta = Vector3.SignedAngle(camFwd, targetFwd, Vector3.up);
        origin.RotateAround(cam.position, Vector3.up, yawDelta);

        // --- Position angleichen (inkl. Y) ---
        Vector3 camPosAfterRot = xrOrigin.Camera.transform.position;
        Vector3 delta = targetCameraPose.position - camPosAfterRot;
        origin.position += delta;

        // --- Optional: Y auf 0 klemmen ---
        if (lockYToFloorZero && xrOrigin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Floor)
        {
            origin.position = new Vector3(origin.position.x, 0f, origin.position.z);
        }
    }
}
