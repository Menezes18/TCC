using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class SoccerMinigameController : MinigameController
{
    [Header("Settings")]
    [SerializeField] private SettingsMiniGameData settingsData;

    [Header("Field References")] 
    [SerializeField] private BallPhysics ball;
    [SerializeField] private Transform ballSpawn;
    [SerializeField] private GoalTrigger leftGoal;   // gol da esquerda (pertence a um time)
    [SerializeField] private GoalTrigger rightGoal;  // gol da direita (pertence ao outro time)

    [Header("Optional")] 
    [SerializeField] private Database database;
    [Header("Detection")]
    [SerializeField] private float goalCooldown = 0.75f; // anti-double-score

    // Estado
    private bool _matchActive;
    private readonly Dictionary<ulong, int> _playerTeam = new(); // 0 ou 1
    private readonly List<ulong> _teamA = new();
    private readonly List<ulong> _teamB = new();

    private int _scoreA;
    private int _scoreB;
    private float _lastGoalAt;

    // Pontuações (para scoreboard e resultados)
    private readonly Dictionary<ulong, int> _liveScoresByPlayer = new();
    private readonly Dictionary<ulong, int> _finalPointsByPlayer = new();
    private readonly Dictionary<ulong, UnityAction> _deathHandlerByPlayer = new();

    private PlayerList PlayerList => PlayerList.singleton;

    // Times replicados para clientes (para UI identificar)
    public readonly SyncList<ulong> teamAIds = new();
    public readonly SyncList<ulong> teamBIds = new();

    private void Awake()
    {
        // tenta auto-referenciar caso não setado na cena
        if (ball == null) ball = FindAnyObjectByType<BallPhysics>();
        if (ballSpawn == null)
        {
            var t = GameObject.FindWithTag("Respawn");
            if (t != null) ballSpawn = t.transform;
        }
        if (leftGoal == null || rightGoal == null)
        {
            var goals = FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None);
            if (goals != null && goals.Length >= 2)
            {
                leftGoal = goals[0];
                rightGoal = goals[1];
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // garante callbacks dos gols para este controlador
        if (leftGoal != null) leftGoal.BindController(this);
        if (rightGoal != null) rightGoal.BindController(this);

        // zera o placar e scoreboard inicial
        _scoreA = 0;
        _scoreB = 0;
        _liveScoresByPlayer.Clear();
        Notifica();
    }

    [Server]
    public override void SetupMiniGame()
    {
        base.SetupMiniGame();
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();

        _matchActive = true;

        _scoreA = 0;
        _scoreB = 0;
        _playerTeam.Clear();
        _teamA.Clear();
        _teamB.Clear();
        _finalPointsByPlayer.Clear();
        _liveScoresByPlayer.Clear();

        // sorteia os times A/B
        ServerAssignTeamsRandomly();

        // replicar listas de times para clientes
        teamAIds.Clear();
        teamBIds.Clear();
        foreach (var sid in _teamA) teamAIds.Add(sid);
        foreach (var sid in _teamB) teamBIds.Add(sid);

        // feedback dos times
        AnnounceTeams();

        // opcional: reset da bola
        ServerResetBall();

        // inicializa scoreboard (todos com 0 do seu time)
        UpdateLiveScoresFromTeamScores();
        Notifica();
        RpcUpdateSoccerScore(_scoreA, _scoreB);

        // listeners de morte (opcionalmente congelar carrying/efeitos)
        foreach (var pd in PlayerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            var playerScript = pd.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                if (_deathHandlerByPlayer.TryGetValue(playerId, out var prev) && prev != null)
                    playerScript.EventOnDeathServerSide.RemoveListener(prev);

                UnityAction onDeathHandler = () => OnPlayerDeath(pd);
                _deathHandlerByPlayer[playerId] = onDeathHandler;
                playerScript.EventOnDeathServerSide.AddListener(onDeathHandler);
            }
        }
    }

    [Server]
    public override void EndMatch()
    {
        _matchActive = false;

        foreach (var pd in PlayerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            var playerScript = pd.GetComponent<PlayerScript>();
            if (playerScript != null && _deathHandlerByPlayer.TryGetValue(playerId, out var cb) && cb != null)
                playerScript.EventOnDeathServerSide.RemoveListener(cb);
        }
        _deathHandlerByPlayer.Clear();

        // definir pontos finais e anunciar vencedor(es) antes de despachar
        AssignFinalPoints();
        AnnounceWinners();

        base.EndMatch();
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (!_matchActive) return;
        // Fallback para quem usa física própria (sem Rigidbody): valida sobreposição manual
        ServerManualGoalCheck();
    }

    [Server]
    public override void UpdateScores()
    {
        if (!_matchActive) return;

        // placar por time replicado como placar por jogador, para aparecer no ScoreboardUI
        UpdateLiveScoresFromTeamScores();
        Notifica();
    }

    [Server]
    public override void AssignFinalPoints()
    {
        _finalPointsByPlayer.Clear();

        int a = _scoreA;
        int b = _scoreB;

        if (a == b)
        {
            int drawPts = settingsData != null ? settingsData.secondPlaceBonus : 0;
            foreach (var sid in _teamA) _finalPointsByPlayer[sid] = drawPts;
            foreach (var sid in _teamB) _finalPointsByPlayer[sid] = drawPts;
            return;
        }

        bool aWins = a > b;
        int winPts = settingsData != null ? settingsData.firstPlaceBonus : 0;
        int losePts = 0;

        if (aWins)
        {
            foreach (var sid in _teamA) _finalPointsByPlayer[sid] = winPts;
            foreach (var sid in _teamB) _finalPointsByPlayer[sid] = losePts;
        }
        else
        {
            foreach (var sid in _teamB) _finalPointsByPlayer[sid] = winPts;
            foreach (var sid in _teamA) _finalPointsByPlayer[sid] = losePts;
        }
    }

    public override Dictionary<ulong, int> GetResults() =>
        _finalPointsByPlayer.Count > 0 ? _finalPointsByPlayer : _liveScoresByPlayer;

    public override Dictionary<ulong, int> GetLiveScores() => _liveScoresByPlayer;

    // ======== Times / Placar ========
    [Server]
    private void ServerAssignTeamsRandomly()
    {
        var sids = PlayerList.players.Select(p => p.playerInfo.steamId).ToList();
        // shuffle
        for (int i = 0; i < sids.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, sids.Count);
            (sids[i], sids[j]) = (sids[j], sids[i]);
        }
        int half = Mathf.CeilToInt(sids.Count / 2f);

        for (int i = 0; i < sids.Count; i++)
        {
            ulong id = sids[i];
            int team = (i < half) ? 0 : 1; // 0 = A, 1 = B
            _playerTeam[id] = team;
            if (team == 0) _teamA.Add(id); else _teamB.Add(id);
        }
    }

    [Server]
    private void AnnounceTeams()
    {
        string TeamNames(List<ulong> team)
        {
            var names = new List<string>();
            foreach (var sid in team)
            {
                var pd = PlayerList.players.FirstOrDefault(p => p.playerInfo.steamId == sid);
                names.Add(pd != null ? pd.alias : sid.ToString());
            }
            return string.Join(", ", names);
        }

        RpcToast($"Times sorteados!\nAzul: {TeamNames(_teamA)}\nVermelho: {TeamNames(_teamB)}");
    }

    [Server]
    private void AnnounceWinners()
    {
        if (_scoreA == _scoreB)
        {
            RpcToast($"Fim de jogo! Empate {ScoreString()}.");
            return;
        }
        bool aWins = _scoreA > _scoreB;
        string winners = aWins ? TeamString(_teamA) : TeamString(_teamB);
        RpcToast($"Fim de jogo! {ScoreString()}\nVencedores: {winners}");

        string TeamString(List<ulong> tids)
        {
            var names = new List<string>();
            foreach (var sid in tids)
            {
                var pd = PlayerList.players.FirstOrDefault(p => p.playerInfo.steamId == sid);
                names.Add(pd != null ? pd.alias : sid.ToString());
            }
            return string.Join(", ", names);
        }
    }

    private string ScoreString() => $"Azul {_scoreA} x {_scoreB} Vermelho";

    // Helpers de consulta
    public int GetTeamOf(ulong steamId)
    {
        if (teamAIds.Contains(steamId)) return 0;
        if (teamBIds.Contains(steamId)) return 1;
        if (_playerTeam.TryGetValue(steamId, out var t)) return t;
        return -1;
    }
    public string GetTeamName(int team) => team == 0 ? "Azul" : team == 1 ? "Vermelho" : string.Empty;

    [Server]
    private void UpdateLiveScoresFromTeamScores()
    {
        _liveScoresByPlayer.Clear();
        foreach (var pd in PlayerList.players)
        {
            ulong id = pd.playerInfo.steamId;
            int team = _playerTeam.TryGetValue(id, out var t) ? t : 0;
            int teamScore = (team == 0) ? _scoreA : _scoreB;
            _liveScoresByPlayer[id] = teamScore;
        }
    }

    [Server]
    private void ServerManualGoalCheck()
    {
        if (ball == null) return;
        if (Time.time - _lastGoalAt < goalCooldown) return;

        float radius = 0.5f;
        var sc = ball.GetComponent<SphereCollider>();
        if (sc != null)
        {
            float s = Mathf.Max(ball.transform.lossyScale.x, ball.transform.lossyScale.y, ball.transform.lossyScale.z);
            radius = sc.radius * s;
        }
        else
        {
            radius = ball.Radius;
        }

        var hits = Physics.OverlapSphere(ball.transform.position, radius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var gt = hits[i].GetComponentInParent<GoalTrigger>();
            if (gt != null)
            {
                _lastGoalAt = Time.time;
                ServerRegisterGoal(gt.netOwnerTeam);
                break;
            }
        }
    }

    // ======== Eventos de Gol / Bola ========
    [Server]
    public void ServerRegisterGoal(int netOwnerTeam) // time dono da rede; adversário marcou
    {
        if (!_matchActive) return;
        int scoringTeam = (netOwnerTeam == 0) ? 1 : 0;
        if (scoringTeam == 0) _scoreA++; else _scoreB++;

        ulong scorerSid = 0UL;
        if (ball != null)
        {
            scorerSid = ball.GetLastTouchSteamId();
        }

        RpcUpdateSoccerScore(_scoreA, _scoreB);
        RpcShowGoal(scoringTeam, scorerSid);
        RpcToast($"Gol! {ScoreString()}");

        UpdateLiveScoresFromTeamScores();
        Notifica();

        ServerResetBall();
    }

    [Server]
    private void ServerResetBall()
    {
        if (ball != null)
        {
            Vector3 pos = ballSpawn != null ? ballSpawn.position : Vector3.zero;
            ball.ResetBall(pos);
        }
    }

    [ClientRpc]
    private void RpcToast(string msg)
    {
        ChatManager.ShowToastGlobal(msg);
    }

    [ClientRpc]
    private void RpcUpdateSoccerScore(int a, int b)
    {
        var hud = FindAnyObjectByType<SoccerHUD>();
        if (hud != null) hud.SetScore(a, b);
    }

    [ClientRpc]
    private void RpcShowGoal(int team, ulong scorerSid)
    {
        string alias = string.Empty;
        try
        {
            var pd = PlayerList?.players?.FirstOrDefault(p => p.playerInfo.steamId == scorerSid);
            if (pd != null) alias = pd.alias;
        }
        catch { }

        var hud = FindAnyObjectByType<SoccerHUD>();
        if (hud != null) hud.ShowGoal(team, alias);
    }

    [Server]
    private void OnPlayerDeath(PlayerData pd)
    {

    }
}
