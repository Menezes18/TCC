using UnityEngine;

[CreateAssetMenu(fileName = "SettingsMinigame", menuName = "Minigame/SettingsMinigame")]
public class SettingsMiniGameData : ScriptableObject
{
    
    public string miniGameName;
    
    public float miniGameDuration;
    
    public int firstPlaceBonus = 50;
    public int secondPlaceBonus = 30;
    public int thirdPlaceBonus = 10;
    public int fourthPlaceBonus = 5;
}
