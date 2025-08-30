using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class StreetMinigameController : MinigameController, IObserver
{
    [SerializeField] SettingsMiniGameData settingsData;
    [SerializeField] Database database;

    bool _matchActive;

    readonly Dictionary<ulong, bool> _carryingByPlayer = new(); 
    readonly Dictionary<ulong, int> _deliveriesByPlayer = new();
    Dictionary<ulong, int> _finalPointsByPlayer = new();
    readonly Dictionary<ulong, UnityAction> _deathHandlerByPlayer = new();
    private readonly Dictionary<ulong, StreetCourierZone> _dropoffZoneByPlayer = new();
    private PlayerList playerList => PlayerList.singleton;
    
    public override void SetupMiniGame()
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
        ServerAutoBindDropoffsAndColor();
        _matchActive = true;

        _carryingByPlayer.Clear();
        _deliveriesByPlayer.Clear();

        foreach (var pd in playerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            _carryingByPlayer[playerId] = false;
            _deliveriesByPlayer[playerId] = 0;

            var playerScript = pd.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                if (_deathHandlerByPlayer.TryGetValue(playerId, out var previousDeathHandler) && previousDeathHandler != null)
                {
                    playerScript.EventOnDeathServerSide.RemoveListener(previousDeathHandler);
                }
                UnityAction onDeathHandler = () => OnPlayerDeath(pd);
                _deathHandlerByPlayer[playerId] = onDeathHandler;
                playerScript.EventOnDeathServerSide.AddListener(onDeathHandler);
            }
        }

    // push initial zeroed scoreboard so ranking shows from the beginning
    Notifica();
    }

    [Server]
    public override void EndMatch()
    {
        _matchActive = false;
        AssignFinalPoints();
        foreach (var pd in playerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            var playerScript = pd.GetComponent<PlayerScript>();
            if (playerScript != null && _deathHandlerByPlayer.TryGetValue(playerId, out var onDeathHandler) && onDeathHandler != null)
                playerScript.EventOnDeathServerSide.RemoveListener(onDeathHandler);
        }
        _deathHandlerByPlayer.Clear();
        base.EndMatch();
    }

    public override void UpdateScores() { }

    [Server]
    public void ServerPickup(PlayerData pd)
    {
        if (!_matchActive) return;
        ulong playerId = pd.playerInfo.steamId;
        if (!_carryingByPlayer.ContainsKey(playerId)) _carryingByPlayer[playerId] = false;
        if (!_carryingByPlayer[playerId])
        {
            _carryingByPlayer[playerId] = true;
            Notifica();

            // notify only this player
            if (pd.connectionToClient != null)
                TargetToast(pd.connectionToClient, "Pegou a banana! Leve até sua pilha.");
        }
    }

    [Server]
    public void ServerDropoff(PlayerData pd)
    {
        if (!_matchActive) return;
        ulong playerId = pd.playerInfo.steamId;
        if (!_carryingByPlayer.ContainsKey(playerId)) _carryingByPlayer[playerId] = false;
        if (_carryingByPlayer[playerId])
        {
            _carryingByPlayer[playerId] = false;
            _deliveriesByPlayer[playerId] = _deliveriesByPlayer.TryGetValue(playerId, out var currentDeliveryCount) ? currentDeliveryCount + 1 : 1;
            Notifica();

            // notify only this player
            if (pd.connectionToClient != null)
            {
                int total = _deliveriesByPlayer[playerId];
                int ptsNow = GetProvisionalPointsFor(playerId);
                TargetToast(pd.connectionToClient, $"Entrega! +1 (Total: {total}) — Pontos atuais: {ptsNow}");
            }
        }
    }

    [Server]
    private void OnPlayerDeath(PlayerData pd)
    {
        if (!_matchActive || pd == null) return;
        ulong playerId = pd.playerInfo.steamId;
        if (_carryingByPlayer.ContainsKey(playerId))
            _carryingByPlayer[playerId] = false;
        Notifica();
    }


    [Server]
    public override void AssignFinalPoints()
    {
        if (!isServer) return;
    var rankedByDeliveries = _deliveriesByPlayer
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        _finalPointsByPlayer = new Dictionary<ulong, int>();
        for (int i = 0; i < rankedByDeliveries.Count; i++)
        {
            int pts = i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            ulong playerId = rankedByDeliveries[i].Key;
            _finalPointsByPlayer[playerId] = pts;
        }
        Notifica();
    }

    // Mostrar pontuação final (bônus de colocação) desde o início, com base no ranking atual de entregas
    public override Dictionary<ulong, int> GetLiveScores()
    {
        // calcula prévia de pontos por colocação usando deliveries atuais
        var ranked = _deliveriesByPlayer
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        var preview = new Dictionary<ulong, int>(_deliveriesByPlayer.Count);
        for (int i = 0; i < ranked.Count; i++)
        {
            int pts = i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            preview[ranked[i].Key] = pts;
        }
        return preview;
    }
    public override Dictionary<ulong, int> GetResults() => _finalPointsByPlayer.Count > 0 ? _finalPointsByPlayer : _deliveriesByPlayer;

    int GetProvisionalPointsFor(ulong playerId)
    {
        var ranked = _deliveriesByPlayer
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            if (ranked[i].Key != playerId) continue;
            return i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
        }
        return 0;
    }

    [Server]
    public void ServerRegisterDropoff(ulong playerId, StreetCourierZone dropoffZone)
    {
        if (dropoffZone == null) return;
        _dropoffZoneByPlayer[playerId] = dropoffZone;
        dropoffZone.ServerSetOwner(playerId);
    }

    [Server]
    public bool ServerIsPlayerDropoff(ulong playerId, StreetCourierZone zone)
    {
        return _dropoffZoneByPlayer.TryGetValue(playerId, out var z) && z == zone;
    }

    [Server]
    private void ServerAutoBindDropoffsAndColor()
    {
        var allZones = FindObjectsByType<StreetCourierZone>(FindObjectsSortMode.None);
        var availableDropoffs = new List<StreetCourierZone>();
        foreach (var z in allZones)
            if (z.ZoneType == StreetCourierZoneType.Dropoff && !z.HasOwner)
                availableDropoffs.Add(z);

        if (availableDropoffs.Count == 0) return;

        foreach (var pd in playerList.players)
        {
            ulong playerId = pd.playerInfo.steamId;
            Vector3 ppos = pd.transform.position;

            StreetCourierZone best = null;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < availableDropoffs.Count; i++)
            {
                var dz = availableDropoffs[i];
                float d = (dz.transform.position - ppos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = dz;
                }
            }

            if (best == null) continue;

            ServerRegisterDropoff(playerId, best);
            availableDropoffs.Remove(best);

            // define cor replicada para clientes via SyncVar no zone
            if (database != null && pd.color >= 0 && pd.color < database.playerColors.Count)
            {
                var color = (Color32)database.playerColors[pd.color].color;
                best.ServerSetTint(color);
            }
        }
    }

    [TargetRpc]
    void TargetToast(NetworkConnectionToClient conn, string msg)
    {
    ChatManager.ShowToastGlobal(msg);
    }
}
