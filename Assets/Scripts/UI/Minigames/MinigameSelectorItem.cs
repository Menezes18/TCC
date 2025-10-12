using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MinigameSelectorItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Button actionButton;

    [SerializeField] private Image stateEnabledMark;
    [SerializeField] private Image stateDisabledMark;

    [SerializeField] private Color titleEnabledColor = Color.white;
    [SerializeField] private Color titleDisabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color iconEnabledColor = Color.white;
    [SerializeField] private Color iconDisabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private MinigameCatalog.MinigameEntry _entry;
    private string _id; 
    private Action<MinigameSelectorItem, bool> _onToggle;
    private bool _isOn;
    private bool _locked;
    
    public MinigameCatalog.MinigameEntry Entry => _entry;
    public string Id => _entry != null ? _entry.id : _id;

    public void Bind(MinigameCatalog.MinigameEntry entry, bool isOn, Action<MinigameSelectorItem, bool> onToggle)
    {
        _entry = entry;
        _id = entry != null ? entry.id : null;
        _onToggle = onToggle;
        _isOn = isOn;

        if (title != null)
            title.text = string.IsNullOrWhiteSpace(entry.displayName) ? entry.id : entry.displayName;
        if (icon != null)
            icon.sprite = entry.icon;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        UpdateVisuals(_isOn);
        var mgr = MyNetworkManager.manager;
        SetLocked(mgr != null && mgr.startGame);
    }

    public void BindSimple(string id, string displayName, Sprite iconSprite, bool isOn, Action<MinigameSelectorItem, bool> onToggle)
    {
        _entry = null;
        _id = id;
        _onToggle = onToggle;
        _isOn = isOn;

        if (title != null)
            title.text = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        if (icon != null)
            icon.sprite = iconSprite;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        UpdateVisuals(_isOn);
        var mgr = MyNetworkManager.manager;
        SetLocked(mgr != null && mgr.startGame);
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveAllListeners();
    }

    private void OnActionClicked()
    {
        if (_locked)
        {
            Debug.LogWarning("[MinigameSelectorItem] Não é possível alterar após iniciar a partida.");
            return;
        }
        bool newValue = !_isOn;

        var mgr = MyNetworkManager.manager;
        if (NetworkServer.active && mgr != null && !string.IsNullOrWhiteSpace(Id))
        {
            if (newValue) mgr.AdicionarMiniGames(Id);
            else mgr.tirarMiniGames(Id);
            _isOn = newValue;
        }
        else
        {
            Debug.LogWarning("[MinigameSelectorItem] Apenas o host pode alterar a lista de minigames.");
        }

        UpdateVisuals(_isOn);
        _onToggle?.Invoke(this, _isOn);
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (actionButton != null)
            actionButton.interactable = !locked;
    }

    private void UpdateVisuals(bool isOn)
    {
        if (stateEnabledMark != null) stateEnabledMark.gameObject.SetActive(isOn);
        if (stateDisabledMark != null) stateDisabledMark.gameObject.SetActive(!isOn);

        if (title != null)
            title.color = isOn ? titleEnabledColor : titleDisabledColor;
        if (icon != null)
            icon.color = isOn ? iconEnabledColor : iconDisabledColor;
   }
}
