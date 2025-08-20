using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{

    public event Action EventOnShowPanel;

    public void ShowPanel() => EventOnShowPanel?.Invoke();
    public event Action EventOnShowColorChangePanel;

    public void ShowColorChangePanel()
    {
        Debug.LogError("ShowColorChangePanel");
        this.EventOnShowColorChangePanel?.Invoke();
    }
    
    public event Action EventOnHideColorChangePanel;
    
    public void HideColorChangePanel(){ this.EventOnHideColorChangePanel?.Invoke();}
    
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
    //
    
    public event Action<string> EventOnGameOver;
    public void GameOver(string value) {this.EventOnGameOver?.Invoke(value);}
}

