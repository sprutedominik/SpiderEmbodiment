using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Drop this on an empty GameObject in your start scene.
/// It will set CPU/GPU performance hints on Quest at runtime.
/// Works even if the Meta OpenXR package isn't fully wired yet (no compile errors).
/// </summary>
public class XRPerformanceManager : MonoBehaviour
{
    public enum PerfLevel { PowerSavings, SustainedLow, SustainedHigh, Boost }
    public enum Domain { CPU, GPU }

    [Header("Apply automatically on Start")]
    [Tooltip("Apply the default CPU/GPU performance levels when the scene starts.")]
    public bool applyOnStart = true;

    [Header("Default Levels")]
    public PerfLevel defaultCpu = PerfLevel.SustainedHigh;
    public PerfLevel defaultGpu = PerfLevel.SustainedHigh;

    void Start()
    {
        if (applyOnStart)
        {
            Apply(Domain.CPU, defaultCpu);
            Apply(Domain.GPU, defaultGpu);
        }
    }

    /// <summary> Apply high but stable performance (good balance). </summary>
    public void SetHighPerformance()
    {
        Apply(Domain.CPU, PerfLevel.SustainedHigh);
        Apply(Domain.GPU, PerfLevel.SustainedHigh);
    }

    /// <summary> Apply low performance (for simple menus / save battery). </summary>
    public void SetLowPerformance()
    {
        Apply(Domain.CPU, PerfLevel.PowerSavings);
        Apply(Domain.GPU, PerfLevel.PowerSavings);
    }

    /// <summary> Apply temporary boost (for short heavy scenes). </summary>
    public void SetBoost()
    {
        Apply(Domain.CPU, PerfLevel.Boost);
        Apply(Domain.GPU, PerfLevel.Boost);
    }

    /// <summary>
    /// Apply a performance level hint. Safe in Editor (only logs).
    /// On Quest (Android) it uses Meta OpenXR Performance Settings.
    /// </summary>
    public bool Apply(Domain domain, PerfLevel level)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            const string ns = "UnityEngine.XR.OpenXR.Features.Meta";
            const string asmName = "Unity.XR.OpenXR";

            var featureType = Type.GetType($"{ns}.XRPerformanceSettingsFeature, {asmName}");
            var domainType  = Type.GetType($"{ns}.PerformanceDomain, {asmName}");
            var levelType   = Type.GetType($"{ns}.PerformanceLevelHint, {asmName}");

            if (featureType == null || domainType == null || levelType == null)
            {
                Debug.LogWarning("XRPerformanceManager: Meta XR Performance Settings types not found. " +
                                 "Make sure OpenXR + XR Performance Settings are enabled.");
                return false;
            }

            object dom = Enum.Parse(domainType, domain.ToString());
            object lev = Enum.Parse(levelType, level.ToString());

            var mi = featureType.GetMethod("SetPerformanceLevelHint",
                                           BindingFlags.Public | BindingFlags.Static);
            if (mi == null)
            {
                Debug.LogWarning("XRPerformanceManager: SetPerformanceLevelHint method not found.");
                return false;
            }

            mi.Invoke(null, new object[] { dom, lev });
            Debug.Log($"XRPerformanceManager: Set {domain} -> {level}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("XRPerformanceManager: Failed to apply performance hint: " + ex.Message);
            return false;
        }
#else
        Debug.Log($"XRPerformanceManager (Editor/PC): would set {domain} -> {level}");
        return false;
#endif
    }
}
