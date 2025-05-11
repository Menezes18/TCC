using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{
    
    public event Action<float> EventOnSetBlindAlpha;

    public void SetBlindAlpha(float alpha)
    {
        this.EventOnSetBlindAlpha?.Invoke(alpha);
    }
    

}

