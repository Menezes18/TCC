// SettingsMenu.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Runtime.InteropServices;

public class SettingsMenu : MonoBehaviour
{
    // chaves de PlayerPrefs
    const string KEY_DIRECTX = "DirectX12";
    const string KEY_VSYNC = "VSync";
    const string KEY_FULLSCREEN = "FullScreen";
    const string KEY_RES = "Resolution";
    const string KEY_RESMOD = "ResModifier";
    const string KEY_BRIGHT = "Brightness";
    const string KEY_AA = "AntiAliasing";
    const string KEY_REFRESH = "RefreshRate";

    [Header("Toggles")]
    public Toggle directXToggle;
    public Toggle vSyncToggle;
    public Toggle fullscreenToggle;

    [Header("Selectors")]
    public HorizontalSelector resolutionSelector;
    public HorizontalSelector aaSelector;

    [Header("Sliders")]
    public Slider resModifierSlider;
    public Slider brightnessSlider;
    public Volume globalVolume;

    ColorAdjustments colorAdjustments;
    void Awake()
    {
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
            if (globalVolume == null)
                Debug.LogError("[SettingsMenu] Não encontrou nenhum Volume na cena!");
        }

        
        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments) == false)
        {
            Debug.LogError("[SettingsMenu] Profile não tem ColorAdjustments!");
        }
        //DirectX12 On/Off
        SettingsBinder.BindToggle(
            KEY_DIRECTX,
            directXToggle,
            on => Debug.Log("DirectX12 " + (on ? "On" : "Off"))
        );

        //V-Sync
        SettingsBinder.BindToggle(
            KEY_VSYNC,
            vSyncToggle,
            on => {
                QualitySettings.vSyncCount = on ? 1 : 0;
                Debug.Log($"[VSyncToggle] callback: on={on}, vSyncCount={QualitySettings.vSyncCount}");
            }
        );
        //Fullscreen On/Off
        SettingsBinder.BindToggle(
            KEY_FULLSCREEN,
            fullscreenToggle,
            on =>
            {
                var parts = resolutionSelector.value.Split('x');
                int w = int.Parse(parts[0]);
                int h = int.Parse(parts[1]);
                bool full = fullscreenToggle.isOn;

                Screen.fullScreenMode = full
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
                Screen.SetResolution(w, h, full);
                Debug.Log($"Resolução: {w}×{h} • Fullscreen: {full}");
            }
        );


        //Resolução
        InitSelector(resolutionSelector, KEY_RES, ApplyResolution);

        //Render Scale
        SettingsBinder.BindSlider(
            KEY_RESMOD,
            resModifierSlider,
            defaultValue: 1f,
            apply: v => ApplyRenderScale(v)
        );

        //Brilho (luz ambiente)
        // SettingsBinder.BindSlider(
        //     KEY_BRIGHT,
        //     brightnessSlider,
        //     defaultValue: 0f,
        //     apply: v => {
        //         colorAdjustments.postExposure.value = v;
        //         Debug.Log($"[Brightness] postExposure = {v}");
        //     }
        // );
        // float saved = PlayerPrefs.GetFloat(KEY_BRIGHT, 0f);
        // colorAdjustments.postExposure.value = saved;
        // brightnessSlider.value = saved;
        //
        // //Anti-Aliasing (MSAA URP)
        // InitSelector(aaSelector, KEY_AA, ApplyAA);


    }
    
    void InitSelector(HorizontalSelector sel, string key, System.Action<int> apply)
    {
        int saved = PlayerPrefs.GetInt(key, sel.defaultValueIndex);
        sel.index = saved;
        apply(saved);
        sel.OnValueChanged += i =>
        {
            PlayerPrefs.SetInt(key, i);
            apply(i);
        };
    }
    void ApplyResolution(int idx)
    {
        var parts = resolutionSelector.value.Split('x');
        int w = int.Parse(parts[0]);
        int h = int.Parse(parts[1]);
        bool full = fullscreenToggle.isOn;

        Screen.fullScreenMode = full
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        Screen.SetResolution(w, h, full);
        Debug.Log($"Resolução: {w}×{h} • Fullscreen: {full}");
    }

    void ApplyAA(int idx)
    {
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            Debug.LogWarning("URP Asset não encontrado");
            return;
        }
        switch (idx)
        {
            case 0: urp.msaaSampleCount = 1; break;  // Off
            case 1: urp.msaaSampleCount = 2; break;  // 2×
            case 2: urp.msaaSampleCount = 4; break;  // 4×
            case 3: urp.msaaSampleCount = 8; break;  // 8×
            default: Debug.LogWarning("AA inválido: " + idx); break;
        }
        Debug.Log("MSAA: " + urp.msaaSampleCount + "×");
    }
    
    void ApplyRenderScale(float v)
    {
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        urp.renderScale = v;
        Debug.Log(v);
    }
    
}
