using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{
    
    public event Action<float> EventOnSetBlindAlpha;

    public void SetBlindAlpha(float alpha)
    {
        this.EventOnSetBlindAlpha?.Invoke(alpha);
    }

    public event Action<float> EventOnPrepareTimerUpdated;
    
    public void PrepareTimerUpdate(float value) {this.EventOnPrepareTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnMatchTimerUpdated;
    
    public void MatchTimerUpdate(float value) {this.EventOnMatchTimerUpdated?.Invoke(value);}
}

