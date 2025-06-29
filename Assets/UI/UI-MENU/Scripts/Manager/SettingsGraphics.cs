// SettingsGraphics.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using ShadowQuality = UnityEngine.ShadowQuality;

public class SettingsGraphics : MonoBehaviour
{
    const string KEY_TEXQUAL = "TextureQuality";
    const string KEY_TEXFILTER = "TextureFiltering";
    const string KEY_SHADOWQ = "ShadowQuality";
    const string KEY_DOFLEVEL = "DoFLevel";
    const string KEY_LODBIAS = "LODBias";

    const string KEY_TESSELL = "Tessellation";
    const string KEY_BLOOM = "Bloom";
    const string KEY_MBLUR = "MotionBlur";

    [Header("Selectors — Grafico")]
    public HorizontalSelector textureQualitySelector;
    public HorizontalSelector textureFilteringSelector;
    public HorizontalSelector shadowQualitySelector;
    public HorizontalSelector dofSelector;
    public HorizontalSelector lodSelector;

    [Header("Toggles — Grafico")]
    public Toggle tessellationToggle;
    public Toggle bloomToggle;
    public Toggle motionBlurToggle;

    private Volume globalVolume;
    private DepthOfField dofOverride;
    private Bloom bloomOverride;
    private MotionBlur motionBlurOverride;

    void Awake()
    {
        globalVolume = FindObjectOfType<Volume>();
        if (globalVolume == null)
        {
            Debug.LogError("[SettingsGraphics] nenhum Volume encontrado na cena!");
            return;
        }

        var profile = globalVolume.profile;
        profile.TryGet(out dofOverride);
        profile.TryGet(out bloomOverride);
        profile.TryGet(out motionBlurOverride);
        InitSelector(textureQualitySelector, KEY_TEXQUAL, ApplyTextureQuality,0);
        InitSelector(textureFilteringSelector, KEY_TEXFILTER, ApplyTextureFiltering, textureFilteringSelector.data.Count - 1);
        InitSelector(shadowQualitySelector, KEY_SHADOWQ, ApplyShadowQuality, shadowQualitySelector.data.Count - 1);
        InitSelector(dofSelector, KEY_DOFLEVEL, ApplyDoFLevel, dofSelector.data.Count - 1);
        InitSelector(lodSelector, KEY_LODBIAS, ApplyLODBias, lodSelector.data.Count - 1);
        

        ForceToggleOn(KEY_TESSELL, tessellationToggle, on => Shader.SetGlobalFloat("_TessellationEnabled", on ? 1f : 0f));
        ForceToggleOn(KEY_BLOOM, bloomToggle, on => bloomOverride.active = on);
        ForceToggleOn(KEY_MBLUR, motionBlurToggle, on => motionBlurOverride.active = on);
    }

    void InitSelector(HorizontalSelector sel, string key, Action<int> apply, int defaultIdx)
    {
        int saved = PlayerPrefs.GetInt(key, defaultIdx);
        sel.index = saved;
        apply(saved);
        sel.OnValueChanged += i =>
        {
            PlayerPrefs.SetInt(key, i);
            apply(i);
        };
    }

    void ForceToggleOn(string key, Toggle toggle, Action<bool> apply)
    {
        if (!PlayerPrefs.HasKey(key))
            PlayerPrefs.SetInt(key, 1);
        SettingsBinder.BindToggle(
            key,
            toggle,
            apply
        );
    }
    void ApplyTextureQuality(int idx)
    {
        QualitySettings.globalTextureMipmapLimit = idx;
        Debug.Log($"[Graf] TextureQuality limit = {idx}");
    }

    void ApplyTextureFiltering(int idx)
    {
        switch (idx)
        {
            case 0:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                break;
            case 1:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                break;
            default:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                break;
        }
        Debug.Log($"[Graf] AnisotropicFiltering = {QualitySettings.anisotropicFiltering}");
    }

    void ApplyShadowQuality(int idx)
    {
        switch (idx)
        {
            case 0:
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
            case 1:
                QualitySettings.shadows = ShadowQuality.HardOnly;
                break;
            default:
                QualitySettings.shadows = ShadowQuality.All;
                break;
        }
        Debug.Log($"[Graf] ShadowQuality = {QualitySettings.shadows}");
    }
    

    void ApplyDoFLevel(int idx)
    {
        if (dofOverride != null)
        {
            dofOverride.active = idx > 0;
            dofOverride.focusDistance.value = 1f + idx * 2f;
            Debug.Log($"[Graf] DoF focusDistance = {dofOverride.focusDistance.value}");
        }
    }

    void ApplyLODBias(int idx)
    {
        float[] biases = { 0.5f, 1f, 2f, 4f };
        QualitySettings.lodBias = biases[Mathf.Clamp(idx, 0, biases.Length - 1)];
        Debug.Log($"[Graf] LOD Bias = {QualitySettings.lodBias}");
    }
}
