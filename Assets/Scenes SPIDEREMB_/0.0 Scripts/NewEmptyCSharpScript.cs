using UnityEngine;

public sealed class XRAnchorDeltaFollowerAdditive : MonoBehaviour
{
    public Transform target;
    void LateUpdate()
    {
        if (!target) return;
        transform.position = target.position; // Mini-Probe
    }
}
