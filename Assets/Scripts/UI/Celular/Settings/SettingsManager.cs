using System;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public event Action OnSettingsChanged;

    private Dictionary<SettingType, ISettingCommand> settings = new();

    public GameObject fpsText;
    public GameObject pingText;
    public GameObject qualityText;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterSetting(SettingType type, ISettingCommand command)
    {
        settings[type] = command;
    }

    public void ApplySetting(SettingType type, object value)
    {
        if (settings.TryGetValue(type, out var command))
        {
            command.Execute(value);
            OnSettingsChanged?.Invoke();
        }
    }
}

public enum SettingType
{
    Resolution,
    Quality,
    AntiAliasing,
    VSync,
    DarkMode,
    Language,
    MasterVolume,
    MusicVolume,
    MuteAll,
    Sensitivity,
    InvertY,
    ShowPing,
    ShowFPS
}