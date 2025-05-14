using System;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] HUDSO HUDSO;
    
    [SerializeField] TMP_Text _matchTimer, _freezeTimer, _respawnTimer;

    private void Start()
    {
        HUDSO.EventOnMatchTimerUpdated += HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated += HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated += HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated += HUDSOOnEventOnRespawnTimerUpdated;
        
        _matchTimer.text = "";
        _freezeTimer.text = "";
        _respawnTimer.text = "";
    }

    private void OnDestroy()
    {
        HUDSO.EventOnMatchTimerUpdated -= HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated -= HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated -= HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated -= HUDSOOnEventOnRespawnTimerUpdated;
    }

    private void HUDSOOnEventOnRespawnTimerUpdated(float obj)
    {
        int s = Mathf.FloorToInt(obj);
        string time = s.ToString();

        if (s == 0){
            _respawnTimer.text = "";
            return;
        }
        
        _respawnTimer.text = "Respawning in" + time + "seconds";
    }


    private void HUDSOOnEventOnPrepareTimerUpdated(float obj)
    {
        int s = Mathf.RoundToInt(obj);

        if (s == -1){
            _freezeTimer.text = "";
            return;
        }
        _freezeTimer.text = s.ToString();
    }
    private void HUDSOOnEventOnFreezeTimerUpdated(float obj)
    {
        int s = Mathf.RoundToInt(obj);
        
        if (s == -1){
            _freezeTimer.text = "";
            return;
        }
        _freezeTimer.text = s.ToString();
    }

    private void HUDSOOnEventOnMatchTimerUpdated(float obj)
    {
        if (Mathf.RoundToInt(obj) == -1){
            _freezeTimer.text = "";
            return;
        }
        _matchTimer.text = CustomMath.FormatTimer(obj);
    }
}
