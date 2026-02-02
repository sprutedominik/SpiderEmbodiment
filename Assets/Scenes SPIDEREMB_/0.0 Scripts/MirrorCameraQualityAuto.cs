using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MirrorCameraQualityAuto : MonoBehaviour
{
    [Header("Qualität")]
    [Tooltip("Skalierung der RT-Höhe relativ zur Bildschirmhöhe. 1.2 = 120%.")]
    public float resolutionScale = 1.2f;
    [Tooltip("MSAA der RenderTexture (mobil meist 4).")]
    public int msaaSamples = 4;
    [Tooltip("Mipmaps beruhigen Flimmern bei schrägem Blick.")]
    public bool useMipMaps = true;
    public FilterMode filterMode = FilterMode.Bilinear;

    [Header("FXAA / Post Processing")]
    public bool enableFXAA = true;
    [Tooltip("Erzwingt Post Processing an dieser Kamera.")]
    public bool forcePostProcessing = true;

    [Header("Ziel-Renderer (Spiegel-Fläche)")]
    [Tooltip("Leer lassen = wird automatisch im Self/Child/Parent gesucht.")]
    public Renderer targetRenderer;
    [Tooltip("Nur falls nötig: Textur-Property überschreiben (z. B. _MainTex).")]
    public string overrideTextureProperty = "";

    // intern
    RenderTexture rt;
    string propName = "_BaseMap";
    string stPropName = "_BaseMap_ST";      // Tiling/Offset-Name
    MaterialPropertyBlock mpb;

    // zum Erkennen von Größenänderungen
    Vector3 lastRendererScale = Vector3.zero;
    Vector3 lastRendererSize = Vector3.zero;

    void Awake()
    {
        var cam = GetComponent<Camera>();
        var add = GetComponent<UniversalAdditionalCameraData>();
        if (!add) add = gameObject.AddComponent<UniversalAdditionalCameraData>();

        // Dynamic Resolution (falls Feld vorhanden) deaktivieren
        TrySetAllowDynamicResolution(add, false);

        if (enableFXAA)
        {
            add.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            add.antialiasingQuality = AntialiasingQuality.High;
        }
        if (forcePostProcessing) add.renderPostProcessing = true;

        // Renderer finden (Self -> Child -> Parent)
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (!targetRenderer) targetRenderer = GetComponentInParent<Renderer>();

        // Property-Namen ermitteln
        DetectTextureAndSTProperty(targetRenderer, out propName, out stPropName);

        BuildOrRebuildRT(cam, targetRenderer); // initial
    }

    void Update()
    {
        // Wenn die Spiegelwand skaliert/verändert wurde → RT neu anpassen
        if (targetRenderer)
        {
            var size = targetRenderer.bounds.size;
            var scale = targetRenderer.transform.lossyScale;
            if ((size - lastRendererSize).sqrMagnitude > 1e-4f ||
                (scale - lastRendererScale).sqrMagnitude > 1e-4f)
            {
                BuildOrRebuildRT(GetComponent<Camera>(), targetRenderer);
            }
        }
    }

    // ---- Kern: RT passend zur Spiegel-Fläche erzeugen ----
    void BuildOrRebuildRT(Camera cam, Renderer rend)
    {
        // alte lösen
        if (rt)
        {
            if (cam && cam.targetTexture == rt) cam.targetTexture = null;
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        // Seitenverhältnis der Fläche bestimmen (breit/hoch in Welt)
        float aspect = Mathf.Max(0.1f, CalcRendererAspect(rend));
        lastRendererSize  = rend ? rend.bounds.size : Vector3.zero;
        lastRendererScale = rend ? rend.transform.lossyScale : Vector3.one;

        // Höhe aus ScreenHeight, Breite aus Höhe * Aspect
        int h = Mathf.Max(512, Mathf.RoundToInt(Screen.height * resolutionScale));
        int w = Mathf.Max(512, Mathf.RoundToInt(h * aspect));

        var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.Default, 24)
        {
            msaaSamples     = Mathf.Clamp(msaaSamples, 1, 8),
            sRGB            = true,
            useMipMap       = useMipMaps,
            autoGenerateMips= useMipMaps
        };

        rt = new RenderTexture(desc)
        {
            name       = "MirrorRT_Runtime",
            filterMode = filterMode,
            wrapMode   = TextureWrapMode.Clamp
        };
        rt.Create();
        cam.targetTexture = rt;

        // RT auf genau diesen Renderer legen (kein globales Material ändern)
        if (rend)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetTexture(propName, rt);

            // Tiling/Offset so setzen, dass keine Stauchung passiert (Fill ohne Verzerrung)
            // Da RT-Aspect == Flächen-Aspect ist, reicht 1,1 / 0,0 – sicherheitshalber setzen wir's.
            if (!string.IsNullOrEmpty(stPropName))
                mpb.SetVector(stPropName, new Vector4(1f, 1f, 0f, 0f)); // (tilingX, tilingY, offsetX, offsetY)

            rend.SetPropertyBlock(mpb);
        }
    }

    // Seitenverhältnis der Fläche robust bestimmen (zwei größten Ausdehnungen in der Ebene)
    static float CalcRendererAspect(Renderer r)
    {
        if (!r) return 1f;
        Vector3 size = r.bounds.size;
        float[] d = { Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z) };
        Array.Sort(d);                  // d[2] = größte, d[1] = zweitgrößte
        float width = d[2], height = d[1];
        return Mathf.Max(0.1f, width / Mathf.Max(1e-4f, height));
    }

    // Property-Namen herausfinden (Textur + _ST für Tiling/Offset)
    static void DetectTextureAndSTProperty(Renderer r, out string texProp, out string stProp)
    {
        texProp = "_BaseMap"; stProp = "_BaseMap_ST";
        if (!r || !r.sharedMaterial) return;
        var m = r.sharedMaterial;
        if (m.HasProperty("_BaseMap"))      { texProp = "_BaseMap";      stProp = "_BaseMap_ST";      return; }
        if (m.HasProperty("_BaseColorMap")) { texProp = "_BaseColorMap"; stProp = "_BaseColorMap_ST"; return; }
        if (m.HasProperty("_MainTex"))      { texProp = "_MainTex";      stProp = "_MainTex_ST";      return; }
    }

    // allowDynamicResolution kompatibel setzen (URP-Versionen unterscheiden sich)
    static void TrySetAllowDynamicResolution(UniversalAdditionalCameraData add, bool value)
    {
        var prop = typeof(UniversalAdditionalCameraData)
            .GetProperty("allowDynamicResolution", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite) prop.SetValue(add, value, null);
        // Wenn es die Eigenschaft in deiner URP-Version nicht gibt: einfach ignorieren.
    }

    void OnDisable()
    {
        var cam = GetComponent<Camera>();
        if (rt && cam && cam.targetTexture == rt) cam.targetTexture = null;
        if (rt) { rt.Release(); Destroy(rt); rt = null; }
    }

    void OnDestroy()
    {
        var cam = GetComponent<Camera>();
        if (rt && cam && cam.targetTexture == rt) cam.targetTexture = null;
        if (rt) { rt.Release(); Destroy(rt); rt = null; }
    }
}
