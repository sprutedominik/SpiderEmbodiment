using UnityEngine;

[DefaultExecutionOrder(-80)]
public class SpiderFrontLegDriver : MonoBehaviour
{
    [Header("Frames")]
    public Transform spiderRoot;       // Armature root (e.g. "RootBone")
    public Transform referenceFrame;   // XR: "Camera Offset"
    public Transform reachAnchor;      // neutral point in front of spider

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("IK Targets & Hints")]
    public Transform leftTarget;       // equals TwoBoneIK.data.target (left)
    public Transform rightTarget;      // equals TwoBoneIK.data.target (right)
    public Transform leftHint;
    public Transform rightHint;

    [Header("Workspace (Spider local)")]
    public Vector3 baseOffsetInSpider = new Vector3(0f, 0.35f, 0.80f);
    public float   maxReachRadius = 0.9f;
    [Tooltip("Minimum Höhe (Spider-Local Y) für Targets")]
    public float   minLocalY = 0.05f;

    [Header("Gains & Smoothing")]
    public Vector3 positionGain = Vector3.one;
    [Min(0.0001f)] public float uniformGain = 1f;
    [Range(0f, 30f)] public float positionSmooth = 18f;
    [Range(0f, 30f)] public float rotationSmooth = 18f;
    [Range(0f, 30f)] public float hintSmooth     = 18f;

    [Header("Rotation")]
    public bool useControllerRotation = true;
    public Vector3 leftRotOffsetEuler = Vector3.zero;
    public Vector3 rightRotOffsetEuler = Vector3.zero;

    [Header("Axis Mapping (XR ref -> Spider local)")]
    public AxisPreset axisPreset = AxisPreset.SwapXZ; // bei XR meist sinnvoll
    public Vector3 signs = new Vector3(1f, 1f, 1f);   // ggf. x:-1 für Spiegelung etc.

    [Header("Debug")]
    public bool debugWiggle = false;

    public enum AxisPreset { Same, SwapXZ, SwapXY, SwapYZ }

    // --- lifecycle ---
    void Reset()
    {
        if (!referenceFrame)
        {
            referenceFrame = TryFindRefFrame(leftController) ?? TryFindRefFrame(rightController);
            if (!referenceFrame)
            {
                var go = GameObject.Find("Camera Offset");
                if (go) referenceFrame = go.transform;
            }
        }
    }

    void Start()
    {
        if (!spiderRoot) spiderRoot = GuessSpiderRoot();
        var anim = GetComponentInParent<Animator>();
        if (anim) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    void Update()
    {
        if (!spiderRoot || !referenceFrame) return;

        DriveOne(+1f, leftController,  leftTarget,  leftRotOffsetEuler,  leftHint);
        DriveOne(-1f, rightController, rightTarget, rightRotOffsetEuler, rightHint);
    }

    // --- core ---
    void DriveOne(float xMirror, Transform ctrl, Transform target, Vector3 rotOffEuler, Transform hint)
    {
        if (!target) return;

        // Debug-Kreis ohne Controller
        if (debugWiggle)
        {
            Vector3 baseLocalDbg = BaseOffsetLocal();
            float r = Mathf.Min(maxReachRadius, 0.35f);
            float t = Time.time * 1.6f * xMirror;
            Vector3 spiderLocalDbg = baseLocalDbg + new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r);
            spiderLocalDbg.y = Mathf.Max(minLocalY, spiderLocalDbg.y);
            Vector3 p = spiderRoot.TransformPoint(spiderLocalDbg);
            SmoothTarget(target, p, spiderRoot.rotation * Quaternion.Euler(rotOffEuler));
            if (hint) SmoothHint(hint, p, xMirror);
            return;
        }

        if (!ctrl) return;

        // Positions-Deltas im XR-Referenzrahmen
        Vector3 baseRef = ReachAnchorInRef();
        Vector3 ctrlRef = referenceFrame.InverseTransformPoint(ctrl.position);
        Vector3 deltaRef = ctrlRef - baseRef;

        // Safety: wenn Controller noch am Ursprung „kleben“, nicht fahren
        if (deltaRef.sqrMagnitude < 1e-6f) return;

        // Achs-Swizzle + Vorzeichen
        Vector3 mapped = MapAxes(deltaRef);
        mapped = new Vector3(mapped.x * signs.x, mapped.y * signs.y, mapped.z * signs.z);

        // Links/Rechts-Spiegelung nur seitlich (x in Spider-Local nach Mapping)
        mapped.x *= xMirror;

        // Gains
        mapped = Vector3.Scale(mapped, positionGain) * Mathf.Max(0.0001f, uniformGain);

        // In Spider-Local positionieren & clampen
        Vector3 baseLocal = BaseOffsetLocal();
        Vector3 spiderLocal = baseLocal + mapped;

        // Boden-Clamp (nicht unter minLocalY)
        spiderLocal.y = Mathf.Max(minLocalY, spiderLocal.y);

        // Radius-Clamp
        Vector3 d = spiderLocal - baseLocal;
        float m = d.magnitude;
        if (m > maxReachRadius) spiderLocal = baseLocal + d / m * maxReachRadius;

        Vector3 targetPos = spiderRoot.TransformPoint(spiderLocal);

        // Rotation
        Quaternion targetRot = target.rotation;
        if (useControllerRotation)
        {
            Quaternion relCtrlRot = Quaternion.Inverse(referenceFrame.rotation) * ctrl.rotation;
            targetRot = spiderRoot.rotation * relCtrlRot * Quaternion.Euler(rotOffEuler);
        }

        SmoothTarget(target, targetPos, targetRot);

        if (hint) SmoothHint(hint, targetPos, xMirror);
    }

    // --- helpers ---
    Vector3 ReachAnchorInRef()
    {
        if (reachAnchor) return referenceFrame.InverseTransformPoint(reachAnchor.position);
        Vector3 world = spiderRoot.TransformPoint(baseOffsetInSpider);
        return referenceFrame.InverseTransformPoint(world);
    }

    Vector3 BaseOffsetLocal()
    {
        if (reachAnchor) return spiderRoot.InverseTransformPoint(reachAnchor.position);
        return baseOffsetInSpider;
    }

    Vector3 MapAxes(Vector3 v)
    {
        switch (axisPreset)
        {
            case AxisPreset.SwapXZ: return new Vector3(v.z, v.y, v.x);
            case AxisPreset.SwapXY: return new Vector3(v.y, v.x, v.z);
            case AxisPreset.SwapYZ: return new Vector3(v.x, v.z, v.y);
            default: return v;
        }
    }

    float SmoothFactor(float s) => 1f - Mathf.Exp(-Mathf.Max(0f, s) * Time.deltaTime);

    void SmoothTarget(Transform t, Vector3 pos, Quaternion rot)
    {
        float kp = SmoothFactor(positionSmooth);
        float kr = SmoothFactor(rotationSmooth);
        t.position = Vector3.Lerp(t.position, pos, kp);
        t.rotation = Quaternion.Slerp(t.rotation, rot, kr);
    }

    void SmoothHint(Transform hint, Vector3 targetPos, float xMirror)
    {
        float kh = SmoothFactor(hintSmooth);
        Vector3 hintPos = targetPos + spiderRoot.right * xMirror * 0.15f + spiderRoot.up * 0.05f;
        hint.position = Vector3.Lerp(hint.position, hintPos, kh);
    }

    // --- calibration ---
    [ContextMenu("Calibrate (set reachAnchor to current controllers)")]
    public void Calibrate()
    {
        if (!spiderRoot || !referenceFrame || !leftController || !rightController) return;

        Vector3 l = leftController.position;
        Vector3 r = rightController.position;
        Vector3 mid = (l + r) * 0.5f;

        if (!reachAnchor)
        {
            var go = new GameObject("ReachAnchor (auto)");
            reachAnchor = go.transform;
            reachAnchor.SetParent(spiderRoot, worldPositionStays: true);
        }
        reachAnchor.position = mid;
        reachAnchor.rotation = spiderRoot.rotation;
    }

    // --- wiring ---
    Transform TryFindRefFrame(Transform ctrl)
    {
        if (!ctrl) return null;
        Transform t = ctrl;
        while (t != null)
        {
            if (t.name.Contains("Camera Offset")) return t;
            if (t.name.Contains("XR Origin") || t.name.Contains("XROrigin"))
            {
                var camOff = t.Find("Camera Offset");
                if (camOff) return camOff;
            }
            t = t.parent;
        }
        return null;
    }

    Transform GuessSpiderRoot()
    {
        var anim = GetComponentInParent<Animator>();
        return anim ? anim.transform : transform;
    }
}
