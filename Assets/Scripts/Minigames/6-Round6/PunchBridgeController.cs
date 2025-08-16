using System.Collections.Generic;
using UnityEngine;

public class PunchBridgeController : MinigameController, IObserver
{
    [SerializeField] List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] List<PlayerData> eliminationOrder = new List<PlayerData>();
    PlayerList playerList => PlayerList.singleton;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void UpdateScores()
    {
        throw new System.NotImplementedException();
    }

    public override void AssignFinalPoints()
    {
        throw new System.NotImplementedException();
    }

    public override Dictionary<ulong, int> GetResults()
    {
        throw new System.NotImplementedException();
    }

    public override Dictionary<ulong, int> GetLiveScores()
    {
        throw new System.NotImplementedException();
    }
}
