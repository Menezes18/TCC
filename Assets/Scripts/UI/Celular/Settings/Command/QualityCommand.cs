using UnityEngine;

public class QualityCommand : ISettingCommand
{
    public void Execute(object value)
    {
        if (value is int qualityLevel)
            QualitySettings.SetQualityLevel(qualityLevel);
    }
}