using UnityEngine;

public class FollowSpiderXR : MonoBehaviour
{
    [Tooltip("Root-Transform der Spinne.")]
    public Transform spider;

    [Tooltip("Offset relativ zur Spinne (lokal). z.B. (0,0.6,0) = 1stP, (0,1.2,-2) = 3rdP.")]
    public Vector3 localOffset = new Vector3(0f, 1.2f, -2f);

    [Tooltip("Positions-Nachführung (höher = schneller).")]
    public float positionFollow = 10f;

    [Tooltip("Yaw-Nachführung (höher = schneller).")]
    public float yawFollow = 8f;

    void LateUpdate()
    {
        if (!spider) return;

        // Zielposition: lokaler Offset an der Spinnen-Orientation
        Vector3 targetPos = spider.TransformPoint(localOffset);

        // Weiches Folgen (stabiler als Lerp mit fixem t)
        float posT = 1f - Mathf.Exp(-positionFollow * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, posT);

        // Blickwinkel/Yaw mitnehmen (nur Y-Achse). HMD-Rotation bleibt frei.
        float targetYaw = spider.eulerAngles.y;
        float yawT = 1f - Mathf.Exp(-yawFollow * Time.deltaTime);
        float newYaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, yawT);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }
}
