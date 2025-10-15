using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpectatorListItem : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image iconImage; 
    [SerializeField] private TextMeshProUGUI nameText;


    [Header("Data")]
    [SerializeField] private Database db;


    public void Set(string playerName, int colorIndex, string state)
    {
        if (nameText) nameText.text = string.IsNullOrEmpty(playerName) ? "—" : playerName;


        if (iconImage)
        {
            Color color = Color.white;
            if (db != null)
            {
                color = db.GetColor(colorIndex);
            }
            iconImage.color = color;
            iconImage.sprite = null; 
            iconImage.enabled = true;
        }
    }
}
