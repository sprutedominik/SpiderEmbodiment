using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderValueDisplay : MonoBehaviour
{
    [Header("Referenzen")]
    public TextMeshProUGUI valueText;

    private Slider _slider;

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    void Start()
    {
        if (valueText == null)
        {
            Debug.LogError("Bitte ValueText im Inspector setzen.");
            return;
        }

        // Alte Listener entfernen und neuen hinzufügen
        _slider.onValueChanged.RemoveAllListeners();
        _slider.onValueChanged.AddListener(OnSliderChanged);

        // Initial einmal updaten
        OnSliderChanged(_slider.value);
    }

    void OnSliderChanged(float rawValue)
    {
        // Normierte Prozentberechnung (0..1 → 0..100)
        float pct = _slider.normalizedValue * 100f;
        int displayValue = Mathf.RoundToInt(pct);
        // Text ohne Prozentzeichen setzen
        valueText.text = displayValue.ToString();
    }
}
