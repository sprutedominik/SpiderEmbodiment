using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Zu verfolgendes Ziel (z.B. dein Dummy)")]
    [SerializeField] private Transform target;

    [Header("Position relativ zum Ziel")]
    [Tooltip("Y = Höhe über Ziel, Z = Abstand hinter dem Ziel")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -3f);

    // Damit die Kamera erst bewegt wird, nachdem alle anderen Bewegungen (z.B. dein Dummy) abgeschlossen sind
    private void LateUpdate()
    {
        if (target == null) return;

        // 1) Weltposition für den Kamerablock berechnen
        Vector3 desiredPosition = target.position + offset;

        // 2) Diese Komponente (z.B. 'Camera Offset'-GameObject) an die gewünschte Position setzen
        transform.position = desiredPosition;

        // 3) Kamera in Blickrichtung zum Dummy kippen, auf Augenhöhe (Zielposition + Höhe)
        Vector3 lookAtPos = target.position + Vector3.up * offset.y;
        transform.LookAt(lookAtPos);
    }
}
