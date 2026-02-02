using UnityEngine;

public class LockTransform : MonoBehaviour
{
    public bool lockPosition = true;
    public bool lockRotation = true;

    Vector3 _worldPos;
    Quaternion _worldRot;

    void Awake()
    {
        _worldPos = transform.position;
        _worldRot = transform.rotation;
    }

    void LateUpdate()
    {
        if (lockPosition) transform.position = _worldPos;
        if (lockRotation) transform.rotation = _worldRot;
    }
}
