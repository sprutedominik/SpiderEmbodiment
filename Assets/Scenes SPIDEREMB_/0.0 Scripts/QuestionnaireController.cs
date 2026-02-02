using UnityEngine;
using UnityEngine.UI;

public class QuestionnaireController : MonoBehaviour
{
    [SerializeField] private Slider fearSlider;
    [SerializeField] private Slider embodimentSlider;
    [SerializeField] private Button nextButton;
    [SerializeField] private bool hideButtonAfterClick = true;

    // Gespeicherte Werte bleiben hier drin, bis die Szene entladen wird
    public float LastFear { get; private set; } = -1f;
    public float LastEmbodiment { get; private set; } = -1f;
    public bool HasNewValues { get; private set; } = false;

    // Wird vom Button im Inspector aufgerufen
    public void OnNextClicked()
    {
        LastFear = fearSlider ? fearSlider.value : -1f;
        LastEmbodiment = embodimentSlider ? embodimentSlider.value : -1f;
        HasNewValues = true;

        Debug.Log($"[QuestionnaireController] Saved fear={LastFear}, embodiment={LastEmbodiment}");

        if (hideButtonAfterClick && nextButton)
            nextButton.gameObject.SetActive(false);

        // KEIN Szenenwechsel – der Sequencer entscheidet, wann weitergeschaltet wird
    }

    // Falls der Sequencer abfragen will
    public bool ConsumeNewValuesFlag()
    {
        if (!HasNewValues) return false;
        HasNewValues = false;
        return true;
    }
}
