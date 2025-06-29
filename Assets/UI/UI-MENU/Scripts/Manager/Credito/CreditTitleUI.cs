using TMPro;
using UnityEngine;

public class CreditTitleUI : MonoBehaviour
{
    public TextMeshProUGUI titleTMP;
    
    public void Setup(CreditEntry data)
    {
        titleTMP.text = data.titleText;
        titleTMP.color = data.titleTextColor;
    }
}
