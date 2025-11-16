using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class SoccerMinigameController : MinigameController
{
    public override bool HandlesInitialSpawns => true;
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
    [SerializeField] private float outOfBoundsY = -20f;   
    [SerializeField] private float resetCooldown = 1.0f;  

    // Estado
    private bool _matchActive;
    private readonly Dictionary<ulong, int> _playerTeam = new(); // 0 ou 1
    private readonly List<ulong> _teamA = new();
    private readonly List<ulong> _teamB = new();

    private int _scoreA;
    private int _scoreB;
    private float _lastGoalAt;
    private float _lastResetAt;
    private bool _teamsAssigned;

    // Pontuações (para scoreboard e resultados)
    private readonly Dictionary<ulong, int> _liveScoresByPlayer = new();
    private readonly Dictionary<ulong, int> _finalPointsByPlayer = new();
    private readonly Dictionary<ulong, UnityAction> _deathHandlerByPlayer = new();
    private readonly Dictionary<uint, int> _playerTeamByNetId = new();
    private readonly HashSet<ulong> _teleportedSids = new();
    private readonly HashSet<uint> _teleportedNids = new();

    private PlayerList PlayerList => PlayerList.singleton;

    // Times replicados para clientes (para UI identificar)
    public readonly SyncList<ulong> teamAIds = new();
    public readonly SyncList<ulong> teamBIds = new();

    [Header("Player Team Spawns")]
    [SerializeField] private List<Transform> blueSpawns = new();
    [SerializeField] private List<Transform> redSpawns = new();
    private readonly List<Transform> _blueUsed = new();
    private readonly List<Transform> _redUsed = new();

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
        StartCoroutine(ServerPrepareAndTeleportRoutine());
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();

        _matchActive = true;

        _scoreA = 0;
        _scoreB = 0;
        _finalPointsByPlayer.Clear();
        _liveScoresByPlayer.Clear();


        ServerEnsureTeamsAssigned();
        ServerBackfillSteamMappings();
        ServerTeleportPlayersToTeamSpawns(false);

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

        // aplica cores de time nos jogadores (cliente)
        RpcApplySoccerTeamColors();

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

    [ClientRpc]
    private void RpcApplySoccerTeamColors()
    {
        // Para cada jogador presente, encontra um TeamColorMaterialController e aplica a cor do time.
        var allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            var pd = allPlayers[i];
            if (pd == null) continue;
            int team = GetTeamOf(pd.playerInfo.steamId);
            if (team < 0) continue;

            var appliers = pd.GetComponentsInChildren<TeamColorMaterialController>(true);
            for (int j = 0; j < appliers.Length; j++)
            {
                if (appliers[j] != null)
                    appliers[j].ApplyForTeam(team);
            }
        }
    }

    [Server]
    private void ServerBackfillSteamMappings()
    {
        foreach (var pd in PlayerList.players)
        {
            ulong sid = pd.playerInfo.steamId;
            if (sid == 0UL) continue;
            if (_playerTeam.ContainsKey(sid)) continue;
            uint nid = pd.GetComponent<NetworkIdentity>() != null ? pd.GetComponent<NetworkIdentity>().netId : 0u;
            if (nid == 0u) continue;
            if (_playerTeamByNetId.TryGetValue(nid, out var team))
            {
                _playerTeam[sid] = team;
                if (team == 0 && !_teamA.Contains(sid)) _teamA.Add(sid);
                if (team == 1 && !_teamB.Contains(sid)) _teamB.Add(sid);
            }
        }
    }

    [Server]
    private void ServerEnsureTeamsAssigned()
    {
        if (_teamsAssigned && _teamA.Count > 0 && _teamB.Count > 0) return;

        _playerTeam.Clear();
        _playerTeamByNetId.Clear();
        _teamA.Clear();
        _teamB.Clear();
        ServerAssignTeamsRandomly();

        _teamsAssigned = true;
    }

    [Server]
    private void ServerReplicateTeamLists()
    {
        teamAIds.Clear();
        teamBIds.Clear();
        foreach (var sid in _teamA) teamAIds.Add(sid);
        foreach (var sid in _teamB) teamBIds.Add(sid);
    }

    private bool AllPlayerIdsReady()
    {
        var list = PlayerList.players;
        if (list == null || list.Count == 0) return false;
        foreach (var pd in list)
            if (pd.playerInfo.steamId == 0UL)
                return false;
        return true;
    }

    [Server]
    private void ServerTeleportPlayersToTeamSpawns(bool force)
    {
        if (!isServer) return;

        foreach (var pd in PlayerList.players)
        {
            int team = GetTeamOf(pd); // 0 = Azul, 1 = Vermelho
            Transform spawn = ServerGetNextTeamSpawn(team);


            if (spawn == null)
            {
                var mm = MatchManager.singleton;
                if (mm != null)
                    spawn = mm.GetRandomSpawnPoint();
            }
            if (spawn == null)
                spawn = transform;

            var ps = pd.GetComponent<PlayerScript>();
            if (ps == null) continue;
            var conn = pd.GetComponent<NetworkIdentity>()?.connectionToClient;
            if (conn == null) continue;

            ulong sid = pd.playerInfo.steamId;
            uint nid = pd.GetComponent<NetworkIdentity>() != null ? pd.GetComponent<NetworkIdentity>().netId : 0u;
            bool already = (sid != 0 && _teleportedSids.Contains(sid)) || (nid != 0 && _teleportedNids.Contains(nid));
            if (force || !already)
            {
                Debug.Log($"[Soccer] Teleport {pd.alias} (sid={sid}, nid={nid}) team={(team==0?"Azul":team==1?"Vermelho":"?")} to '{spawn.name}' pos={spawn.position}");
                ps.TargetRpcTeleport(conn, spawn.position, spawn.rotation);
                if (sid != 0) _teleportedSids.Add(sid);
                if (nid != 0) _teleportedNids.Add(nid);
            }
        }
    }

    [Server]
    private IEnumerator ServerPrepareAndTeleportRoutine()
    {

        float start = Time.time;
        while (!AllPlayerIdsReady() && Time.time - start < 3f)
            yield return new WaitForSeconds(0.1f);

        ServerEnsureTeamsAssigned();
        ServerBackfillSteamMappings();
        ServerReplicateTeamLists();
        AnnounceTeams();
        _teleportedSids.Clear();
        _teleportedNids.Clear();
        ServerTeleportPlayersToTeamSpawns(true);
        // tentativas adicionais para garantir
        yield return new WaitForSeconds(0.5f);
        ServerBackfillSteamMappings();
        ServerTeleportPlayersToTeamSpawns(false);
        yield return new WaitForSeconds(1.0f);
        ServerTeleportPlayersToTeamSpawns(false);
    }

    private int GetTeamOf(PlayerData pd)
    {
        ulong sid = pd.playerInfo.steamId;
        if (sid != 0 && _playerTeam.TryGetValue(sid, out var tBySid)) return tBySid;
        uint nid = pd.GetComponent<NetworkIdentity>() != null ? pd.GetComponent<NetworkIdentity>().netId : 0u;
        if (nid != 0 && _playerTeamByNetId.TryGetValue(nid, out var tByNid)) return tByNid;
        return -1;
    }

    private Transform ServerGetNextTeamSpawn(int team)
    {
        if (team == 0)
        {
            if (blueSpawns == null || blueSpawns.Count == 0)
                return null;
            var idx = UnityEngine.Random.Range(0, blueSpawns.Count);
            var t = blueSpawns[idx];
            blueSpawns.RemoveAt(idx);
            _blueUsed.Add(t);
            if (blueSpawns.Count == 0)
            {
                blueSpawns = _blueUsed.ToList();
                _blueUsed.Clear();
            }
            return t;
        }
        else if (team == 1)
        {
            if (redSpawns == null || redSpawns.Count == 0)
                return null;
            var idx = UnityEngine.Random.Range(0, redSpawns.Count);
            var t = redSpawns[idx];
            redSpawns.RemoveAt(idx);
            _redUsed.Add(t);
            if (redSpawns.Count == 0)
            {
                redSpawns = _redUsed.ToList();
                _redUsed.Clear();
            }
            return t;
        }
        return null;
    }

    [ContextMenu("Force Team Respawn (Server)")]
    [Server]
    public void ServerForceTeamRespawn()
    {
        ServerEnsureTeamsAssigned();
        _teleportedSids.Clear();
        _teleportedNids.Clear();
        ServerTeleportPlayersToTeamSpawns(true);
        RpcToast("Reposicionando jogadores nos spawns do time…");
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
        ServerOutOfBoundsCheck();
    }

    [Server]
    private void ServerOutOfBoundsCheck()
    {
        if (ball == null) return;
        if (Time.time - _lastResetAt < resetCooldown) return;
        if (Time.time - _lastGoalAt < goalCooldown) return;
        if (ball.transform.position.y < outOfBoundsY)
        {
            _lastResetAt = Time.time;
            ServerResetBall();
        }
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
        var players = PlayerList.players.ToList();
        // shuffle
        for (int i = 0; i < players.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, players.Count);
            (players[i], players[j]) = (players[j], players[i]);
        }
        int half = Mathf.CeilToInt(players.Count / 2f);

        for (int i = 0; i < players.Count; i++)
        {
            var pd = players[i];
            ulong sid = pd.playerInfo.steamId;
            uint nid = pd.GetComponent<NetworkIdentity>() != null ? pd.GetComponent<NetworkIdentity>().netId : 0u;
            int team = (i < half) ? 0 : 1; // 0 = A, 1 = B

            // Map por steamId quando disponível (para scoreboard etc.)
            if (sid != 0UL)
            {
                _playerTeam[sid] = team;
                if (team == 0) _teamA.Add(sid); else _teamB.Add(sid);
            }

            // Map por netId para uso imediato no teleporte
            if (nid != 0u)
            {
                _playerTeamByNetId[nid] = team;
            }
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

        string blueTeam = TeamNames(_teamA);
        string redTeam = TeamNames(_teamB);
        
        // Mostra no HUD grande
        RpcShowTeamAnnouncement(blueTeam, redTeam);
        
        // Também envia no chat como backup
        RpcToast($"Times sorteados!\nAzul: {blueTeam}\nVermelho: {redTeam}");
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
    private void RpcShowTeamAnnouncement(string blueTeamNames, string redTeamNames)
    {
        var hud = FindAnyObjectByType<SoccerHUD>();
        if (hud != null) 
            hud.ShowTeamAnnouncement(blueTeamNames, redTeamNames);
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
