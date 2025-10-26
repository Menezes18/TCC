using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;


public class RaceMinigameController : MinigameController
{
    public static RaceMinigameController singleton;

    [SerializeField] private SettingsMiniGameData settingsData;
    [SerializeField] private Database database;
    [SerializeField] private Transform startReference; // ponto inicial opcional para cálculo de progresso
    [SerializeField] private RaceFinishTrigger finishTrigger;
    [SerializeField] private List<RaceCheckpoint> checkpoints = new();
    [SerializeField] private int maxPoints = 250;
    private bool _matchActive;
    private PlayerList PlayerList => PlayerList.singleton;

    // Estado por jogador
    private readonly Dictionary<ulong, int> _lastCheckpointIndex = new();
    private readonly Dictionary<ulong, Vector3> _respawnPos = new();
    private readonly Dictionary<ulong, Quaternion> _respawnRot = new();
    private readonly Dictionary<ulong, int> _liveScoresByPlayer = new();
    private readonly Dictionary<ulong, int> _finalPointsByPlayer = new();
    private readonly HashSet<ulong> _finished = new();
    private readonly List<ulong> _finishOrder = new();
    private readonly Dictionary<ulong, UnityAction> _deathHandlerByPlayer = new();

    private void Awake()
    {
        singleton = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (checkpoints == null || checkpoints.Count == 0)
            AutoFindCheckpoints();
        if (finishTrigger == null)
            finishTrigger = FindAnyObjectByType<RaceFinishTrigger>();
        if (startReference == null)
            startReference = transform; // fallback
    }

    [Server]
    private void AutoFindCheckpoints()
    {
        checkpoints = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None)
            .OrderBy(c => c.index)
            .ToList();
        foreach (var cp in checkpoints)
            cp.BindController(this);
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();

        _matchActive = true;
        _lastCheckpointIndex.Clear();
        _respawnPos.Clear();
        _respawnRot.Clear();
        _liveScoresByPlayer.Clear();
        _finalPointsByPlayer.Clear();
        _finished.Clear();
        _finishOrder.Clear();

        foreach (var pd in PlayerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            _lastCheckpointIndex[playerId] = -1; // nenhum checkpoint ainda
            _respawnPos[playerId] = pd.transform.position; // respawn inicial = posição no início da partida
            _respawnRot[playerId] = pd.transform.rotation;

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

        Notifica(); // atualiza scoreboard inicial
    }

    [Server]
    public override void EndMatch()
    {
        _matchActive = false;

        // limpar listeners de morte
        foreach (var pd in PlayerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            var playerScript = pd.GetComponent<PlayerScript>();
            if (playerScript != null && _deathHandlerByPlayer.TryGetValue(playerId, out var cb) && cb != null)
                playerScript.EventOnDeathServerSide.RemoveListener(cb);
        }
        _deathHandlerByPlayer.Clear();

        base.EndMatch();
    }

    [Server]
    public override void UpdateScores()
    {
        if (!_matchActive) return;

        _liveScoresByPlayer.Clear();
        foreach (var pd in PlayerList.players)
        {
            ulong id = pd.playerInfo.steamId;
            float prog = GetNormalizedProgress(pd);
            int pts = settingsData != null
                ? Mathf.RoundToInt(prog * Mathf.Max(0, maxPoints))
                : Mathf.RoundToInt(prog * 100f);
            _liveScoresByPlayer[id] = pts;
        }

        Notifica();
    }

    [Server]
    public override void AssignFinalPoints()
    {
        _finalPointsByPlayer.Clear();

        // bônus por posição para quem chegou
        for (int i = 0; i < _finishOrder.Count; i++)
        {
            int bonus = i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            _finalPointsByPlayer[_finishOrder[i]] = bonus;
        }

        // progresso para não-finalistas
        foreach (var pd in PlayerList.players)
        {
            ulong id = pd.playerInfo.steamId;
            if (_finished.Contains(id)) continue;

            float prog = GetNormalizedProgress(pd);
            int pts = Mathf.RoundToInt(prog * Mathf.Max(0, maxPoints));
            _finalPointsByPlayer[id] = pts;
        }

        // Override: pontuação final por colocação (1º..4º), incluindo não-finalistas por progresso
        var ranked = new List<ulong>();
        ranked.AddRange(_finishOrder);
        var nonFinishers = new List<(ulong id, float prog)>();
        foreach (var pd2 in PlayerList.players)
        {
            ulong nid = pd2.playerInfo.steamId;
            if (_finished.Contains(nid)) continue;
            nonFinishers.Add((nid, GetNormalizedProgress(pd2)));
        }
        nonFinishers.Sort((a, b) => b.prog.CompareTo(a.prog));
        foreach (var nf in nonFinishers) ranked.Add(nf.id);
        _finalPointsByPlayer.Clear();
        for (int ri = 0; ri < ranked.Count; ri++)
        {
            int p = ri switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus  ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            _finalPointsByPlayer[ranked[ri]] = p;
        }
    }

    public override Dictionary<ulong, int> GetResults() =>
        _finalPointsByPlayer.Count > 0 ? _finalPointsByPlayer : _liveScoresByPlayer;

    public override Dictionary<ulong, int> GetLiveScores() => _liveScoresByPlayer;

    // ======== API chamada pelos triggers ========
    [Server]
    public void ServerRegisterCheckpoint(PlayerData pd, RaceCheckpoint checkpoint)
    {
        if (!_matchActive || pd == null || checkpoint == null) return;
        ulong id = pd.playerInfo.steamId;

        if (!_lastCheckpointIndex.TryGetValue(id, out var cur) || checkpoint.index > cur)
        {
            _lastCheckpointIndex[id] = checkpoint.index;
            _respawnPos[id] = checkpoint.GetRespawnPosition();
            _respawnRot[id] = checkpoint.GetRespawnRotation();
        }
    }

    [Server]
    public void ServerOnPlayerFinish(PlayerData pd)
    {
        if (!_matchActive || pd == null) return;
        ulong id = pd.playerInfo.steamId;
        if (_finished.Contains(id)) return;
        _finished.Add(id);
        _finishOrder.Add(id);

        // garantir progresso máximo
        _lastCheckpointIndex[id] = checkpoints.Count - 1;
        if (finishTrigger != null)
        {
            _respawnPos[id] = finishTrigger.transform.position;
            _respawnRot[id] = finishTrigger.transform.rotation;
        }

        // informa MatchManager para encerramento quando todos chegarem
        MatchManager.singleton?.AddWinnerPlayer(pd);
    }

    [Server]
    private void OnPlayerDeath(PlayerData pd)
    {
        if (!_matchActive || pd == null) return;
        float delay = database != null ? database.playerRespawnDuration : 2.0f;
        StartCoroutine(ServerRespawnAfter(pd, delay));
    }

    [Server]
    private System.Collections.IEnumerator ServerRespawnAfter(PlayerData pd, float delay)
    {
        yield return new WaitForSeconds(delay);

        ulong id = pd.playerInfo.steamId;
        var ps = pd.GetComponent<PlayerScript>();
        var conn = pd.GetComponent<NetworkIdentity>()?.connectionToClient;
        if (ps != null && conn != null)
        {
            Vector3 pos = _respawnPos.TryGetValue(id, out var p) ? p : pd.transform.position;
            Quaternion rot = _respawnRot.TryGetValue(id, out var r) ? r : pd.transform.rotation;
            ps.TargetRpcTeleport(conn, pos, rot);
            ps.RpcOnRespawn();
        }
    }

    // ======== Cálculo de progresso ========
    private float GetNormalizedProgress(PlayerData pd)
    {
        ulong id = pd.playerInfo.steamId;
        int idx = _lastCheckpointIndex.TryGetValue(id, out var cp) ? cp : -1;

        Transform prev = idx < 0 ? startReference : GetCheckpointTransform(idx);
        Transform next = GetNextTransform(idx);

        if (prev == null && startReference != null) prev = startReference;
        if (next == null)
        {
            // sem próximo: considera finalizado se passou do último checkpoint e existe finish
            return (finishTrigger != null && idx >= checkpoints.Count - 1) ? 1f : 0f;
        }

        Vector3 a = prev.position;
        Vector3 b = next.position;
        Vector3 p = pd.transform.position;
        Vector3 ab = b - a;
        float segLen = ab.magnitude;
        if (segLen <= 0.0001f) return 0f;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab.normalized) / segLen);

        int segmentsCount = checkpoints.Count + (finishTrigger != null ? 1 : 0);
        int completedSegments = Mathf.Max(0, idx + 1); // -1 => 0; 0 => 1; etc.
        float normalized = segmentsCount > 0 ? Mathf.Clamp01((completedSegments + t) / segmentsCount) : 0f;
        return normalized;
    }

    private Transform GetCheckpointTransform(int idx)
    {
        if (idx >= 0 && idx < checkpoints.Count) return checkpoints[idx].transform;
        return null;
    }

    private Transform GetNextTransform(int idx)
    {
        int nextIdx = idx + 1;
        if (nextIdx < checkpoints.Count) return checkpoints[nextIdx].transform;
        if (finishTrigger != null) return finishTrigger.transform;
        return null;
    }
}

