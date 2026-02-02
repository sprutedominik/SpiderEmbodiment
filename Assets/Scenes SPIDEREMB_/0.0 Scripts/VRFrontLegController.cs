using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-50)] // früh, damit Rig später frische Posen liest
public class VRFrontLegController : MonoBehaviour
{
    [Header("Frames")]
    public Transform spiderRoot;       // z.B. "RootBone"
    public Transform referenceFrame;   // XR: "Camera Offset"
    public Transform reachAnchor;      // Empty unter spiderRoot (empf. (0,0.35,0.8))

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("VR Targets (die gelben Marker NUR für die Frontbeine)")]
    public Transform leftFrontLegTarget;
    public Transform rightFrontLegTarget;
    public Transform leftHint;
    public Transform rightHint;

    [Header("EXPLIZITE IK-REFERENZEN (bitte die Front-Constraints hier reinziehen)")]
    public TwoBoneIKConstraint leftFrontIK;
    public TwoBoneIKConstraint rightFrontIK;
    [Tooltip("Optional: werden testweise deaktiviert")]
    public ChainIKConstraint leftFrontChain;
    public ChainIKConstraint rightFrontChain;

    [Header("Workspace (Spider local)")]
    public Vector3 baseOffsetInSpider = new Vector3(0f, 0.35f, 0.80f);
    public Vector3 positionGain = Vector3.one;
    [Min(0.0001f)] public float uniformGain = 1.2f;
    public float maxReachRadius = 1.5f;

    [Header("Rotation")]
    public Vector3 leftRotOffsetEuler = Vector3.zero;
    public Vector3 rightRotOffsetEuler = Vector3.zero;

    [Header("Smoothing (0 = sofort)")]
    [Range(0f, 30f)] public float positionSmooth = 0f;
    [Range(0f, 30f)] public float rotationSmooth = 0f;
    [Range(0f, 30f)] public float hintSmooth     = 0f;

    [Header("Debug")]
    public bool disableChainIK = true;
    public bool forceIKEveryFrame = true;
    public bool debugWiggle = false;     // erzwingt sichtbare Kreisbewegung ohne Controller
    public bool directCopyTest = false;  // 1:1 Controller -> Target (Clamp/Mappings umgehen)
    public bool drawGizmos = true;

    Animator _anim;
    RigBuilder _rb;

    void Awake()
    {
        if (!referenceFrame) referenceFrame = GameObject.Find("Camera Offset")?.transform;
        if (!spiderRoot)
        {
            var a = GetComponentInParent<Animator>();
            spiderRoot = a ? a.transform : transform;
        }
    }

    void Start()
    {
        _anim = GetComponentInParent<Animator>();
        if (_anim) _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        _rb = GetComponentInParent<RigBuilder>();
        if (_rb) { _rb.enabled = true; _rb.Build(); }

        // Explizit: diese IKs sind NUR für die Frontbeine zuständig
        RebindFrontIK(leftFrontIK,  leftFrontLegTarget,  leftHint);
        RebindFrontIK(rightFrontIK, rightFrontLegTarget, rightHint);

        if (disableChainIK)
        {
            if (leftFrontChain)  { leftFrontChain.weight = 0f; leftFrontChain.enabled = false; }
            if (rightFrontChain) { rightFrontChain.weight = 0f; rightFrontChain.enabled = false; }
        }

        Debug.Log($"[VRFrontLegController] SR:{NameOf(spiderRoot)} RF:{NameOf(referenceFrame)} RA:{NameOf(reachAnchor)}  LTarget:{NameOf(leftFrontLegTarget)} RTarget:{NameOf(rightFrontLegTarget)}");
    }

    void Update()
    {
        if (!spiderRoot || !referenceFrame) return;
        if (forceIKEveryFrame)
        {
            ForceIKOn(leftFrontIK);
            ForceIKOn(rightFrontIK);
        }

        DriveOne(+1f, leftController,  leftFrontLegTarget,  leftRotOffsetEuler,  leftHint);
        DriveOne(-1f, rightController, rightFrontLegTarget, rightRotOffsetEuler, rightHint);
    }

    // ---------- Kernbewegung ----------
    Vector3 BaseLocal() =>
        (spiderRoot && reachAnchor) ? spiderRoot.InverseTransformPoint(reachAnchor.position) : baseOffsetInSpider;

    void DriveOne(float xMirror, Transform ctrl, Transform target, Vector3 rotOffEuler, Transform hint)
    {
        if (!target) return;

        // A) Zwangsbewegung
        if (debugWiggle)
        {
            var baseLocal = BaseLocal();
            float r = Mathf.Min(maxReachRadius, 0.4f), t = Time.time * 1.5f;
            var spiderLocal = baseLocal + new Vector3(Mathf.Cos(t)*r*xMirror, 0f, Mathf.Sin(t)*r);
            var p = spiderRoot.TransformPoint(spiderLocal);
            target.position = p;
            target.rotation = spiderRoot.rotation * Quaternion.Euler(rotOffEuler);
            if (hint) hint.position = p + spiderRoot.right * xMirror * 0.15f + spiderRoot.up * 0.05f;
            return;
        }

        if (!ctrl) return;

        // B) Direktkopie (zum Ausschluss)
        if (directCopyTest)
        {
            target.position = ctrl.position;
            target.rotation = ctrl.rotation;
            if (hint) hint.position = target.position + spiderRoot.right * xMirror * 0.15f + spiderRoot.up * 0.05f;
            return;
        }

        // C) Seriöses Mapping
        Vector3 ctrlRef  = referenceFrame.InverseTransformPoint(ctrl.position);
        Vector3 baseRef  = reachAnchor ? referenceFrame.InverseTransformPoint(reachAnchor.position) : Vector3.zero;
        Vector3 deltaRef = ctrlRef - baseRef;
        deltaRef.x *= xMirror;

        Vector3 deltaWorld  = referenceFrame.TransformVector(deltaRef);
        Vector3 deltaSpider = spiderRoot.InverseTransformVector(deltaWorld);
        deltaSpider = Vector3.Scale(deltaSpider, positionGain) * Mathf.Max(0.0001f, uniformGain);

        Vector3 baseLocal2  = BaseLocal();
        Vector3 spiderLocal2 = baseLocal2 + deltaSpider;

        Vector3 d = spiderLocal2 - baseLocal2;
        if (d.sqrMagnitude > maxReachRadius * maxReachRadius)
            spiderLocal2 = baseLocal2 + d.normalized * maxReachRadius;

        Vector3 targetPos = spiderRoot.TransformPoint(spiderLocal2);

        Quaternion relCtrlRot = Quaternion.Inverse(referenceFrame.rotation) * ctrl.rotation;
        Quaternion targetRot  = spiderRoot.rotation * relCtrlRot * Quaternion.Euler(rotOffEuler);

        float kp = 1f - Mathf.Exp(-Mathf.Max(0f, positionSmooth) * Time.deltaTime);
        float kr = 1f - Mathf.Exp(-Mathf.Max(0f, rotationSmooth) * Time.deltaTime);

        target.position = (kp > 0f) ? Vector3.Lerp(target.position, targetPos, kp) : targetPos;
        target.rotation = (kr > 0f) ? Quaternion.Slerp(target.rotation, targetRot, kr) : targetRot;

        if (hint)
        {
            Vector3 side = spiderRoot.right * xMirror;
            Vector3 hintPos = targetPos + side * 0.15f + spiderRoot.up * 0.05f;
            float kh = 1f - Mathf.Exp(-Mathf.Max(0f, hintSmooth) * Time.deltaTime);
            hint.position = (kh > 0f) ? Vector3.Lerp(hint.position, hintPos, kh) : hintPos;
        }
    }

    // ---------- IK Utils ----------
    void RebindFrontIK(TwoBoneIKConstraint tb, Transform tgt, Transform hint)
    {
        if (!tb) return;
        var d = tb.data;
        if (tgt)  d.target = tgt;
        if (hint) d.hint   = hint;
        d.targetPositionWeight = 1f;
        d.targetRotationWeight = 1f;
        d.hintWeight = hint ? 1f : 0f;
        tb.data = d;
        tb.weight = 1f;
        tb.enabled = true;
    }

    void ForceIKOn(TwoBoneIKConstraint tb)
    {
        if (!tb) return;
        tb.enabled = true;
        tb.weight = 1f;
        var d = tb.data;
        d.targetPositionWeight = 1f;
        d.targetRotationWeight = 1f;
        d.hintWeight = d.hint ? 1f : 0f;
        tb.data = d;
    }

    static string NameOf(Transform t) => t ? t.name : "null";

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !spiderRoot) return;
        var baseLocal = BaseLocal();
        var baseWorld = spiderRoot.TransformPoint(baseLocal);
        Gizmos.DrawWireSphere(baseWorld, 0.03f);
        Gizmos.DrawWireSphere(baseWorld, maxReachRadius);
    }
}
