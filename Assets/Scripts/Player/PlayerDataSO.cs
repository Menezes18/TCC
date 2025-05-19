using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Player/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
   
   public event Action<int> EventOnColorRequest;

   public void RequestColor(int colorId)
   {
      Debug.Log("Requesting color " + colorId);
      this.EventOnColorRequest?.Invoke(colorId);
   }
}
