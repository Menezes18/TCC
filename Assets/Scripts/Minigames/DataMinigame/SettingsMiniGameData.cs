using UnityEngine;

[CreateAssetMenu(fileName = "SettingsMinigame", menuName = "Minigame/SettingsMinigame")]
public class SettingsMiniGameData : ScriptableObject
{
    
    public string miniGameName;
    
    public float miniGameDuration;
    
    [Header("Minigames de rua e corrida")]
    public int maxPoints;
    public int firstPlaceBonus = 50;
    public int secondPlaceBonus = 30;
    public int thirdPlaceBonus = 10;
}
