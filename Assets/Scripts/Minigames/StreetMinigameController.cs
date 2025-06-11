using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class StreetMinigameController : MinigameController, IObserver
{
    [Header("Configuração da Pista")]
    [SerializeField] Transform _startLine;
    [SerializeField] Transform _finishLine;
    [SerializeField] SettingsMiniGameData settingsMiniGameData;
    

    bool _isMatchActive;

    readonly Dictionary<ulong, float> _lastProgress = new();
    readonly Dictionary<ulong, float> _rawScore = new();
    readonly Dictionary<ulong, int> _scores = new();
    Dictionary<ulong, int> _finalScores = new();
    private readonly List<ulong> _finishOrder = new();
    private PlayerList playerList => PlayerList.singleton;
    
    public void SetupMiniGame()
    {
        base.SetupMiniGame();
        
        
    }
    public override void OnStartServer()
    {
        Adicionar(this);
        Notifica();
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();
        _isMatchActive = true;

        _lastProgress.Clear();
        _rawScore.Clear();
        _scores.Clear();

        foreach (var p in playerList.players)
        {
            ulong id = p.playerInfo.steamId;
            _lastProgress[id] = 0f;
            _rawScore[id] = 0f;
            _scores[id] = 0;
        }
    }

    [Server]
    public override void EndMatch()
    {
        _isMatchActive = false;
        AssignFinalPoints();
        base.EndMatch();
    }

    [ServerCallback]
    private void Update()
    {
        if (_isMatchActive)
            UpdateScores();
    }

    public override void UpdateScores()
    {
        Vector3 start = _startLine.position;
        Vector3 finish = _finishLine.position;
        Vector3 trackVec = finish - start;
        float trackSqr = trackVec.sqrMagnitude;

        foreach (var p in playerList.players)
        {
            ulong id = p.playerInfo.steamId;
            Vector3 pos = p.transform.position;

            if (!_lastProgress.ContainsKey(id))
            {
                _lastProgress[id] = 0f;
                _rawScore[id]   = 0f;
                _scores[id]     = 0;
            }

            float progress = Mathf.Clamp01(Vector3.Dot(pos - start, trackVec) / trackSqr);
            float delta    = progress - _lastProgress[id];

            if (delta > 0f)
            {
                _rawScore[id] += delta * settingsMiniGameData.maxPoints;
                _lastProgress[id] = progress;
            }

            if (progress >= 1f && !_finishOrder.Contains(id))
            {
                _finishOrder.Add(id);
                int place = _finishOrder.Count; // 1, 2, 3, …

                int bonus =
                    place == 1 ? settingsMiniGameData.firstPlaceBonus :
                    place == 2 ? settingsMiniGameData.secondPlaceBonus :
                    place == 3 ? settingsMiniGameData.thirdPlaceBonus :
                    0;

                _rawScore[id] += bonus;
            }

            _scores[id] = Mathf.FloorToInt(_rawScore[id]);
        }

        Notifica();
    }


    [Server]
    public override void AssignFinalPoints()
    {
        Notifica();
    }

    // SIM essa porra vai ficar igual 
    public override Dictionary<ulong, int> GetLiveScores() =>
        _scores; 
    public override Dictionary<ulong, int> GetResults() => _scores;
}
