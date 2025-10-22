using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class MemoriaMinigameController : MinigameController, IObserver
{
    public UnityEvent finalizar;
    [SerializeField] SettingsMiniGameData settingsData;

    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private Dictionary<ulong,int> finalScores = new Dictionary<ulong,int>();
    private bool _matchEnded;

    private PlayerList playerList => PlayerList.singleton;

    [Header("Fases do Instrutor")]
    [SerializeField] private Instrutor instrutor;

    public bool _startGame = false;

    public void StartGame()
    {
        _startGame = true;
    }

    public override void SetupMiniGame()
    {
        base.SetupMiniGame();
    }

    public override void StartMatch()
    {
        base.StartMatch();
        _matchEnded = false;
        Notifica();
        if (isServer)
        {
            if (instrutor == null)
                instrutor = FindFirstObjectByType<Instrutor>();
            if (instrutor != null)
                instrutor.StartMemoryCycle();
            else
                Debug.LogWarning("[MEMÓRIA] Instrutor não encontrado ao iniciar a partida.");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        alivePlayers  = playerList.players.ToList();
        eliminationOrder.Clear();
        finalScores.Clear();

        if (instrutor == null)
            instrutor = FindFirstObjectByType<Instrutor>();

        Adicionar(this);
        Notifica();

        Debug.Log($"🎲 [MEMÓRIA] Round iniciado com {alivePlayers.Count} jogadores");
        Invoke("AddPlayer", 2f);
    }

    public void AddPlayer()
    {
        alivePlayers  = playerList.players.ToList();
    }

    public override void UpdateScores()
    {
        if (!isServer || !_startGame)
            return;
    }

    [Server]
    public void Eliminate(PlayerData pd)
    {
        if (_matchEnded)
        {
            Debug.LogWarning($"[MEMÓRIA] Tentativa de eliminar {pd.playerInfo.steamId} após fim da partida - IGNORADO");
            return;
        }
        
        alivePlayers.Remove(pd);
        eliminationOrder.Add(pd);
        Debug.LogWarning($"❌ [MEMÓRIA] Eliminado: {pd.playerInfo.steamId}");
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
