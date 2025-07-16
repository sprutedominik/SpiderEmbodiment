using UnityEngine;

public class IKTargetFollower : MonoBehaviour
{
    public Transform controller;

    void LateUpdate()
    {
        if (controller != null)
        {
            transform.position = controller.position;
            transform.rotation = controller.rotation;

            // DEBUG TEST
            Debug.Log($"{name} is following {controller.name} at position {controller.position}");
        }
        else
        {
            Debug.LogWarning($"{name} has no controller assigned!");
        }
    }
}
