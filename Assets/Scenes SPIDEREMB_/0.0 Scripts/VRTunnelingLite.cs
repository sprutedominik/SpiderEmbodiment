using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[AddComponentMenu("XR Comfort/VR Tunneling (Einfach)")]
public class VRTunnelingSimple : MonoBehaviour
{
    [Header("0) Zielkamera & Volume")]
    [Tooltip("Nur diese Kamera bekommt die Vignette.")]
    public Camera targetCamera;
    [Tooltip("Globales Volume in der Szene (mit Vignette-Override).")]
    public Volume globalVolume;

    [Header("1) Trigger")]
    public bool beiBewegung = false;
    public bool beiDrehung = true;

    [Header("2) Referenzen")]
    [Tooltip("Objekt, dessen Position sich bewegt (z. B. XR Rig).")]
    public Transform bewegungsObjekt;
    [Tooltip("Objekt, dessen Yaw die Drehung vorgibt (z. B. XR Rig Parent). Leer = bewegungsObjekt.")]
    public Transform drehObjekt;

    [Header("3) Empfindlichkeit")]
    public float bewegungStart = 0.08f;
    public float bewegungVoll = 1.2f;
    public float drehungStart = 35f;
    public float drehungVoll = 160f;

    [Header("4) Stärke & Glättung")]
    [Range(0f, 1f)] public float maxStaerke = 0.5f;
    public float gleitZeit = 0.08f;

    // intern
    Vignette _vig;
    Vector3 _lastPos, _lastFwd;
    float _vel;
    float _currentIntensity;

    void OnEnable()
    {
        if (!globalVolume || !targetCamera)
        {
            Debug.LogError("[VRTunnelingSimple] Bitte GlobalVolume und Zielkamera zuweisen!");
            enabled = false;
            return;
        }

        if (!globalVolume.profile.TryGet(out _vig))
            _vig = globalVolume.profile.Add<Vignette>(true);

        _vig.active = true;
        _vig.color.Override(Color.black);
        _vig.center.Override(new Vector2(0.5f, 0.5f));
        _vig.smoothness.Override(0.8f);
        _vig.rounded.Override(true);
        _vig.intensity.Override(0f);

        var yawRef = drehObjekt ? drehObjekt : bewegungsObjekt;
        _lastPos = bewegungsObjekt ? bewegungsObjekt.position : (yawRef ? yawRef.position : Vector3.zero);
        _lastFwd = yawRef ? Flatten(yawRef.forward) : Vector3.forward;

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        if (_vig != null)
            _vig.intensity.Override(0f);
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // Bewegung
        float speed = 0f;
        if (beiBewegung && bewegungsObjekt)
        {
            speed = (bewegungsObjekt.position - _lastPos).magnitude / dt;
            _lastPos = bewegungsObjekt.position;
        }

        // Drehung
        float yawSpeed = 0f;
        if (beiDrehung)
        {
            var yawRef = drehObjekt ? drehObjekt : bewegungsObjekt;
            if (yawRef)
            {
                var f = Flatten(yawRef.forward);
                yawSpeed = Mathf.Abs(Vector3.SignedAngle(_lastFwd, f, Vector3.up)) / dt;
                _lastFwd = f;
            }
        }

        float tMove = beiBewegung ? Mathf.InverseLerp(bewegungStart, bewegungVoll, speed) : 0f;
        float tTurn = beiDrehung ? Mathf.InverseLerp(drehungStart, drehungVoll, yawSpeed) : 0f;
        float target = Mathf.Clamp01(Mathf.Max(tMove, tTurn)) * maxStaerke;

        _currentIntensity = Mathf.SmoothDamp(_vig.intensity.value, target, ref _vel, gleitZeit);
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == targetCamera)
        {
            // Nur die Zielkamera → Vignette aktiv
            _vig.intensity.Override(_currentIntensity);
        }
        else
        {
            // Alle anderen Kameras (Spiegel etc.) → Vignette aus
            _vig.intensity.Override(0f);
        }
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized;
    }
}
