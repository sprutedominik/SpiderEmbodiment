using UnityEngine;

public class FollowPositionOnly : MonoBehaviour
{
    [Header("Ziel / Target (Hand-Anchor)")]
    public Transform target;                     // z. B. LeftHandAnchor / RightHandAnchor
    public bool offsetInTargetLocal = true;
    public Vector3 positionOffset = Vector3.zero;

    // ─────────────────────────────────────────────────────────────────────────────
    [Header("OPTION A (AUS lassen): Pivot-Gain (absolute Position um Bezugspunkt)")]
    public Transform pivot = null;               // neutraler Bezugspunkt (Root, Scale 1)
    [Min(0f)] public float gain = 1f;
    public Vector3 axisGain = Vector3.one;
    [Min(0f)] public float maxDistanceFromPivot = 0f;

    // ─────────────────────────────────────────────────────────────────────────────
    [Header("OPTION B: Delta-Only Mapping (empfohlen)")]
    public bool useDeltaOnly = true;
    public Transform anchorForDelta = null;      // meist derselbe wie target
    public Transform referenceSpace = null;      // z. B. MappingPivot (Root, Rot=0, Scale=1)
    [Min(0f)] public float deltaGain = 2.0f;
    public Vector3 deltaAxisGain = new Vector3(3f, 0f, 3f);
    [Min(0f)] public float maxDeltaMeters = 1.0f;
    public bool recenterAtStart = true;
    [Range(0,30)] public int warmupFrames = 3;
    public bool recenterOnLargeFirstDelta = true;
    public float largeDeltaMeters = 0.30f;
    [Min(0f)] public float deltaDeadzoneMeters = 0.003f;

    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Optional: Höhen-Klammer (Y begrenzen)")]
    public bool clampY = false;
    public float yMin = -Mathf.Infinity, yMax = Mathf.Infinity;

    [Header("Optional: Minimales Glätten")]
    public bool smooth = false;
    [Range(1f,60f)] public float positionLerp = 20f;

    // intern
    Vector3 _restBaseWorld;           // aktuelle Ruhe-Basis
    Vector3 _initialRestBaseWorld;    // beim Start gemerkte Ruhe-Basis (für „Initial Reset“)
    Vector3 _anchorStartWorld;        // Delta-Null des Anchors
    int _framesSinceEnable;
    bool _restInit;
    bool _deltaInit;

    void OnEnable()
    {
        _framesSinceEnable = 0;
        _restInit = false;
        _deltaInit = false;
    }

    void Start()
    {
        _restBaseWorld = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        _initialRestBaseWorld = _restBaseWorld;       // << neu: Start-Basis merken
        _restInit = true;

        if (useDeltaOnly && anchorForDelta && recenterAtStart)
        {
            _anchorStartWorld = anchorForDelta.position;
            _deltaInit = true;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 baseNow = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        Vector3 desired = baseNow;

        if (useDeltaOnly && anchorForDelta)
        {
            if (!_deltaInit)
            {
                _anchorStartWorld = anchorForDelta.position;
                _deltaInit = true;
                _framesSinceEnable = 0;
            }
            _framesSinceEnable++;
            if (_framesSinceEnable <= warmupFrames)
                _anchorStartWorld = anchorForDelta.position;

            Vector3 deltaWorld = anchorForDelta.position - _anchorStartWorld;
            if (deltaWorld.magnitude < deltaDeadzoneMeters) deltaWorld = Vector3.zero;

            Vector3 deltaLocal = referenceSpace
                ? referenceSpace.InverseTransformDirection(deltaWorld)
                : deltaWorld;

            deltaLocal = Vector3.Scale(deltaLocal, deltaAxisGain) * Mathf.Max(0f, deltaGain);

            if (maxDeltaMeters > 0f)
            {
                float m = deltaLocal.magnitude;
                if (m > maxDeltaMeters) deltaLocal *= (maxDeltaMeters / m);
            }

            Vector3 deltaScaledWorld = referenceSpace
                ? referenceSpace.TransformDirection(deltaLocal)
                : deltaLocal;

            if (!_restInit)
            {
                _restBaseWorld = baseNow;
                _restInit = true;
            }
            desired = _restBaseWorld + deltaScaledWorld;

            if (recenterOnLargeFirstDelta && _framesSinceEnable <= warmupFrames + 2)
            {
                float jump = (desired - transform.position).magnitude;
                if (jump > largeDeltaMeters)
                {
                    _anchorStartWorld = anchorForDelta.position;
                    desired = _restBaseWorld;
                }
            }
        }
        else
        {
            if (pivot && Mathf.Abs(gain - 1f) > 0.0001f)
                desired = ApplyPivotGain(baseNow, pivot, axisGain, gain, maxDistanceFromPivot);
            else
                desired = baseNow;
        }

        if (clampY) desired.y = Mathf.Clamp(desired.y, yMin, yMax);

        if (!smooth) transform.position = desired;
        else
        {
            float t = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
    }

    // Helpers
    static Vector3 ComputeBaseFromTarget(Transform tgt, Vector3 offset, bool local)
    {
        if (!tgt) return Vector3.zero;
        return local ? tgt.TransformPoint(offset) : tgt.position + offset;
    }

    static Vector3 ApplyPivotGain(Vector3 ptWorld, Transform pivot, Vector3 axisGain, float gain, float maxDist)
    {
        if (!pivot) return ptWorld;
        Vector3 local = pivot.InverseTransformPoint(ptWorld);
        local = Vector3.Scale(local, axisGain) * gain;
        if (maxDist > 0f)
        {
            float m = local.magnitude;
            if (m > maxDist) local *= (maxDist / m);
        }
        return pivot.TransformPoint(local);
    }

    // === Reset-APIs ============================================================

    /// <summary>Nur Delta-Null auf aktuelle Anchor-Position setzen.</summary>
    [ContextMenu("Recenter Delta (nur Delta-Null)")]
    public void RecenterDelta()
    {
        if (anchorForDelta)
        {
            _anchorStartWorld = anchorForDelta.position;
            _framesSinceEnable = 0;
            _deltaInit = true;
        }
    }

    /// <summary>Ruhe-Basis auf aktuelle Anchor+Offset-Position setzen.</summary>
    [ContextMenu("Reset To Neutral (nur Ruhe-Basis)")]
    public void ResetToNeutralNow()
    {
        _restBaseWorld = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        _restInit = true;
    }

    /// <summary>Ruhe-Basis & Delta-Null auf „jetzt“ setzen.</summary>
    [ContextMenu("Full Reset (Basis + Delta)")]
    public void FullResetNow()
    {
        ResetToNeutralNow();
        RecenterDelta();
    }

    /// <summary>Ruhe-Basis auf die beim Start gemerkte Pose zurücksetzen.</summary>
    public void ResetToInitialRest()
    {
        _restBaseWorld = _initialRestBaseWorld;
        _restInit = true;
    }
}
