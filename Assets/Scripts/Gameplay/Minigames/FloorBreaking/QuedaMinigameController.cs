using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class QuedaMinigameController : MinigameController
{
    public UnityEvent finalizar;
    [SerializeField] SettingsMiniGameData settingsData;
    
    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private Dictionary<ulong,int> finalScores = new Dictionary<ulong,int>();
    private readonly HashSet<ulong> disconnectedPlayers = new HashSet<ulong>();
    private bool _matchEnded; 
    
    private PlayerList playerList => PlayerList.singleton;
    [SyncVar] int currentIndex;
    [SyncVar] float timer = 5f;

    public bool _startGame = false;

    public void StartGame()
    {
        _startGame = true;
    }
    public override void StartMatch()
    {
        base.StartMatch();
        CancelInvoke(nameof(AddPlayer));
        alivePlayers = playerList.players.Where(p => p != null).ToList();
        eliminationOrder.Clear();
        finalScores.Clear();
        disconnectedPlayers.Clear();
        _matchEnded = false; 
        Notifica();  
    }
    
    //public override bool UseAliveStatusOnScoreboard => true;
    public override void SetupMiniGame()
    {
        base.SetupMiniGame();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        MyNetworkManager.ServerPlayerDisconnected += OnServerPlayerDisconnected;
    
        alivePlayers  = playerList.players.ToList();   
        eliminationOrder.Clear();
        finalScores.Clear();
        disconnectedPlayers.Clear();
        Notifica();
        
        Debug.Log($"🎲 [QUEDA] Round iniciado com {alivePlayers.Count} jogadores");
        Invoke("AddPlayer", 2f);
    }

    public override void OnStopServer()
    {
        CancelInvoke(nameof(AddPlayer));
        MyNetworkManager.ServerPlayerDisconnected -= OnServerPlayerDisconnected;
        base.OnStopServer();
    }

    [Server]
    private void OnServerPlayerDisconnected(PlayerData player)
    {
        if (player == null || !alivePlayers.Remove(player)) return;
        disconnectedPlayers.Add(player.playerInfo.steamId);
        Notifica();
        if (!_matchEnded && alivePlayers.Count <= 1)
        {
            _matchEnded = true;
            CancelInvoke(nameof(AddPlayer));
            AssignFinalPoints();
            finalizar?.Invoke();
        }
    }

    public void AddPlayer()
    { 
        alivePlayers  = playerList.players.ToList();    
    }
    public override void UpdateScores()
    {
        if (!isServer || !_startGame )
            return;
    }

    [Server]
    public void Eliminate(PlayerData pd)
    {
        if (pd == null || _matchEnded || !alivePlayers.Remove(pd))
        {
            Debug.LogWarning($"[QUEDA] Tentativa de eliminar {(pd != null ? pd.playerInfo.steamId : 0)} após fim da partida - IGNORADO");
            return;
        }
        
        eliminationOrder.Add(pd);
        Debug.LogWarning($"❌ [QUEDA] Eliminado: {pd.playerInfo.steamId}");
        Notifica();
        if (alivePlayers.Count <= 1)
        {
            _matchEnded = true;
            CancelInvoke(nameof(AddPlayer));
            AssignFinalPoints();
            finalizar?.Invoke();
        }
    }
    [Server]
    public override void EndMatch()
    {
        bool shouldNotify = !_matchEnded;
        _matchEnded = true;
        CancelInvoke(nameof(AddPlayer));
        if (shouldNotify) base.EndMatch();
    }
    public override void AssignFinalPoints()
    {
        if (!isServer) return;

        finalScores.Clear();
        int posIndex = 0;

        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            finalScores[winner.playerInfo.steamId] =
                (posIndex == 0) ? settingsData.firstPlaceBonus : 0;
            posIndex++;
        }

        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            var pd = eliminationOrder[i];
            int pts = 0;
            switch (posIndex) // 0=1º, 1=2º, 2=3º, 3=4º
            {
                case 0: pts = settingsData.firstPlaceBonus;  break;
                case 1: pts = settingsData.secondPlaceBonus; break;
                case 2: pts = settingsData.thirdPlaceBonus;  break;
                case 3: pts = settingsData.fourthPlaceBonus; break;
                default: pts = 0; break;
            }
            finalScores[pd.playerInfo.steamId] = pts;
            posIndex++;
        }
        foreach (ulong id in disconnectedPlayers)
            finalScores[id] = 0;
    }

    

    public override Dictionary<ulong,int> GetResults() => finalScores;
    public override Dictionary<ulong,int> GetLiveScores()
    {
        var live = new Dictionary<ulong,int>();
        int baseScore = alivePlayers.Count + eliminationOrder.Count;

        foreach (var pd in alivePlayers)
            live[pd.playerInfo.steamId] = baseScore;

        for (int i = 0; i < eliminationOrder.Count; i++)
        {
            var pd = eliminationOrder[i];
            live[pd.playerInfo.steamId] = baseScore - (i + 1);
        }
        foreach (ulong id in disconnectedPlayers)
            live[id] = 0;

        return live;
    }
}
