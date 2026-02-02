using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[AddComponentMenu("XR Comfort/Force Renderer For Cameras")]
public class ForceRendererForCameras : MonoBehaviour
{
    [Header("Zielkamera & Renderer-Zuweisung")]
    [Tooltip("Nur diese Kamera soll Tunneling/PostFX bekommen.")]
    public Camera targetCamera;

    [Tooltip("Renderer-Index im URP-Asset für die Zielkamera (mit Tunneling/PostFX).")]
    public int mainRendererIndex = 0;

    [Tooltip("Renderer-Index im URP-Asset für alle anderen Kameras (ohne Tunneling/PostFX).")]
    public int otherRendererIndex = 1;

    [Header("Heuristik für Offscreen/Spiegelkameras")]
    [Tooltip("Auch Kameras mit RenderTexture (Reflection/Planar) automatisch auf 'otherRendererIndex' zwingen.")]
    public bool forceForRenderTextures = true;

    [Tooltip("Optional: Namen enthält dieses Fragment → als Spiegelkamera behandeln.")]
    public string nameContainsHint = "Reflect";

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == null) return;

        var acd = cam.GetUniversalAdditionalCameraData();
        if (acd == null) return;

        // Zielkamera → Renderer A (mit Tunneling/PostFX)
        if (cam == targetCamera)
        {
            if (acd.SetRendererSafe(mainRendererIndex))
                acd.renderPostProcessing = true; // falls du noch andere PostFX willst
            return;
        }

        // Heuristiken: alles andere → Renderer B (ohne Tunneling/PostFX)
        bool looksLikeMirror =
            (forceForRenderTextures && cam.targetTexture != null) ||
            (!string.IsNullOrEmpty(nameContainsHint) && cam.name.IndexOf(nameContainsHint, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
            acd.cameraStack.Count > 0 && acd.renderType == CameraRenderType.Overlay;

        if (looksLikeMirror || cam != targetCamera)
        {
            if (acd.SetRendererSafe(otherRendererIndex))
                acd.renderPostProcessing = false; // harte Abriegelung
        }
    }
}

static class URPExt
{
    // Sicheres Setzen des Renderers (Index geprüft)
    public static bool SetRendererSafe(this UniversalAdditionalCameraData acd, int index)
    {
        if (acd == null) return false;
#if UNITY_EDITOR
        // Im Editor kann man den gültigen Bereich nicht direkt abfragen; wir setzen trotzdem.
#endif
        acd.SetRenderer(index);
        return true;
    }

    public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera cam)
    {
        cam.TryGetComponent<UniversalAdditionalCameraData>(out var data);
        return data;
    }
}
