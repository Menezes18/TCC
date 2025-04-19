using UnityEngine;
using Mirror;

public class ShowPingCommand : ISettingCommand
{
    public void Execute(object value)
    {
        Debug.Log("ShowPingCommand");

        bool show = (bool)value;
        SettingsManager.Instance.pingText.SetActive(show);
        SettingsManager.Instance.qualityText.SetActive(show);

        Debug.Log($"Mostrar Ping: {(show ? "Sim" : "Não")}");
    }
}