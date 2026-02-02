using UnityEngine;
using SD = System.Diagnostics;   // Alias für System.Diagnostics

[DisallowMultipleComponent]
public class WhoDisabledMe : MonoBehaviour
{
    void OnEnable()
    {
        UnityEngine.Debug.Log($"[WhoDisabledMe] {name} ENABLED\n{new SD.StackTrace(true)}");
    }

    void OnDisable()
    {
        UnityEngine.Debug.LogWarning($"[WhoDisabledMe] {name} DISABLED\n{new SD.StackTrace(true)}");
    }

    void OnDestroy()
    {
        UnityEngine.Debug.LogWarning($"[WhoDisabledMe] {name} DESTROYED\n{new SD.StackTrace(true)}");
    }
}
