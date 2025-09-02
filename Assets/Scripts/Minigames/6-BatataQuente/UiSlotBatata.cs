using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiSlotBatata : MonoBehaviour
{
    public Image playerColor;
    public TextMeshProUGUI playerName;
    public Database db;

    public void Setup(string name, int color)
    {
        playerName.text = name;
        playerColor.color = db.GetColor(color);
        
    }
}
