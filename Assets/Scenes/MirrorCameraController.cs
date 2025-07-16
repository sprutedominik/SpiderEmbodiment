using UnityEngine;

public class MirrorCameraController : MonoBehaviour
{
    public Transform target; // Das Objekt, das gespiegelt wird (z. B. Würfel)
    public Transform mirror; // Die Spiegel-Ebene (z. B. dein Quad)

    void LateUpdate()
    {
        if (target == null || mirror == null) return;

        // Spiegel-Ebene: Normal ist die forward-Richtung des Quads
        Vector3 mirrorNormal = mirror.forward;
        Vector3 toTarget = target.position - mirror.position;

        // Position spiegeln
        Vector3 mirroredPosition = Vector3.Reflect(toTarget, mirrorNormal);
        transform.position = mirror.position + mirroredPosition;

        // Richtung spiegeln
        Vector3 reflectedDirection = Vector3.Reflect(target.forward, mirrorNormal);
        transform.rotation = Quaternion.LookRotation(reflectedDirection, Vector3.up);
    }
}
