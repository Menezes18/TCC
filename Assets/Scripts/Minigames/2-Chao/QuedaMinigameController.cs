using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class QuedaMinigameController : MinigameController, IObserver
{
    public UnityEvent finalizar;
    [SerializeField] SettingsMiniGameData settingsData;
    
    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private Dictionary<ulong,int> finalScores = new Dictionary<ulong,int>();
    
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
        Notifica();  
    }
    public void SetupMiniGame()
    {
        base.SetupMiniGame();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
    
        alivePlayers  = playerList.players.ToList();   
        eliminationOrder.Clear();
        finalScores.Clear();
        Adicionar(this);
        Notifica();
        
        Debug.Log($"🎲 [QUEDA] Round iniciado com {alivePlayers.Count} jogadores");
        Invoke("AddPlayer", 2f);
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
        alivePlayers.Remove(pd);
        eliminationOrder.Add(pd);
        Debug.LogWarning($"❌ [QUEDA] Eliminado: {pd.playerInfo.steamId}");
        Notifica();
        if (alivePlayers.Count <= 1)
        {
            AssignFinalPoints();
            finalizar?.Invoke();
        }
    }
    public override void AssignFinalPoints()
    {
        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            finalScores[winner.playerInfo.steamId] = settingsData.firstPlaceBonus;
        }
        else if (alivePlayers.Count > 1)
        {
            foreach (var pdA in alivePlayers){
                
                finalScores[pdA.playerInfo.steamId] = settingsData.firstPlaceBonus;
            }
        }
                
        for (int i = 0; i < eliminationOrder.Count; i++)
        {
            int pts = settingsData.secondPlaceBonus;
            var pd  = eliminationOrder[i];
            finalScores[pd.playerInfo.steamId] = pts;
        }
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

        return live;
    }
}
