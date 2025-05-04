using UnityEngine;
using Mirror;

public class SensitivityCommand : ISettingCommand
{
    public void Execute(object value)
    {
        float normalized = (float)value;
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        var ps = localPlayer.GetComponent<PlayerScript>();
        if (ps == null) return;

        ps.RequestSensitivityChange(normalized);
    }

}