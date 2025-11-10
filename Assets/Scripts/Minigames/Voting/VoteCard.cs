using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI component representing a single minigame voting option.
/// Shows the minigame name, icon, and vote count.
/// </summary>
public class VoteCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text voteCountText;
    [SerializeField] private Button voteButton;
    [SerializeField] private GameObject selectedIndicator;

    [Header("Styling")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Sprite defaultIcon;

    private MinigameOptionRuntime _option;
    private int _optionIndex;
    private bool _isSelected;

    public event Action<int> OnVoteClicked;

    private void Awake()
    {
        if (voteButton != null)
        {
            voteButton.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Initializes the vote card with a minigame option.
    /// </summary>
    public void Initialize(MinigameOptionRuntime option, int optionIndex)
    {
        _option = option;
        _optionIndex = optionIndex;

        // Debug: Log what we're receiving
        Debug.Log($"🎴 [VOTE CARD] Initializing card {optionIndex}: ID='{option.id}', DisplayName='{option.displayName}'");

        if (nameText != null)
        {
            string displayText = !string.IsNullOrWhiteSpace(option.displayName) ? option.displayName : option.id;
            nameText.text = displayText;
            Debug.Log($"  📝 Setting text to: '{displayText}'");
        }

        if (iconImage != null)
        {
            iconImage.sprite = option.icon != null ? option.icon : defaultIcon;
        }

        UpdateVoteCount(0);
        SetSelected(false);
    }

    /// <summary>
    /// Updates the displayed vote count.
    /// </summary>
    public void UpdateVoteCount(int count)
    {
        if (voteCountText != null)
        {
            voteCountText.text = count == 1 ? "1 voto" : $"{count} votos";
        }
    }

    /// <summary>
    /// Sets the visual state for selection.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }

        if (iconImage != null)
        {
            iconImage.color = selected ? selectedColor : normalColor;
        }
    }

    private void OnButtonClicked()
    {
        OnVoteClicked?.Invoke(_optionIndex);
    }

    private void OnDestroy()
    {
        if (voteButton != null)
        {
            voteButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
