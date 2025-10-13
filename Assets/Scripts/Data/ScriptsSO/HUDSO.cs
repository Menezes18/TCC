using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{

    public event Action EventOnShowColorChangePanel;

    [SerializeField] private bool _colorChangeOpen;
    public bool ColorChangeOpen => _colorChangeOpen;

    public void ShowColorChangePanel()
    {
        _colorChangeOpen = true;
        this.EventOnShowColorChangePanel?.Invoke();
    }
    
    public event Action EventOnHideColorChangePanel;
    
    public void HideColorChangePanel(){
        _colorChangeOpen = false;
        this.EventOnHideColorChangePanel?.Invoke();
    }

    public void ToggleColorChangePanel()
    {
        if (_colorChangeOpen) HideColorChangePanel();
        else ShowColorChangePanel();
    }
    
    //
    public event Action<float> EventOnSetBlindAlpha;

    public void SetBlindAlpha(float alpha)
    {
        this.EventOnSetBlindAlpha?.Invoke(alpha);
    }

    public event Action<float> EventOnFreezeTimerUpdated;
    
    public void FreezeTimerUpdated(float value) {this.EventOnFreezeTimerUpdated?.Invoke(value);}
    public event Action<float> EventOnPrepareTimerUpdated;
    
    public void PrepareTimerUpdate(float value) {this.EventOnPrepareTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnMatchTimerUpdated;
    
    public void MatchTimerUpdate(float value) {this.EventOnMatchTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnRespawnTimerUpdated;
    public void RespawnTimerUpdate(float value) {this.EventOnRespawnTimerUpdated?.Invoke(value);}
    
    public event Action<string> EventOnPotatoHolderUpdated;
    public void PotatoHolderUpdate(string value) { this.EventOnPotatoHolderUpdated?.Invoke(value); }
    //
    
    public event Action<string> EventOnGameOver;
    public void GameOver(string value) {this.EventOnGameOver?.Invoke(value);}    

    // Minigame Selection Panel
    public event Action EventOnShowMinigameSelectionPanel;
    public event Action EventOnHideMinigameSelectionPanel;

    [SerializeField] private bool _minigameSelectionOpen;
    public bool MinigameSelectionOpen => _minigameSelectionOpen;

    public void ShowMinigameSelectionPanel()
    {
        _minigameSelectionOpen = true;
        this.EventOnShowMinigameSelectionPanel?.Invoke();
    }

    public void HideMinigameSelectionPanel()
    {
        _minigameSelectionOpen = false;
        this.EventOnHideMinigameSelectionPanel?.Invoke();
    }

    public void ToggleMinigameSelectionPanel()
    {
        if (_minigameSelectionOpen) HideMinigameSelectionPanel();
        else ShowMinigameSelectionPanel();
    }
}

