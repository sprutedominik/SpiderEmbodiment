using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MirrorPlane : MonoBehaviour
{
    public Camera mirrorCamera;
    private RenderTexture renderTexture;

    void Start()
    {
        if (mirrorCamera == null)
        {
            Debug.LogError("MirrorPlane: Mirror Camera not assigned!");
            return;
        }

        // Neue RenderTexture erstellen
        renderTexture = new RenderTexture(Screen.width, Screen.height, 16);
        mirrorCamera.targetTexture = renderTexture;

        // Material dieses Objekts auf RenderTexture setzen
        GetComponent<Renderer>().material.mainTexture = renderTexture;
    }

    void Update()
    {
        if (mirrorCamera == null) return;

        // Spiegelposition: Kamera auf gegenüberliegende Seite setzen
        Vector3 mirrorNormal = transform.forward;
        Vector3 mirrorPos = transform.position;

        Vector3 camDir = Camera.main.transform.forward;
        Vector3 camPos = Camera.main.transform.position;

        Vector3 reflectedDir = Vector3.Reflect(camDir, mirrorNormal);
        Vector3 reflectedPos = camPos - 2 * Vector3.Project(camPos - mirrorPos, mirrorNormal);

        mirrorCamera.transform.position = reflectedPos;
        mirrorCamera.transform.forward = reflectedDir;
    }
}
