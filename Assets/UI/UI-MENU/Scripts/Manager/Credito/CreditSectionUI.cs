using UnityEngine;
using TMPro;


public class CreditSectionUI : MonoBehaviour
{
    public TextMeshProUGUI titleTMP;
    public TextMeshProUGUI namesTMP;
    
    public void Setup(CreditEntry data)
    {
        
        titleTMP.text = data.sectionTitles;
        namesTMP.text = string.Join("\n", data.names);
    }
}