using System;
using UnityEngine;


[System.Serializable]
public class VictoryPlayerData
{
    public ulong steamId;
    public string playerName;
    public int finalScore;
    public int playerColorIndex;
    public PlayerCustomizationData customization;
    
    public Color playerColor;
    
    public VictoryPlayerData()
    {
        steamId = 0;
        playerName = string.Empty;
        finalScore = 0;
        playerColorIndex = -1;
        customization = new PlayerCustomizationData();
        playerColor = Color.white;
    }
    
    public VictoryPlayerData(ulong steamId, string playerName, int score, int colorIndex, PlayerCustomizationData customization)
    {
        this.steamId = steamId;
        this.playerName = playerName;
        this.finalScore = score;
        this.playerColorIndex = colorIndex;
        this.customization = customization ?? new PlayerCustomizationData();
        this.playerColor = Color.white;
    }
    

    public VictoryPlayerData Clone()
    {
        return new VictoryPlayerData(steamId, playerName, finalScore, playerColorIndex, customization?.Clone())
        {
            playerColor = this.playerColor
        };
    }
    
    public override string ToString()
    {
        return $"VictoryPlayerData [SteamID: {steamId}, Name: {playerName}, Score: {finalScore}, Color: {playerColorIndex}, Customization: {customization}]";
    }
}


[System.Serializable]
public class VictoryRankingData
{
    public VictoryPlayerData[] rankedPlayers;
    
    public VictoryRankingData()
    {
        rankedPlayers = new VictoryPlayerData[4];
    }
    
    public VictoryRankingData(VictoryPlayerData[] players)
    {
        rankedPlayers = players ?? new VictoryPlayerData[4];
    }
    

    public VictoryPlayerData GetPlayerAtPosition(int position)
    {
        if (position < 1 || position > 4)
            return null;
        
        int index = position - 1;
        if (index >= rankedPlayers.Length || rankedPlayers[index] == null)
            return null;
        
        return rankedPlayers[index];
    }
    

    public VictoryPlayerData GetWinner()
    {
        return GetPlayerAtPosition(1);
    }
    
    public override string ToString()
    {
        string result = "VictoryRankingData [";
        for (int i = 0; i < rankedPlayers.Length; i++)
        {
            if (rankedPlayers[i] != null)
            {
                result += $"\n  {i + 1}º: {rankedPlayers[i].playerName} ({rankedPlayers[i].finalScore} pts)";
            }
        }
        result += "\n]";
        return result;
    }
}

