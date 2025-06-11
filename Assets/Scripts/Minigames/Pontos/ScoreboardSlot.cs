using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardSlot : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] Image colorImage;

    public void Refresh(int rank, string playerName, int points, Color color)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null) nameText.text = playerName;
        if (pointsText != null) pointsText.text = points.ToString();
        if (colorImage != null) colorImage.color = color;
    }
}