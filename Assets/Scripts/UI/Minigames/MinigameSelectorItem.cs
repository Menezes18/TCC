using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinigameSelectorItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Toggle toggle;

    private MinigameCatalog.MinigameEntry _entry;
    private Action<MinigameSelectorItem, bool> _onToggle;
    public MinigameCatalog.MinigameEntry Entry => _entry;

    public void Bind(MinigameCatalog.MinigameEntry entry, bool isOn, Action<MinigameSelectorItem, bool> onToggle)
    {
        _entry = entry;
        _onToggle = onToggle;

        if (title != null)
            title.text = string.IsNullOrWhiteSpace(entry.displayName) ? entry.id : entry.displayName;
        if (icon != null)
            icon.sprite = entry.icon;

        if (toggle != null)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(OnToggled);
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveAllListeners();
    }

    private void OnToggled(bool value)
    {
        _onToggle?.Invoke(this, value);
    }
}

