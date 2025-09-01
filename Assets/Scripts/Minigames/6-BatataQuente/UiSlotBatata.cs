using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiSlotBatata : MonoBehaviour
{
    public Image playerColor;
    public TextMeshPro playerName;
    public Database db;

    public void Setup(string name, int color)
    {
        playerColor.
        playerName.text = name;
        playerColor.color = db.GetColor(color);
        
    }
}
