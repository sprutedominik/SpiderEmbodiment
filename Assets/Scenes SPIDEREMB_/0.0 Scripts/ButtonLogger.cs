using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonLogger : MonoBehaviour
{
    private Button _button;

    void Awake()
    {
        // Die Button-Komponente holen
        _button = GetComponent<Button>();
        // Listener auf den onClick-Event legen
        _button.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        Debug.Log("🎉 ButtonLogger: Klick erkannt auf Button");
    }
}
