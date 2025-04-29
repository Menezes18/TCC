using UnityEngine;


public class SensitivityCommand : ISettingCommand
{
    public void Execute(object value)
    {
        float sensitivity = (float)value;
        // PlayerCamera cam = GameObject.FindObjectOfType<PlayerCamera>();
        // if (cam != null)
        // {
        //     cam.mouseSensitivity = sensitivity;
        // }
        Debug.Log($"Nova sensibilidade definida: {sensitivity}");
    }
}