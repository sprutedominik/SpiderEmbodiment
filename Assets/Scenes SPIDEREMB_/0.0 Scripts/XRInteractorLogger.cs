using UnityEngine;
using UnityEngine.EventSystems;

public class SliderDebugger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IDragHandler
{
    private string _name;

    void Awake()
    {
        _name = gameObject.name;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnPointerEnter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnPointerExit");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnPointerDown (trigger gedrückt)");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnPointerUp (trigger losgelassen)");
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnInitializePotentialDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[{_name}] OnDrag – Delta: {eventData.delta}");
    }
}
