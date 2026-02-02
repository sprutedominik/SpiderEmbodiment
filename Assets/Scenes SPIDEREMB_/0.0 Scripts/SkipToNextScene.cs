// SkipToNextScene.cs
using UnityEngine;
using UnityEngine.UI;

public class SkipToNextScene : MonoBehaviour
{
    [Header("Deinen Next‑Button hierherziehen")]
    public Button nextButton;

    void Start()
    {
#if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
        nextButton.onClick.AddListener(OnNextClicked);
    }

    void OnNextClicked()
    {
        // Der zentrale Controller…
        var seq = FindObjectOfType<SequencedSceneController>();
        if (seq != null)
            seq.LoadNextScene();
    }
}

