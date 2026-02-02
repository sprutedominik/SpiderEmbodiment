using UnityEngine;

public class XRAnchorDeltaFollowerAdditiveV2 : MonoBehaviour
{
    public enum ApplyMode { AbsoluteWorldToDriven, AdditiveLocalToNode }

    // Targets
    public Transform target;           // z. B. RightHandAnchor
    public Transform anchorForDelta;   // meist = target
    public Transform referenceSpace;   // leer = Weltachsen

    // Ausgabe / Anwendung
    public ApplyMode applyMode = ApplyMode.AdditiveLocalToNode;
    public Transform driven;           // nur für AbsoluteWorldToDriven
    public Transform offsetNode;       // z. B. BodyOffset
    public bool applyInLateUpdate = true;

    // Basis/Offset
    public bool offsetInTargetLocal = true;
    public Vector3 positionOffset = Vector3.zero;

    // Delta Mapping
    public bool useDeltaOnly = true;
    [Min(0f)] public float deltaGain = 2.0f;
    public Vector3 deltaAxisGain = new Vector3(3f, 0f, 3f);
    [Min(0f)] public float maxDeltaMeters = 1.0f;
    [Min(0f)] public float deltaDeadzoneMeters = 0.003f;
    [Range(0, 30)] public int warmupFrames = 3;
    public bool recenterAtStart = true;
    public bool recenterOnLargeFirstDelta = true;
    public float largeDeltaMeters = 0.30f;

    // Y-Klammer (optional)
    public bool clampY = false;
    public bool fixYAtStart = false;
    public float yMin = -Mathf.Infinity, yMax = Mathf.Infinity;

    // Glättung
    public bool smooth = true;
    [Range(1f, 60f)] public float positionLerp = 20f;

    // intern
    Vector3 _restBaseWorld, _initialRestBaseWorld, _anchorStartWorld;
    float _fixedY;
    int _framesSinceEnable;
    bool _restInit, _deltaInit;

    void OnEnable()
    {
        _framesSinceEnable = 0;
        _restInit = _deltaInit = false;
    }

    void Start()
    {
        _restBaseWorld = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        _initialRestBaseWorld = _restBaseWorld;
        _restInit = true;

        if (fixYAtStart)
        {
            _fixedY = _restBaseWorld.y;
            clampY = true; yMin = yMax = _fixedY;
        }

        if (useDeltaOnly && anchorForDelta && recenterAtStart)
        {
            _anchorStartWorld = anchorForDelta.position;
            _deltaInit = true;
        }
    }

    void Update()
    {
        if (!applyInLateUpdate) Apply();
    }

    void LateUpdate()
    {
        if (applyInLateUpdate) Apply();
    }

    void Apply()
    {
        if (!target) return;

        // Basis aus target + Offset
        Vector3 baseNow = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        Vector3 desired = baseNow;

        // Delta addieren
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

            // in Referenzraum → Gains → Clamp → zurück in Welt
            Vector3 deltaLocal = referenceSpace ? referenceSpace.InverseTransformDirection(deltaWorld) : deltaWorld;
            deltaLocal = Vector3.Scale(deltaLocal, deltaAxisGain) * Mathf.Max(0f, deltaGain);

            if (maxDeltaMeters > 0f)
            {
                float m = deltaLocal.magnitude;
                if (m > maxDeltaMeters) deltaLocal *= (maxDeltaMeters / m);
            }

            Vector3 deltaScaledWorld = referenceSpace ? referenceSpace.TransformDirection(deltaLocal) : deltaLocal;

            if (!_restInit) { _restBaseWorld = baseNow; _restInit = true; }

            desired = _restBaseWorld + deltaScaledWorld;

            if (recenterOnLargeFirstDelta && _framesSinceEnable <= warmupFrames + 2)
            {
                float jump = (desired - GetCurrentWorldPosition()).magnitude;
                if (jump > largeDeltaMeters)
                {
                    _anchorStartWorld = anchorForDelta.position;
                    desired = _restBaseWorld;
                }
            }
        }

        if (clampY) desired.y = Mathf.Clamp(desired.y, yMin, yMax);

        // Ausgabe
        if (applyMode == ApplyMode.AbsoluteWorldToDriven)
        {
            Transform t = driven ? driven : transform;
            Vector3 cur = t.position;
            t.position = smooth ? Smooth(cur, desired, positionLerp) : desired;
        }
        else // AdditiveLocalToNode
        {
            if (!offsetNode)
            {
                Debug.LogWarning("[XRAnchorDeltaFollowerAdditiveV2] AdditiveLocalToNode gewählt, aber offsetNode fehlt.");
                return;
            }

            // Offset relativ zur Ruhebasis anwenden
            Vector3 worldOffset = desired - _restBaseWorld;
            Transform parent = offsetNode.parent;
            Vector3 localOffset = parent ? parent.InverseTransformVector(worldOffset) : worldOffset;

            if (clampY) localOffset.y = Mathf.Clamp(localOffset.y, yMin, yMax);

            Vector3 curLocal = offsetNode.localPosition;
            offsetNode.localPosition = smooth ? Smooth(curLocal, localOffset, positionLerp) : localOffset;
        }
    }

    Vector3 GetCurrentWorldPosition()
    {
        if (applyMode == ApplyMode.AbsoluteWorldToDriven)
            return driven ? driven.position : transform.position;

        if (!offsetNode) return Vector3.zero;
        Transform parent = offsetNode.parent;
        return parent ? parent.TransformPoint(offsetNode.localPosition) : offsetNode.localPosition;
    }

    static Vector3 ComputeBaseFromTarget(Transform tgt, Vector3 offset, bool local)
        => !tgt ? Vector3.zero : (local ? tgt.TransformPoint(offset) : tgt.position + offset);

    static Vector3 Smooth(Vector3 current, Vector3 target, float lerpSpeed)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, lerpSpeed) * Time.deltaTime);
        return Vector3.Lerp(current, target, t);
    }

    // Kontextmenü-Utilities
    [ContextMenu("Recenter Delta")]
    public void RecenterDelta()
    {
        if (anchorForDelta)
        {
            _anchorStartWorld = anchorForDelta.position;
            _framesSinceEnable = 0;
            _deltaInit = true;
        }
    }

    [ContextMenu("Reset To Neutral")]
    public void ResetToNeutralNow()
    {
        _restBaseWorld = ComputeBaseFromTarget(target, positionOffset, offsetInTargetLocal);
        _restInit = true;
        if (fixYAtStart)
        {
            _fixedY = _restBaseWorld.y;
            clampY = true; yMin = yMax = _fixedY;
        }
    }

    [ContextMenu("Full Reset")]
    public void FullResetNow()
    {
        ResetToNeutralNow();
        RecenterDelta();
    }

    public void ResetToInitialRest()
    {
        _restBaseWorld = _initialRestBaseWorld;
        _restInit = true;
    }
}
