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
    private bool _matchEnded;
    
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

    // public Animator _thor;

    public void StartGame()
    {
        _startGame = true;
    }

    public override bool UseAliveStatusOnScoreboard => true;
    public override void StartMatch()
    {
        alivePlayers = playerList.players.Where(p => p != null).ToList();
        eliminationOrder.Clear();
        finalScores.Clear();
        
        Debug.Log($"[Sumo] StartMatch iniciado com {alivePlayers.Count} jogadores.");

        base.StartMatch();
        _matchEnded = false;
        Notifica();  
    }
    public override void SetupMiniGame()
    {
        base.SetupMiniGame();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Adicionar(this);
        Notifica();
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
                //  _thor?.SetTrigger("Marretar");

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
    public override void EndMatch()
    {
        if (_matchEnded) return;
        _matchEnded = true;
        AssignFinalPoints();
        base.EndMatch();
    }

    [Server]
    public void Eliminate(PlayerData pd)
    {
        if (_matchEnded)
        {
            Debug.LogWarning($"[Sumo] Tentativa de eliminar {pd.playerInfo.steamId} após fim da partida - IGNORADO");
            return;
        }

        if (eliminationOrder.Contains(pd))
        {
            Debug.LogWarning($"[Sumo] Jogador {pd.playerInfo.steamId} já eliminado anteriormente - IGNORADO");
            return;
        }
        
        alivePlayers.Remove(pd);
        eliminationOrder.Add(pd);
        Debug.Log($"💣[Sumo] Eliminado: {pd.playerInfo.steamId}");
        Notifica();
        if (alivePlayers.Count <= 1)
        {
            _matchEnded = true;
            AssignFinalPoints();
            finalizar?.Invoke();
        }
    }
    public override void AssignFinalPoints()
    {
        if (!isServer) return;

        finalScores.Clear();
        int posIndex = 0;

        if (alivePlayers.Count > 0)
        {
            foreach (var winner in alivePlayers)
            {
                finalScores[winner.playerInfo.steamId] = gameData.firstPlaceBonus;
            }
            posIndex++;
        }

        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            var pd = eliminationOrder[i];
            int pts = 0;
            
            switch (posIndex) 
            {
                case 0: pts = gameData.firstPlaceBonus;  break;
                case 1: pts = gameData.secondPlaceBonus; break;
                case 2: pts = gameData.thirdPlaceBonus;  break;
                case 3: pts = gameData.fourthPlaceBonus; break;
                default: pts = 0; break;
            }
            
            finalScores[pd.playerInfo.steamId] = pts;
            posIndex++;
        }
        
        Debug.Log($"[Sumo] Pontos atribuídos. Vivos: {alivePlayers.Count}, Eliminados: {eliminationOrder.Count}");
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

        foreach (var pd in alivePlayers)
            live[pd.playerInfo.steamId] = 1;

        foreach (var pd in eliminationOrder)
            live[pd.playerInfo.steamId] = 0;

        return live;
    }

    

}
