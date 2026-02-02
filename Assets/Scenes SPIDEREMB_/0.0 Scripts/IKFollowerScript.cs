using UnityEngine;

public class IKTargetFollower : MonoBehaviour
{
    public Transform controller;

    void Update()
    {
        if (controller != null)
        {
            transform.position = controller.position;
            transform.rotation = controller.rotation;
        }
    }
}
