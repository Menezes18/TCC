using UnityEngine;
using TMPro;

/// <summary>
/// Displays the voting timer on the UI.
/// Subscribe to HUDSO voting timer events to update.
/// </summary>
public class VotingTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerPanel;

    [Header("Settings")]
    [SerializeField] private HUDSO hudso;
    [SerializeField] private string timerFormat = "Tempo de Votação: {0:0}s";
    [SerializeField] private bool autoHideWhenZero = true;

    private void OnEnable()
    {
        if (hudso != null)
        {
            hudso.EventOnVotingTimerUpdated += OnVotingTimerUpdated;
        }
    }

    private void OnDisable()
    {
        if (hudso != null)
        {
            hudso.EventOnVotingTimerUpdated -= OnVotingTimerUpdated;
        }
    }

    private void Start()
    {
        // Start hidden
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    private void OnVotingTimerUpdated(float timeRemaining)
    {
        if (timeRemaining > 0)
        {
            // Show timer
            if (timerPanel != null)
            {
                timerPanel.SetActive(true);
            }

            // Update text
            if (timerText != null)
            {
                timerText.text = string.Format(timerFormat, timeRemaining);
            }
        }
        else if (autoHideWhenZero)
        {
            // Hide when voting ends
            if (timerPanel != null)
            {
                timerPanel.SetActive(false);
            }
        }
    }
}
