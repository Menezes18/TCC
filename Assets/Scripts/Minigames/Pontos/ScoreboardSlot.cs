using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardSlot : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] Image colorImage;
    [SerializeField] private Color aliveLabelColor = Color.white;
    [SerializeField] private Color deadLabelColor = Color.red;

    private Color _initialLabelColor = Color.white;

    private void Awake()
    {
        if (pointsText != null)
        {
            _initialLabelColor = pointsText.color;
            if (aliveLabelColor == Color.white)
                aliveLabelColor = _initialLabelColor;
        }
    }

    public void Refresh(int rank, string playerName, string pointsLabel, Color color, bool isAlive, bool tintNameColor, Color nameColor, Color pointsColor)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null)
        {
            nameText.text = playerName;
            if (tintNameColor)
                nameText.color = nameColor;
            else
                nameText.color = _initialLabelColor;
        }
        if (pointsText != null)
        {
            pointsText.text = pointsLabel;
            pointsText.color = pointsColor;
        }
        if (colorImage != null) colorImage.color = color;
    }
    
    // Sobrecarga para manter compatibilidade com código existente
    public void Refresh(int rank, string playerName, string pointsLabel, Color color, bool isAlive, bool tintNameColor, Color nameColor)
    {
        Color pointsColor = isAlive ? aliveLabelColor : deadLabelColor;
        Refresh(rank, playerName, pointsLabel, color, isAlive, tintNameColor, nameColor, pointsColor);
    }
}
