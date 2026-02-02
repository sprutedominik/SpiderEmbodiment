using UnityEngine;

public class DisableFoveatedRendering : MonoBehaviour
{
    private void Awake()
    {
        // Schalte alle Formen von Foveated Rendering ab
        OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.Off;
        OVRManager.tiledMultiResLevel = OVRManager.TiledMultiResLevel.Off;

        Debug.Log("✅ Foveated Rendering wurde deaktiviert.");

        // Verhindert, dass dieses Objekt beim Szenenwechsel zerstört wird
        DontDestroyOnLoad(gameObject);
    }
}
