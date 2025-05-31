using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class QuedaMinigameController : MinigameController
{
    public UnityEvent finalizar;
    [SerializeField] private int winnerPoints = 100;
    [SerializeField] private int eliminationStepPoints = 20;
    
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
    
    public override void OnStartServer()
    {
        base.OnStartServer();
    
        alivePlayers  = playerList.players.ToList();   
        eliminationOrder.Clear();
        finalScores.Clear();

        Debug.Log($"[Queda] Round iniciado com {alivePlayers.Count} jogadores.");
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
        Debug.LogError($"[Sumo] Eliminado: {pd.playerInfo.steamId}");
        if (alivePlayers.Count <= 1)
        {
            finalizar?.Invoke();
        }
    }
    public override void AssignFinalPoints()
    {
        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            finalScores[winner.playerInfo.steamId] = winnerPoints;
        }
        else if (alivePlayers.Count > 1)
        {
            foreach (var pdA in alivePlayers){
                
                finalScores[pdA.playerInfo.steamId] = winnerPoints;
            }
        }
                
        for (int i = 0; i < eliminationOrder.Count; i++)
        {
            int pts = eliminationStepPoints * (i + 1);
            var pd  = eliminationOrder[i];
            finalScores[pd.playerInfo.steamId] = pts;
        }
    }

    public override Dictionary<ulong,int> GetResults() => finalScores;
}
