using UnityEngine;

public class ShowFPSCommand : ISettingCommand
{
    public void Execute(object value)
    {
        bool show = (bool)value;
        SettingsManager.Instance.fpsText.SetActive(show);

        Debug.Log($"Mostrar FPS: {(show ? "Sim" : "Não")}");
    }
}