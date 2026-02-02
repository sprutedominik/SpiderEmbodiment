// Attach next to VRFrontLegController. On-screen overlay + console + optional logfile.
using UnityEngine;
using System.Text;
using System.IO;
using UnityEngine.Animations.Rigging;

public class VRFrontLegDeepDebug : MonoBehaviour
{
    [Header("Targets & Mapping")]
    public VRFrontLegController ctrl;

    [Header("Overlay / Logging")]
    public bool showOverlay = true;
    public int overlayFontSize = 14;
    public bool writeLogFile = false;          // file: <persistentDataPath>/FrontLegDebug.txt
    public bool echoToConsole = true;          // echoes overlay text to console
    public float sampleHz = 1f;

    [Header("Gizmos (Editor only)")]
    public bool drawGizmos = true;
    public bool drawAxes = true;
    public float axisLen = 0.2f;
    public float sphereSize = 0.035f;

    [Header("Movement thresholds (m)")]
    public float movedController = 0.02f;
    public float movedTarget = 0.005f;

    [Header("Auto-discovered IK")]
    public TwoBoneIKConstraint leftIK, rightIK;
    public ChainIKConstraint leftChain, rightChain;

    [Header("Auto-discovered core")]
    public RigBuilder rigBuilder;
    public Animator spiderAnimator;

    // last sample
    Vector3 pLC, pRC, pLT, pRT;
    Quaternion rLC, rRC, rLT, rRT;

    StringBuilder sb = new StringBuilder(2048);
    float timer;
    StreamWriter writer;

    void Reset(){ ctrl = GetComponent<VRFrontLegController>(); }

    void Awake()
    {
        if (!ctrl) ctrl = GetComponent<VRFrontLegController>();
        if (ctrl && ctrl.spiderRoot)
        {
            rigBuilder     = ctrl.spiderRoot.GetComponentInParent<RigBuilder>();
            spiderAnimator = ctrl.spiderRoot.GetComponentInParent<Animator>();
        }
        AutoFindIKByTargets();
        if (writeLogFile) OpenWriter();
        SampleNow();
        LogRefsOnce();
    }

    void OnDestroy(){ CloseWriter(); }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / Mathf.Max(1f, sampleHz))
        {
            timer = 0f;
            PollAndReport();
        }
    }

    void PollAndReport()
    {
        var LC = ctrl.leftController;
        var RC = ctrl.rightController;
        var LT = ctrl.leftFrontLegTarget;
        var RT = ctrl.rightFrontLegTarget;
        var RF = ctrl.referenceFrame;
        var SR = ctrl.spiderRoot;
        var RA = ctrl.reachAnchor;

        Vector3 lc = LC ? LC.position : Vector3.zero;
        Vector3 rc = RC ? RC.position : Vector3.zero;
        Vector3 lt = LT ? LT.position : Vector3.zero;
        Vector3 rt = RT ? RT.position : Vector3.zero;
        Quaternion lcr = LC ? LC.rotation : Quaternion.identity;
        Quaternion rcr = RC ? RC.rotation : Quaternion.identity;
        Quaternion ltr = LT ? LT.rotation : Quaternion.identity;
        Quaternion rtr = RT ? RT.rotation : Quaternion.identity;

        float lcMove = (lc - pLC).magnitude;
        float rcMove = (rc - pRC).magnitude;
        float ltMove = (lt - pLT).magnitude;
        float rtMove = (rt - pRT).magnitude;

        // transform into reference & spider frame (just for display)
        Vector3 lcRF = RF ? RF.InverseTransformPoint(lc) : lc;
        Vector3 rcRF = RF ? RF.InverseTransformPoint(rc) : rc;
        Vector3 ltRF = RF ? RF.InverseTransformPoint(lt) : lt;
        Vector3 rtRF = RF ? RF.InverseTransformPoint(rt) : rt;
        Vector3 lcSR = SR ? SR.InverseTransformPoint(lc) : lc;
        Vector3 rcSR = SR ? SR.InverseTransformPoint(rc) : rc;
        Vector3 ltSR = SR ? SR.InverseTransformPoint(lt) : lt;
        Vector3 rtSR = SR ? SR.InverseTransformPoint(rt) : rt;

        // rig weight sum
        float rigWeight = 0f;
        if (rigBuilder != null && rigBuilder.layers != null)
            foreach (var layer in rigBuilder.layers) if (layer.rig) rigWeight += layer.rig.weight;

        // TwoBoneIK weights
        float leftIKW  = leftIK  ? leftIK.weight  : -1f;
        float rightIKW = rightIK ? rightIK.weight : -1f;
        float leftPosW = leftIK  ? leftIK.data.targetPositionWeight : -1f;
        float leftRotW = leftIK  ? leftIK.data.targetRotationWeight : -1f;
        float rightPosW= rightIK ? rightIK.data.targetPositionWeight: -1f;
        float rightRotW= rightIK ? rightIK.data.targetRotationWeight: -1f;
        float leftChainW  = leftChain  ? leftChain.weight  : -1f;
        float rightChainW = rightChain ? rightChain.weight : -1f;

        // simple heuristics
        if (LC && LT && lcMove > movedController && ltMove < movedTarget) Warn("Left controller moved, left target stayed (check position weight / clamp / constraints).");
        if (RC && RT && rcMove > movedController && rtMove < movedTarget) Warn("Right controller moved, right target stayed (check position weight / clamp / constraints).");
        if (leftIK  && leftPosW  < 0.5f) Warn($"Left TwoBoneIK targetPositionWeight={leftPosW:0.##} (should be 1).");
        if (rightIK && rightPosW < 0.5f) Warn($"Right TwoBoneIK targetPositionWeight={rightPosW:0.##} (should be 1).");

        // overlay text
        sb.Length = 0;
        sb.AppendLine("=== VRFrontLegDeepDebug ===");
        sb.AppendLine($"fps:{1f/Mathf.Max(0.00001f, Time.unscaledDeltaTime):0.#}   sampleHz:{sampleHz}");
        sb.AppendLine($"Refs  spiderRoot:{YN(SR)}  refFrame:{YN(RF)}  reach:{YN(RA)}  LC:{YN(LC)}  RC:{YN(RC)}  LT:{YN(LT)}  RT:{YN(RT)}");

        string culling = spiderAnimator ? spiderAnimator.cullingMode.ToString() : "-";
        sb.AppendLine($"RigBuilder:{YN(rigBuilder)}  rigWeightSum:{rigWeight:0.##}  AnimatorCulling:{culling}");

        sb.AppendLine($"TwoBoneIK  L:{YN(leftIK)} w:{leftIKW:0.##} posW:{leftPosW:0.##} rotW:{leftRotW:0.##}   R:{YN(rightIK)} w:{rightIKW:0.##} posW:{rightPosW:0.##} rotW:{rightRotW:0.##}");
        sb.AppendLine($"ChainIK    L:{YN(leftChain)} w:{leftChainW:0.##}  R:{YN(rightChain)} w:{rightChainW:0.##}");
        sb.AppendLine("");
        sb.AppendLine($"Controller L  W:{V(lc)}  RF:{V(lcRF)}  SR:{V(lcSR)}  rot:{E(lcr)}  Δ:{lcMove:0.000}");
        sb.AppendLine($"Controller R  W:{V(rc)}  RF:{V(rcRF)}  SR:{V(rcSR)}  rot:{E(rcr)}  Δ:{rcMove:0.000}");
        sb.AppendLine($"Target     L  W:{V(lt)}  RF:{V(ltRF)}  SR:{V(ltSR)}  rot:{E(ltr)}  Δ:{ltMove:0.000}");
        sb.AppendLine($"Target     R  W:{V(rt)}  RF:{V(rtRF)}  SR:{V(rtSR)}  rot:{E(rtr)}  Δ:{rtMove:0.000}");

        if (RA)
        {
            float clampR = ctrl != null ? ctrl.maxReachRadius : 0f;
            float lDist = LT ? Vector3.Distance(LT.position, RA.position) : 0f;
            float rDist = RT ? Vector3.Distance(RT.position, RA.position) : 0f;
            sb.AppendLine($"ReachAnchor W:{V(RA.position)}  Radius:{clampR:0.###}");
            if (LT) sb.AppendLine($"  L dist:{lDist:0.###}{(lDist > clampR ? "  (OUTSIDE!)" : "")}");
            if (RT) sb.AppendLine($"  R dist:{rDist:0.###}{(rDist > clampR ? "  (OUTSIDE!)" : "")}");
        }

        if (echoToConsole) Debug.Log("[VRFrontLegDeepDebug]\n" + sb.ToString());
        Write(sb.ToString());

        // keep last sample
        pLC = lc; pRC = rc; pLT = lt; pRT = rt;
        rLC = lcr; rRC = rcr; rLT = ltr; rRT = rtr;
    }

    void OnGUI()
    {
        if (!showOverlay) return;
        int old = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = overlayFontSize;
        GUI.Label(new Rect(8, 8, Screen.width - 16, Screen.height - 16), sb.ToString());
        GUI.skin.label.fontSize = old;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || ctrl == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.7f);
        if (ctrl.leftFrontLegTarget)  Gizmos.DrawSphere(ctrl.leftFrontLegTarget.position, sphereSize);
        if (ctrl.rightFrontLegTarget) Gizmos.DrawSphere(ctrl.rightFrontLegTarget.position, sphereSize);
        if (ctrl.leftController)      Gizmos.DrawSphere(ctrl.leftController.position, sphereSize * 0.7f);
        if (ctrl.rightController)     Gizmos.DrawSphere(ctrl.rightController.position, sphereSize * 0.7f);

        if (drawAxes)
        {
            if (ctrl.spiderRoot)    DrawAxes(ctrl.spiderRoot.position,    ctrl.spiderRoot.rotation,    axisLen);
            if (ctrl.leftController)  DrawAxes(ctrl.leftController.position,  ctrl.leftController.rotation,  axisLen);
            if (ctrl.rightController) DrawAxes(ctrl.rightController.position, ctrl.rightController.rotation, axisLen);
            if (ctrl.leftFrontLegTarget)  DrawAxes(ctrl.leftFrontLegTarget.position,  ctrl.leftFrontLegTarget.rotation,  axisLen);
            if (ctrl.rightFrontLegTarget) DrawAxes(ctrl.rightFrontLegTarget.position, ctrl.rightFrontLegTarget.rotation, axisLen);
            if (ctrl.reachAnchor) DrawAxes(ctrl.reachAnchor.position, ctrl.reachAnchor.rotation, axisLen);
        }
        if (ctrl.referenceFrame && drawAxes)
            DrawAxes(ctrl.referenceFrame.position, ctrl.referenceFrame.rotation, axisLen * 1.2f);
    }

    // helpers for overlay/gizmos
    string V(Vector3 v) => $"({v.x:0.###},{v.y:0.###},{v.z:0.###})";
    string E(Quaternion q) => $"({q.eulerAngles.x:0.#},{q.eulerAngles.y:0.#},{q.eulerAngles.z:0.#})";
    string YN(Object o) => o ? "Y" : "-";
    void DrawAxes(Vector3 p, Quaternion q, float len)
    {
        var x = q * Vector3.right * len;
        var y = q * Vector3.up * len;
        var z = q * Vector3.forward * len;
        Gizmos.color = Color.red;   Gizmos.DrawLine(p, p + x);
        Gizmos.color = Color.green; Gizmos.DrawLine(p, p + y);
        Gizmos.color = Color.blue;  Gizmos.DrawLine(p, p + z);
    }

    void OpenWriter(){ try { writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "FrontLegDebug.txt"), false); } catch { } }
    void CloseWriter(){ try { writer?.Flush(); writer?.Close(); } catch { } }
    void Write(string t){ if (writeLogFile && writer != null) { writer.WriteLine(System.DateTime.Now.ToString("HH:mm:ss.fff") + " " + t); writer.Flush(); } }

    void AutoFindIKByTargets()
    {
        if (!ctrl) return;
        var allTB = GameObject.FindObjectsOfType<TwoBoneIKConstraint>(true);
        foreach (var tb in allTB)
        {
            if (ctrl.leftFrontLegTarget  && tb.data.target == ctrl.leftFrontLegTarget)  leftIK  = tb;
            if (ctrl.rightFrontLegTarget && tb.data.target == ctrl.rightFrontLegTarget) rightIK = tb;
        }
        var allCH = GameObject.FindObjectsOfType<ChainIKConstraint>(true);
        foreach (var ch in allCH)
        {
            if (ctrl.leftFrontLegTarget  && ch.data.target == ctrl.leftFrontLegTarget)  leftChain  = ch;
            if (ctrl.rightFrontLegTarget && ch.data.target == ctrl.rightFrontLegTarget) rightChain = ch;
        }
    }

    // tiny utils
    void Warn(string msg) { Debug.LogWarning("[VRFrontLegDeepDebug] " + msg); }
    void Info(string msg) { Debug.Log("[VRFrontLegDeepDebug] " + msg); }

    void SampleNow()
    {
        pLC = ctrl && ctrl.leftController     ? ctrl.leftController.position     : Vector3.zero;
        pRC = ctrl && ctrl.rightController    ? ctrl.rightController.position    : Vector3.zero;
        pLT = ctrl && ctrl.leftFrontLegTarget ? ctrl.leftFrontLegTarget.position : Vector3.zero;
        pRT = ctrl && ctrl.rightFrontLegTarget? ctrl.rightFrontLegTarget.position: Vector3.zero;

        rLC = ctrl && ctrl.leftController     ? ctrl.leftController.rotation     : Quaternion.identity;
        rRC = ctrl && ctrl.rightController    ? ctrl.rightController.rotation    : Quaternion.identity;
        rLT = ctrl && ctrl.leftFrontLegTarget ? ctrl.leftFrontLegTarget.rotation : Quaternion.identity;
        rRT = ctrl && ctrl.rightFrontLegTarget? ctrl.rightFrontLegTarget.rotation: Quaternion.identity;
    }

    void LogRefsOnce()
    {
        Debug.Log("[VRFrontLegDeepDebug] Refs → spiderRoot:" + (ctrl ? ctrl.spiderRoot : null)
            + " refFrame:" + (ctrl ? ctrl.referenceFrame : null)
            + " reach:" + (ctrl ? ctrl.reachAnchor : null)
            + " LC:" + (ctrl ? ctrl.leftController : null)
            + " RC:" + (ctrl ? ctrl.rightController : null)
            + " LT:" + (ctrl ? ctrl.leftFrontLegTarget : null)
            + " RT:" + (ctrl ? ctrl.rightFrontLegTarget : null));
    }
}
