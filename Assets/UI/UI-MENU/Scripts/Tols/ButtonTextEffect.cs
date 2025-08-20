using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTextEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI targetGraphic;
    public Color normalColor = Color.white;
    public Color hoverColor  = Color.yellow;

    void Awake()
    {
        targetGraphic = GetComponent<TextMeshProUGUI>();
        targetGraphic.color = normalColor;
        OnPointerExit(new PointerEventData(EventSystem.current));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetGraphic.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetGraphic.color = normalColor;
    }
}