using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class HideStep
{
    public MeshRenderer[] blinkTargets;
    public GameObject[] disableTargets;
}

public class SumoMinigameController : MinigameController, IObserver
{
    public UnityEvent finalizar;
    public SettingsMiniGameData gameData;
    
    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private Dictionary<ulong,int> finalScores = new Dictionary<ulong,int>();
    
    private PlayerList playerList => PlayerList.singleton;
    [SerializeField] private HideStep[] hideSequence;

    [Header("Tempos")]
    [SerializeField] private float timeBetweenSteps = 5f;
    [SerializeField] private float blinkDuration = 1f;
    [SerializeField] private float blinkInterval = 0.2f;

    public enum HideState { Waiting, Blinking, Done }
    private HideState state;
    [SyncVar] int currentIndex;
    [SyncVar] float timer = 5f;
    [SyncVar] float nextBlink;

    public bool _startGame = false;

    public Animator _thor;

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
        Debug.Log($"[Sumo] Round iniciado com {alivePlayers.Count} jogadores.");
        Invoke("AddPlayer", 2f);
    }

    public void AddPlayer()
    { 
        alivePlayers  = playerList.players.ToList();    
    }
    public override void UpdateScores()
    {
        if (!isServer || !_startGame || state == HideState.Done)
            return;
        
        float dt = Time.deltaTime;
        switch (state)
        {
            case HideState.Waiting:
                timer -= dt;
                if (timer <= 0f)
                {
                    state = HideState.Blinking;
                    timer = blinkDuration;
                    nextBlink = blinkInterval;
                }
                break;

            case HideState.Blinking:
                timer     -= dt;
                nextBlink -= dt;
                 _thor?.SetTrigger("Marretar");

                if (nextBlink <= 0f)
                {

                    RpcToggleBlink(currentIndex);
                    nextBlink = blinkInterval;
                }

                if (timer <= 0f)
                {
                    RpcEnsureVisible(currentIndex);
                    RpcDisableStep(currentIndex);

                    currentIndex++;
                    PrepareNextStep();
                }
                break;
        }
    }
    private void PrepareNextStep()
    {
        if (currentIndex >= hideSequence.Length)
        {
            state = HideState.Done;
            return;
        }

        timer = timeBetweenSteps;
        state = HideState.Waiting;
    }
    [Server]
    public void Eliminate(PlayerData pd)
    {
        alivePlayers.Remove(pd);
        eliminationOrder.Add(pd);
        Debug.LogError($"[Sumo] Eliminado: {pd.playerInfo.steamId}");
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
            finalScores[winner.playerInfo.steamId] = gameData.firstPlaceBonus;
        }
        else if (alivePlayers.Count > 1)
        {
            foreach (var pdA in alivePlayers){
                
                finalScores[pdA.playerInfo.steamId] = gameData.firstPlaceBonus;
            }
        }
        
        for (int i = 0; i < eliminationOrder.Count; i++)
        {
            int pts = gameData.secondPlaceBonus;
            var pd  = eliminationOrder[i];
            finalScores[pd.playerInfo.steamId] = pts;
        }
    }


    [ClientRpc]
    void RpcToggleBlink(int step)
    {
        foreach (var mr in hideSequence[step].blinkTargets)
            mr.enabled = !mr.enabled;
    }
    void RpcEnsureVisible(int step)
    {
        foreach (var mr in hideSequence[step].blinkTargets)
            mr.enabled = true;
    }

    [ClientRpc]
    void RpcDisableStep(int step)
    {
        foreach (var go in hideSequence[step].disableTargets)
            if (go != null) go.SetActive(false);
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
