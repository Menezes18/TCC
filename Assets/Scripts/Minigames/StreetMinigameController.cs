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
    readonly Dictionary<ulong, int> _finalScores = new();
    private PlayerList playerList => PlayerList.singleton;

    public override void OnStartServer()
    {
        base.SetupMiniGame();
        Adicionar(this);
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

        foreach (var p in PlayerList.singleton.players)
        {
            ulong id = p.playerInfo.steamId;
            Vector3 pos = p.transform.position;

            if (!_lastProgress.ContainsKey(id))
            {
                _lastProgress[id] = 0f;
                _rawScore [id] = 0f;
                _scores [id] = 0;
            }

            float progress = Vector3.Dot(pos - start, trackVec) / trackSqr;
            progress = Mathf.Clamp01(progress);

            float delta = progress - _lastProgress[id];
            if (delta > 0f)
            {
                _rawScore[id] += delta * settingsMiniGameData.maxPoints;
                _lastProgress[id] = progress;
            }

            _scores[id] = Mathf.FloorToInt(_rawScore[id]);

        }
    }


    public override void AssignFinalPoints()
    {
        _finalScores.Clear();
        
        var ranking = _scores
            .OrderByDescending(kv => kv.Value)
            .Select((kv, index) => new { SteamId = kv.Key, Score = kv.Value, Rank = index + 1 })
            .ToList();
        
        var bonusByRank = new[]
        {
            0,
            settingsMiniGameData.firstPlaceBonus,
            settingsMiniGameData.secondPlaceBonus,
            settingsMiniGameData.thirdPlaceBonus
        };
        
        foreach (var entry  in ranking)
        {
            
            int bonus = entry.Rank < bonusByRank.Length
                ? bonusByRank[entry.Rank]
                : 0;

            int finalScore = entry.Score + bonus;
            _finalScores[entry.SteamId] = finalScore;
        }
    }

    public override Dictionary<ulong, int> GetResults() => _finalScores;
}
