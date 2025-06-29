// SettingsBinder.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public static class SettingsBinder
{
    public static void BindToggle(string key, Toggle toggle, Action<bool> apply)
    {
        bool saved = PlayerPrefs.GetInt(key, toggle.isOn ? 1 : 0) == 1;
        toggle.isOn = saved;
        apply(saved);
        toggle.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetInt(key, v ? 1 : 0);
            apply(v);
        });
    }

    public static void BindSlider(string key, Slider slider, float defaultValue, Action<float> apply)
    {
        float saved = PlayerPrefs.GetFloat(key, defaultValue);
        slider.value = saved;
        apply(saved);
        slider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(key, v);
            apply(v);
        });
    }

    public static void BindDropdown<T>(string key, Dropdown dropdown, IList<T> options, Func<T,string> label, Action<int> apply)
    {
        dropdown.ClearOptions();
        var labels = new List<string>(options.Count);
        for (int i = 0; i < options.Count; i++) 
            labels.Add(label(options[i]));
        dropdown.AddOptions(labels);

        int saved = PlayerPrefs.GetInt(key, 0).Clamp(0, options.Count - 1);
        dropdown.value = saved;
        apply(saved);
        dropdown.onValueChanged.AddListener(i =>
        {
            PlayerPrefs.SetInt(key, i);
            apply(i);
        });
    }

    private static int Clamp(this int v, int min, int max) 
        => v < min ? min : (v > max ? max : v);
}