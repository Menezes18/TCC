using UnityEngine;


public class SensitivityCommand : ISettingCommand
{
    public void Execute(object value)
    {
        float normalized  = (float)value;
        
        float minSensitivity = 5f;
        float maxSensitivity = 200f;
        
        float mappedSensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, normalized);
        
        PlayerScript player = GameObject.FindObjectOfType<PlayerScript>();
        if (player == null) return;
        
        player.sensibilidade = mappedSensitivity;
        
        Debug.Log($"Nova sensibilidade definida: {mappedSensitivity}");
    }
}