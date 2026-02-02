using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[AddComponentMenu("XR Comfort/Vignette Blocker For Cameras")]
public class VignetteBlockerForCameras : MonoBehaviour
{
    [Tooltip("Das Volume, in dem dein Vignette-Override liegt (Global Volume).")]
    public Volume targetVolume;

    [Tooltip("Alle Kameras, die KEINE Vignette rendern sollen (Spiegel/Planar Reflections usw.).")]
    public List<Camera> blockedCameras = new List<Camera>();

    // interner Zugriff auf die Vignette im Volume
    Vignette _vig;

    // Wir merken uns pro Kamera den vorherigen Wert, um sauber zurückzustellen
    readonly Dictionary<Camera, float> _prevIntensity = new Dictionary<Camera, float>();

    void OnEnable()
    {
        if (!targetVolume)
        {
            Debug.LogError("[VignetteBlockerForCameras] Bitte 'Target Volume' zuweisen (dein Global Volume).");
            enabled = false; return;
        }

        if (!targetVolume.profile.TryGet(out _vig))
        {
            Debug.LogError("[VignetteBlockerForCameras] Im zugewiesenen Volume wurde keine Vignette gefunden.");
            enabled = false; return;
        }

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        // Sicherheit: Vignette wieder an den zuletzt sinnvollen Wert setzen
        if (_vig != null)
            _vig.intensity.Override(0f);

        _prevIntensity.Clear();
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (_vig == null || cam == null) return;

        // Wenn diese Kamera blockiert werden soll → Vignette temporär aus
        if (blockedCameras.Contains(cam))
        {
            if (!_prevIntensity.ContainsKey(cam))
                _prevIntensity[cam] = _vig.intensity.value;

            _vig.intensity.Override(0f);
        }
    }

    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (_vig == null || cam == null) return;

        // Nach dem Rendern der blockierten Kamera den Wert wieder freigeben.
        // (Der Hauptkamera-Frame setzt danach ohnehin seinen Zielwert.)
        if (_prevIntensity.TryGetValue(cam, out float prev))
        {
            // Wir setzen auf den Vorwert zurück; dein Tunneling-Script setzt
            // für die MainCam anschließend den gewünschten Wert.
            _vig.intensity.Override(prev);
            _prevIntensity.Remove(cam);
        }
    }
}
