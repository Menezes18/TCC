using UnityEngine;

public class ResolutionCommand : ISettingCommand
{
    public void Execute(object value)
    {
        if (value is Vector2 res)
            Screen.SetResolution((int)res.x, (int)res.y, FullScreenMode.Windowed);
    }
}