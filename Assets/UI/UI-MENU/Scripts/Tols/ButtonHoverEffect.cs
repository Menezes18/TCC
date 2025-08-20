using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image targetGraphic;
    public Color normalColor = Color.white;
    public Color hoverColor  = Color.yellow;

    void Awake()
    {
        targetGraphic = GetComponent<Image>();
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