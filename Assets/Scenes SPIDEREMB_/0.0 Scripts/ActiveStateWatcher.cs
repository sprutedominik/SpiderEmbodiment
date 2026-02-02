using UnityEngine;

public class ActiveStateWatcher : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log($"[Watcher] {name} wurde deaktiviert! Parent={transform.parent?.name}");
    }
}
