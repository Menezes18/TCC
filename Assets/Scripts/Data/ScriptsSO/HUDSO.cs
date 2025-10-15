using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{

    public event Action EventOnShowColorChangePanel;

    public void ShowColorChangePanel()
    {
        this.EventOnShowColorChangePanel?.Invoke();
    }
    
    public event Action EventOnHideColorChangePanel;
    
    public void HideColorChangePanel(){
        this.EventOnHideColorChangePanel?.Invoke();
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

    public void ShowMinigameSelectionPanel()
    {
        this.EventOnShowMinigameSelectionPanel?.Invoke();
    }

    public void HideMinigameSelectionPanel()
    {
        this.EventOnHideMinigameSelectionPanel?.Invoke();
    }

    public event Action EventOnShowCustomizationPanel;
    public event Action EventOnHideCustomizationPanel;

    public void ShowCustomizationPanel()
    {
        this.EventOnShowCustomizationPanel?.Invoke();
    }

    public void HideCustomizationPanel()
    {
        this.EventOnHideCustomizationPanel?.Invoke();
    }
}

