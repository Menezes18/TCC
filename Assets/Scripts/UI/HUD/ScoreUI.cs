using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;
    [SerializeField] private TextMeshProUGUI scoreText;

    void Start()
    {
        HUDSO.EventOnScoreUpdated += HUDSOOnScoreUpdated;
    }

    void OnDestroy()
    {
        HUDSO.EventOnScoreUpdated -= HUDSOOnScoreUpdated;
    }

    private void HUDSOOnScoreUpdated(int value)
    {
        if (scoreText != null)
            scoreText.text = value.ToString();
    }
}

