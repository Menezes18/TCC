using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTextNormal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text targetGraphic;
    public Color normalColor = Color.white;
    public Color hoverColor  = Color.yellow;

    void Awake()
    {
        targetGraphic = GetComponent<Text>();
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