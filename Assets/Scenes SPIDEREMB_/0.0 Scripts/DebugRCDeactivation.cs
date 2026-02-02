using UnityEngine;

public class DebugRCDeactivation : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogWarning("[DEBUG] Right Controller wurde deaktiviert!", this);
        PrintReason();
    }

    private void PrintReason()
    {
        Transform parent = transform.parent;
        if (parent != null && !parent.gameObject.activeInHierarchy)
        {
            Debug.Log("[DEBUG] Ursache: Parent GameObject '" + parent.name + "' ist deaktiviert.", parent);
        }

        Debug.Log("[DEBUG] Stack Trace:\n" + System.Environment.StackTrace);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log("[DEBUG] Right Controller ist aktuell inaktiv.");
        }
    }
}
