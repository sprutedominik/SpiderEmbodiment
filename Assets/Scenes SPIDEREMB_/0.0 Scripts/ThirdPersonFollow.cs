using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [Tooltip("Welches Objekt soll verfolgt werden – z.B. dein Dummy")]
    public Transform followTarget;
    [Tooltip("Abstand hinter dem Target")]
    public float distance = 3f;
    [Tooltip("Höhe über dem Boden")]
    public float height = 1.5f;
    [Tooltip("Gleitwert, je kleiner desto schneller")]
    public float smoothTime = 0.1f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (followTarget == null) return;

        Vector3 desired = followTarget.position
                          - followTarget.forward * distance
                          + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref velocity, smoothTime);

        Vector3 e = transform.rotation.eulerAngles;
        e.y = followTarget.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(e);
    }
}
