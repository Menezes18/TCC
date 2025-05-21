using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class StreetMinigameController : MinigameController
{
    [Header("Configuração da Pista")]
    [SerializeField] private Transform startLine;
    [SerializeField] private Transform finishLine;
    
    private Dictionary<ulong, int> lastProgressPercent = new();
    private Dictionary<ulong, int> accumulatedPointsDelta = new();
    private Dictionary<ulong, int> finalScores = new();

    private PlayerList playerList => PlayerList.singleton;

    public void SetupMiniGame()
    {
        base.SetupMiniGame();
        
        
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        lastProgressPercent.Clear();
        accumulatedPointsDelta.Clear();
        finalScores.Clear();

        foreach (var playerData in playerList.players)
        {
            var steamId = playerData.playerInfo.steamId;
            lastProgressPercent[steamId] = 0;
            accumulatedPointsDelta[steamId] = 0;
            finalScores[steamId] = 0;
        }
    }

    public override void UpdateScores()
    {
        float startZ = startLine.position.z;
        float finishZ = finishLine.position.z;
        float trackLength = finishZ - startZ;

        foreach (var playerData in playerList.players)
        {
            var steamId = playerData.playerInfo.steamId;

            if (!lastProgressPercent.TryGetValue(steamId, out int prevPercent))
                lastProgressPercent[steamId] = prevPercent = 0;
            if (!accumulatedPointsDelta.TryGetValue(steamId, out int currDelta))
                accumulatedPointsDelta[steamId] = currDelta = 0;

            float currentZ = playerData.transform.position.z;
            float normalizedProgress = Mathf.Clamp01((currentZ - startZ) / trackLength);
            int currentPercent = Mathf.FloorToInt(normalizedProgress * 100);

            int deltaPoints = currentPercent - prevPercent;
            lastProgressPercent[steamId] = currentPercent;
            Debug.LogWarning(accumulatedPointsDelta[steamId] + " "  +currDelta + " " + deltaPoints);
            accumulatedPointsDelta[steamId] = currDelta + deltaPoints;
        }
    }

    public override void AssignFinalPoints()
    {
        foreach (var steamId in accumulatedPointsDelta.Keys)
            finalScores[steamId] = accumulatedPointsDelta[steamId];

        float finishZ = finishLine.position.z;
        var finishOrder = playerList.players
            .Where(pd => pd.transform.position.z >= finishZ)
            .OrderByDescending(pd => pd.transform.position.z)
            .Select(pd => pd.playerInfo.steamId)
            .ToList();

        // for (int i = 0; i < finishOrder.Count; i++)
        // {
        //     var steamId = finishOrder[i];
        //     finalScores[steamId] += (finishOrder.Count - i) * 20;
        // }

        if (finishOrder.Count == 0 && playerList.players.Count > 0)
        {
            var leaderSteamId = playerList.players
                .OrderByDescending(pd => pd.transform.position.z)
                .First().playerInfo.steamId;
            finalScores[leaderSteamId] += 10;
        }
    }

    public override Dictionary<ulong,int> GetResults()
    {
        return finalScores;
    }
}
