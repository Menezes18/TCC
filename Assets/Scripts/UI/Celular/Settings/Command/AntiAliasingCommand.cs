using UnityEngine;

public class AntiAliasingCommand : ISettingCommand
{
    public void Execute(object value)
    {
        if (value is int aaLevel)
            QualitySettings.antiAliasing = aaLevel;
    }
}