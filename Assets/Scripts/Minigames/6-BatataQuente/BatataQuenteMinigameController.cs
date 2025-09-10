#region Usings
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
#endregion

public class BatataQuenteMinigameController : MinigameController, IObserver
{

    public enum GameMode
    {
        Eliminatorio, // explode/eliminação ao zerar tempo
        Rotativo      // não elimina; volta a selecionar
    }

    private enum Phase
    {
        Idle,
        Selecting,   // roleta rodando (congelado)
        Round        // rodada ativa (timer contando)
    }


    [Header("Configurações")]
    [SerializeField] private SettingsMiniGameData settingsData;
    [SerializeField] private float passDistance = 3f;
    [SerializeField] private float timeLimit = 20f; // 20s por rodada
    [SerializeField] private HUDSO hudso;
    [SerializeField] private GameMode mode = GameMode.Eliminatorio;

    [Header("Roleta (UI de clientes)")]
    [SerializeField] private SlotRoleta roletaUI;
    [SerializeField] private float selectionFreezeSeconds = 3.2f;

    [Header("Comportamento de estouro")]
    [Tooltip("Se true, ao zerar o timer o holder explode (é eliminado).")]
    [SerializeField] private bool eliminateOnTimeout = true;

    [Header("Estado")]
    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private readonly Dictionary<ulong, int> finalScores = new Dictionary<ulong, int>();

    private PlayerList playerList => PlayerList.singleton;
    private bool matchActive;
    private float roundTimer;
    private int _lastWholeSecondLogged = -1;

    private Phase phase = Phase.Idle;
    private double selectionEndTime = -1; // NetworkTime.time alvo p/ terminar seleção

    [SyncVar(hook = nameof(OnHolderChanged))]
    private ulong potatoHolderId;

    private System.Random _rng = new System.Random();


    #region Setup
    public override void SetupMiniGame()
    {
        base.SetupMiniGame();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Notifica();
        Adicionar(this);
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();
        matchActive = true;
        roundTimer = 0f;

        if (mode == GameMode.Eliminatorio)
            MatchManager.singleton.SetMatchTimer(-1f);

        alivePlayers.Clear();
        eliminationOrder.Clear();
        finalScores.Clear();
        alivePlayers.AddRange(playerList.players.Where(p => p != null));

        StartSelection();
        Notifica();
    }
    #endregion


    #region Loop por Update (server-authoritative)
    public override void UpdateScores()
    {


    }
    [ServerCallback]
    private void Update()
    {
        if (!matchActive) return;
        if (phase == Phase.Round && roundTimer > 0f)
            {
                roundTimer -= Time.deltaTime;

                if (hudso != null) hudso.MatchTimerUpdate(Mathf.Max(0f, roundTimer));

                RpcUpdateTimer(Mathf.Max(0f, roundTimer));

                if (roundTimer <= 0f)
                {
                    HandleTimeout();
                }
            }
        switch (phase)
        {
            case Phase.Selecting:
                
                if (NetworkTime.time >= selectionEndTime)
                {
                    //"selection-end"
                    SafeUnfreeze();
                    BeginRoundTimer();
                }
                break;

            case Phase.Round:
                
                if (roundTimer > 0f)
                {
                    roundTimer -= Time.deltaTime;

                    if (hudso != null) hudso.MatchTimerUpdate(Mathf.Max(0f, roundTimer));

                    int whole = Mathf.CeilToInt(roundTimer);
                    if (whole != _lastWholeSecondLogged)
                    {
                        _lastWholeSecondLogged = whole;
                        var holder = alivePlayers.FirstOrDefault(p => p.playerInfo.steamId == potatoHolderId);
                        if (holder != null && whole > 0)
                            Debug.Log($"[BatataQuente] {holder.playerInfo.username}: {whole}s restantes.");
                    }

                    if (roundTimer <= 0f)
                    {
                        HandleTimeout(); 
                    }
                }
                break;

            case Phase.Idle:
            default:
                break;
        }
    }
    #endregion


    
    [Server]
    private void StartSelection()
    {
    if (alivePlayers.Count == 0)
    {
        potatoHolderId = 0;
        MatchManager.singleton.SetMatchTimer(0f);
        phase = Phase.Idle;
        return;
    }

    phase = Phase.Selecting;

    FreezeAll(true);

    var vivos = alivePlayers.ToArray();

    ulong[] order  = new ulong[vivos.Length];
    string[] names = new string[vivos.Length];
    int[] colors   = new int[vivos.Length];

    for (int i = 0; i < vivos.Length; i++)
    {
        var pd = vivos[i];
        order[i]  = pd.playerInfo.steamId;
        names[i]  = string.IsNullOrWhiteSpace(pd.alias) ? pd.playerInfo.username : pd.alias;
        colors[i] = pd.color;
    }

    PlayerData chosen = WeightedPick(alivePlayers);
    potatoHolderId = chosen != null ? chosen.playerInfo.steamId : 0;

    if (chosen != null)
        Debug.Log($"[BatataQuente] Selecionado: {chosen.playerInfo.username} — tempo = {timeLimit:0.#}s para passar.");

    RpcShowRoulette(order, names, colors, potatoHolderId, selectionFreezeSeconds);

    selectionEndTime = NetworkTime.time + selectionFreezeSeconds;
    }
    [ClientRpc]
    private void RpcShowRoulette(
        ulong[] order,
        string[] aliases,
        int[] colors,
        ulong winnerSteamId,
        float freezeSeconds)
    {
        if (roletaUI == null) return;

        roletaUI.duracao = freezeSeconds;
        roletaUI.ShowOverlay(true);

        roletaUI.OnWinTextClosed -= HandleClose;
        roletaUI.OnWinTextClosed += HandleClose;

        roletaUI.PrepareEntriesSnapshot(order, aliases, colors);

        roletaUI.SpinToWinner(winnerSteamId);

        void HandleClose()
        {
            roletaUI.ShowOverlay(false);
            roletaUI.OnWinTextClosed -= HandleClose;
        }
    }
    [ClientRpc]
    private void RpcShowRoulette(ulong[] order, ulong winnerSteamId, float freezeSeconds)
    {
        if (roletaUI == null) return;

        roletaUI.duracao = freezeSeconds;

        roletaUI.ShowOverlay(true);

        roletaUI.OnWinTextClosed -= HandleClose;
        roletaUI.OnWinTextClosed += HandleClose;

        roletaUI.SetEntriesFromSteamIds(order);
        roletaUI.SpinToWinner(winnerSteamId);

        void HandleClose()
        {
            roletaUI.ShowOverlay(false);
            roletaUI.OnWinTextClosed -= HandleClose;
        }
    }




    #region Rodada / Timeout / Eliminação
    [Server]
    private void BeginRoundTimer()
    {
        roundTimer = timeLimit;
        _lastWholeSecondLogged = Mathf.CeilToInt(roundTimer);
        if (hudso != null) hudso.MatchTimerUpdate(roundTimer);
        phase = Phase.Round;
    }

    [Server]
    private void HandleTimeout()
    {
        var holder = alivePlayers.FirstOrDefault(p => p.playerInfo.steamId == potatoHolderId);

        if (eliminateOnTimeout || mode == GameMode.Eliminatorio)
        {
            if (holder != null)
                ExplodeAndEliminateCurrentHolder(holder);
            
            if (alivePlayers.Count <= 1)
                EndMatch();
            else
                StartSelection();
        }
        else
        {
            if (alivePlayers.Count > 1)
                StartSelection();
            else
                EndMatch();
        }
    }

    [Server]
    private void ExplodeAndEliminateCurrentHolder(PlayerData holder)
    {
        string holderName = holder != null ? holder.playerInfo.username : "(desconhecido)";
        Debug.Log($"[BatataQuente] {holderName} explodiu! Ficou {timeLimit:0.#}s sem passar.");
        Eliminate(holder);
    }

    [Server]
    private void Eliminate(PlayerData pd)
    {
        if (pd == null) return;

        alivePlayers.Remove(pd);
        eliminationOrder.Add(pd);

        var ps = pd.GetComponent<PlayerScript>();
        if (ps != null) ps.isFrozen = true;

        Notifica();

        if (alivePlayers.Count <= 1)
        {
            potatoHolderId = alivePlayers.Count == 1 ? alivePlayers[0].playerInfo.steamId : 0;
            EndMatch();
        }
        else
        {
           
        }
    }

    [Server]
    private void EndMatch()
    {
        MatchManager.singleton.SetMatchTimer(0f);
        phase = Phase.Idle;
        matchActive = false;
        SafeUnfreeze();
    }
    #endregion


    [Server]
    public void OnPlayerPush(PlayerData attacker, PlayerData target)
    {
        if (!matchActive) return;
        if (phase != Phase.Round) return;
        if (attacker == null || target == null) return;
        if (attacker.playerInfo.steamId != potatoHolderId) return;
        if (!alivePlayers.Contains(target)) return;

        float distSqr = (attacker.transform.position - target.transform.position).sqrMagnitude;
        if (distSqr > passDistance * passDistance) return;

        potatoHolderId = target.playerInfo.steamId;
        // roundTimer = timeLimit;
        // _lastWholeSecondLogged = Mathf.CeilToInt(roundTimer);

        Debug.Log($"[BatataQuente] {attacker.playerInfo.username} passou para {target.playerInfo.username}. Tempo resetado para {timeLimit:0.#}s.");
    }


    #region Utilitários (freeze/unfreeze/weight)
    [Server]
    private void FreezeAll(bool frozen)
    {
        foreach (var pd in playerList.players)
        {
            if (pd == null) continue;
            var ps = pd.GetComponent<PlayerScript>();
            if (ps != null) ps.isFrozen = frozen; 
        }
        
    }

    [Server]
    private void SafeUnfreeze()
    {
        FreezeAll(false);
       
    }

    [Server]
    private PlayerData WeightedPick(List<PlayerData> list)
    {
        if (list == null || list.Count == 0) return null;

        double sum = 0;
        var weights = new double[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            weights[i] = Mathf.Max(0.0001f, GetWeight(list[i]));
            sum += weights[i];
        }

        double alvo = _rng.NextDouble() * sum;
        double acc = 0;
        for (int i = 0; i < list.Count; i++)
        {
            acc += weights[i];
            if (alvo <= acc) return list[i];
        }

        return list[list.Count - 1];
    }

    [Server]
    private float GetWeight(PlayerData pd) => 1f;
    #endregion


    #region Pontuação / Hooks UI
    public override void AssignFinalPoints()
    {
        finalScores.Clear();

        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            finalScores[winner.playerInfo.steamId] = settingsData.firstPlaceBonus;
        }

        for (int i = 0; i < eliminationOrder.Count; i++)
        {
            var pd = eliminationOrder[i];
            finalScores[pd.playerInfo.steamId] = settingsData.secondPlaceBonus;
        }
    }

    public override Dictionary<ulong, int> GetResults() => finalScores;

    public override Dictionary<ulong, int> GetLiveScores()
    {
        var live = new Dictionary<ulong, int>();
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

    private void OnHolderChanged(ulong oldVal, ulong newVal)
    {
        string name = string.Empty;

        if (newVal != 0)
        {
            var pd = PlayerList.singleton.players.FirstOrDefault(p => p.playerInfo.steamId == newVal);
            if (pd != null)
                name = string.IsNullOrWhiteSpace(pd.alias) ? pd.playerInfo.username : pd.alias;
            else
                name = $"Player {newVal}";
        }

        if (hudso != null)
            hudso.PotatoHolderUpdate(name);
    }
    [ClientRpc]
    private void RpcUpdateTimer(float timeLeft)
    {
        if (hudso != null)
            hudso.MatchTimerUpdate(timeLeft);
    }

    public void Atualizacao(ISubject subject) { }
    #endregion
}
