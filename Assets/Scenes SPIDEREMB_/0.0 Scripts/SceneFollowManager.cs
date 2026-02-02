// SceneFlowManager.cs
using UnityEngine;

public class SceneFlowManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
