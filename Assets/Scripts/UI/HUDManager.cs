using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class HUDManager : MonoBehaviour
{
    [SerializeField] HUDSO HUDSO;

    [SerializeField] TMP_Text _matchTimer, _freezeTimer, _respawnTimer, _gameover;
    
    [Header("Countdown FX")]
    [SerializeField] float spinDuration = 0.45f;
    [SerializeField] Ease spinEase = Ease.OutBack;
    [SerializeField] float popOvershoot = 1.15f; // escala > 1 e volta
    [SerializeField] float popBackDuration = 0.12f;

    int _lastFreezeShown = int.MinValue;
    Sequence _freezeSeq;
    Dictionary<int, Color> _numColors;
    TMP_Text tmp;
    void Start()
    {
        HUDSO.EventOnMatchTimerUpdated += HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated += HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated += HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated += HUDSOOnEventOnRespawnTimerUpdated;
        HUDSO.EventOnGameOver += HUDSOOnEventOnGameOver;

        _matchTimer.text = _freezeTimer.text = _respawnTimer.text = _gameover.text = "";

        _numColors = new Dictionary<int, Color> {
            {5, new Color(1f, 0.36f, 0.67f)}, // rosa
            {4, new Color(0.50f, 0.90f, 0.82f)}, // menta
            {3, new Color(1.00f, 0.84f, 0.30f)}, // amarelo
            {2, new Color(0.50f, 0.83f, 1.00f)}, // azul
            {1, new Color(0.55f, 0.35f, 1.00f)}, // violeta
        };
    }

    void OnDestroy()
    {
        HUDSO.EventOnMatchTimerUpdated -= HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated -= HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated -= HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated -= HUDSOOnEventOnRespawnTimerUpdated;
        HUDSO.EventOnGameOver -= HUDSOOnEventOnGameOver;
    }

    void HUDSOOnEventOnRespawnTimerUpdated(float obj)
    {
        int s = Mathf.RoundToInt(obj);
        if (s == 0) { _respawnTimer.text = ""; return; }
        _respawnTimer.text = "Respawning in " + s + " seconds";
    }

    void HUDSOOnEventOnPrepareTimerUpdated(float obj)
    {
        HandleAnimation(obj);



    }

    void HUDSOOnEventOnFreezeTimerUpdated(float obj)
    {
        HandleAnimation(obj);
    }

    void HandleAnimation(float obj)
    {
        int s = Mathf.RoundToInt(obj);
        if (s == 0)
        {
            _freezeTimer.text = "GO!"; 
            return;
        }
        if (s == -1) { _freezeTimer.text = ""; _lastFreezeShown = int.MinValue; return; }

        _freezeTimer.text = s.ToString();

        if (s != _lastFreezeShown && s >= 1 && s <= 5)
        {
            _lastFreezeShown = s;
            AnimateCountdownTMP(_freezeTimer, s);
        }
    }
    
    void HUDSOOnEventOnMatchTimerUpdated(float obj)
    {
        if (Mathf.RoundToInt(obj) == -1) { _matchTimer.text = ""; return; }
        _matchTimer.text = CustomMath.FormatTimer(obj);





    }

    void HUDSOOnEventOnGameOver(string obj) => _gameover.text = obj;

    void AnimateCountdownTMP(TMP_Text label, int number)
    {
        RectTransform rt = label.rectTransform;

        tmp = label.GetComponent<TMP_Text>();
        
        if (_numColors.TryGetValue(number, out var face))
        {
           
            SetOutline(face);
        }

        _freezeSeq?.Kill();
        rt.DOKill();

        rt.localScale = Vector3.zero;
        rt.localRotation = Quaternion.identity;

        _freezeSeq = DOTween.Sequence()
            .Append(rt.DOScale(popOvershoot, spinDuration * 0.8f).SetEase(spinEase))
            .Join(rt.DORotate(new Vector3(0, 0, -360f), spinDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutCubic))
            .Append(rt.DOScale(1f, popBackDuration).SetEase(Ease.OutQuad));
    }
    
    public void SetOutline(Color color)
    {
        var mat = tmp.fontMaterial;

        mat.EnableKeyword("OUTLINE_ON");

        mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 1);

        tmp.UpdateMeshPadding();
        tmp.havePropertiesChanged = true;
        tmp.SetMaterialDirty();
    }

}