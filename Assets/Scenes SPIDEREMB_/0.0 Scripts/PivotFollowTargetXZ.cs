using UnityEngine;

public class PivotFollowTargetXZ : MonoBehaviour
{
    public Transform target;
    public float height = 0f; // 0 = gleiche Höhe wie target; setze ggf. auf Pivot-Höhe

    void LateUpdate()
    {
        if (!target) return;
        var p = target.position;
        transform.position = new Vector3(p.x, p.y + height, p.z);
        // Rotation bleibt unverändert – du drehst den Pivot separat (z. B. per HeadOrbitSimple)
    }
}
