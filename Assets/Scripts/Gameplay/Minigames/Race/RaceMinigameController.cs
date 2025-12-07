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
    
    [Header("Waypoints para Progresso")]
    [SerializeField] private List<Transform> progressWaypoints = new();
    
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
            int progressPercent = Mathf.RoundToInt(prog * 100f);
            _liveScoresByPlayer[id] = progressPercent;
        }

        Notifica();
    }

    [Server]
    public override void AssignFinalPoints()
    {
        _finalPointsByPlayer.Clear();

        var allPlayersRanked = new List<(ulong id, float progress, bool finished)>();
        
        foreach (var finishedId in _finishOrder)
        {
            allPlayersRanked.Add((finishedId, 1f, true));
        }
        
        var nonFinishers = new List<(ulong id, float progress)>();
        foreach (var pd in PlayerList.players)
        {
            ulong id = pd.playerInfo.steamId;
            if (!_finished.Contains(id))
            {
                float prog = GetNormalizedProgress(pd);
                nonFinishers.Add((id, prog));
            }
        }
        nonFinishers.Sort((a, b) => b.progress.CompareTo(a.progress));
        
        foreach (var nf in nonFinishers)
        {
            allPlayersRanked.Add((nf.id, nf.progress, false));
        }

        for (int i = 0; i < allPlayersRanked.Count; i++)
        {
            int points = i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            ulong playerId = allPlayersRanked[i].id;
            _finalPointsByPlayer[playerId] = points;
        }
    }

    public override Dictionary<ulong, int> GetResults()
    {
                return _finalPointsByPlayer;
    }

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

    private float GetNormalizedProgress(PlayerData pd)
    {
        List<Transform> activeWaypoints = GetActiveWaypoints();
        if (activeWaypoints == null || activeWaypoints.Count == 0)
        {
            return 0f;
        }

        Vector3 playerPos = pd.transform.position;
        Vector3 playerPosFlat = new Vector3(playerPos.x, 0f, playerPos.z);

        float closestDistance = float.MaxValue;
        int closestSegmentIndex = -1;
        float closestT = 0f;

        for (int i = 0; i < activeWaypoints.Count - 1; i++)
        {
            if (activeWaypoints[i] == null || activeWaypoints[i + 1] == null) continue;

            Vector3 a = activeWaypoints[i].position;
            Vector3 b = activeWaypoints[i + 1].position;
            Vector3 aFlat = new Vector3(a.x, 0f, a.z);
            Vector3 bFlat = new Vector3(b.x, 0f, b.z);

            Vector3 ab = bFlat - aFlat;
            float segLen = ab.magnitude;
            if (segLen <= 0.0001f) continue;

            Vector3 ap = playerPosFlat - aFlat;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab.normalized) / segLen);
            Vector3 projectedPoint = aFlat + ab.normalized * (t * segLen);
            
            float dist = Vector3.Distance(playerPosFlat, projectedPoint);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestSegmentIndex = i;
                closestT = t;
            }
        }

        if (closestSegmentIndex < 0)
        {
            for (int i = 0; i < activeWaypoints.Count; i++)
            {
                if (activeWaypoints[i] == null) continue;
                float dist = Vector3.Distance(playerPosFlat, new Vector3(activeWaypoints[i].position.x, 0f, activeWaypoints[i].position.z));
                if (dist < 5f) // margem de 5 unidades
                {
                    closestSegmentIndex = i;
                    closestT = 0f;
                    break;
                }
            }
        }

        if (closestSegmentIndex < 0) return 0f;

        float totalProgress = closestSegmentIndex + closestT;
        float maxProgress = activeWaypoints.Count - 1; // número de segmentos
        
        if (maxProgress <= 0) return 0f;
        
        float normalized = Mathf.Clamp01(totalProgress / maxProgress);
        return normalized;
    }

    private List<Transform> GetActiveWaypoints()
    {
        if (progressWaypoints != null && progressWaypoints.Count > 0)
        {
            return progressWaypoints.Where(w => w != null).ToList();
        }
        
        if (checkpoints != null && checkpoints.Count > 0)
        {
            return checkpoints
                .Where(c => c != null)
                .OrderBy(c => c.index)
                .Select(c => c.transform)
                .ToList();
        }
        
        return new List<Transform>();
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

    private void OnDrawGizmos()
    {
        DrawWaypointGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawWaypointGizmos(true);
    }

    private void DrawWaypointGizmos(bool selected)
    {
        List<Transform> activeWaypoints = GetActiveWaypoints();
        
        if (activeWaypoints == null || activeWaypoints.Count == 0) return;

        Gizmos.color = selected ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
        
        if (startReference != null && activeWaypoints.Count > 0 && activeWaypoints[0] != startReference)
        {
            Gizmos.DrawLine(startReference.position, activeWaypoints[0].position);
            Gizmos.color = selected ? Color.green : new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(startReference.position, 0.5f);
        }


        Gizmos.color = selected ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
        for (int i = 0; i < activeWaypoints.Count - 1; i++)
        {
            if (activeWaypoints[i] != null && activeWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    activeWaypoints[i].position,
                    activeWaypoints[i + 1].position
                );
            }
        }

        if (finishTrigger != null && activeWaypoints.Count > 0)
        {
            Transform lastWaypoint = activeWaypoints[activeWaypoints.Count - 1];
            if (lastWaypoint != null && finishTrigger.transform != lastWaypoint)
            {
                Gizmos.DrawLine(
                    lastWaypoint.position,
                    finishTrigger.transform.position
                );
            }
            Gizmos.color = selected ? Color.red : new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(finishTrigger.transform.position, 0.5f);
        }

        for (int i = 0; i < activeWaypoints.Count; i++)
        {
            var wp = activeWaypoints[i];
            if (wp == null) continue;

            Vector3 pos = wp.position;
            
            Gizmos.color = selected ? Color.cyan : new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(pos, 0.4f);
            
            Gizmos.DrawLine(pos, pos + Vector3.up * 2f);
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        
        int totalWaypoints = activeWaypoints.Count;
        for (int i = 0; i < activeWaypoints.Count; i++)
        {
            var wp = activeWaypoints[i];
            if (wp == null) continue;
            
            Vector3 labelPos = wp.position + Vector3.up * 2.5f;
            
            int percent = 0;
            if (totalWaypoints > 1)
            {
                percent = Mathf.RoundToInt((i / (float)(totalWaypoints - 1)) * 100f);
            }
            else if (totalWaypoints == 1)
            {
                percent = 100;
            }
            
            string label = $"WP {i + 1}\n{percent}%";
            UnityEditor.Handles.Label(labelPos, label);
        }

        if (startReference != null && (activeWaypoints.Count == 0 || activeWaypoints[0] != startReference))
        {
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(startReference.position + Vector3.up * 2.5f, "START\n0%");
        }

        if (finishTrigger != null)
        {
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(finishTrigger.transform.position + Vector3.up * 2.5f, "FINISH\n100%");
        }
        #endif
    }
}

