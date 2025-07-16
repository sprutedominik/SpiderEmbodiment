using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderValueHandler : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI valueText;

    void Start()
    {
        UpdateValueText(slider.value);
        slider.onValueChanged.AddListener(UpdateValueText);
    }

    void UpdateValueText(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        valueText.text = intValue.ToString();
    }

    public int GetSliderValue()
    {
        return Mathf.RoundToInt(slider.value);
    }
}
