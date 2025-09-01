#region Usings
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
#endregion

public class BatataQuenteMinigameController : MinigameController, IObserver
{
    #region Campos e Estado
    [Header("Configurações da Batata Quente")]
    [SerializeField] private SettingsMiniGameData settingsData;
    [SerializeField] private float passDistance = 3f;
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private HUDSO hudso;
    [Header("Roleta (UI de clientes)")]
    [SerializeField] private SlotRoleta roletaUI;
    [SerializeField] private float selectionFreezeSeconds = 3.2f;

    [Header("Estado")]
    [SerializeField] private List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] private List<PlayerData> eliminationOrder = new List<PlayerData>();
    private readonly Dictionary<ulong, int> finalScores = new Dictionary<ulong, int>();

    private PlayerList playerList => PlayerList.singleton;
    private bool matchActive;

    [SyncVar(hook = nameof(OnHolderChanged))]
    private ulong potatoHolderId;

    private System.Random _rng = new System.Random();
    #endregion


    #region Setup
    public override void OnStartServer()
    {
        base.OnStartServer();
        Adicionar(this);
        Notifica();
    
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();
        matchActive = true;

        alivePlayers.Clear();
        eliminationOrder.Clear();
        finalScores.Clear();


        alivePlayers.AddRange(playerList.players.Where(p => p != null));

        StartCoroutine(SelectHolderWithRoulette());
        Notifica();
    }
    #endregion
    #region Seleção via Roleta (server-authoritative)

    [Server]
    private IEnumerator SelectHolderWithRoulette()
    {
        if (alivePlayers.Count == 0)
        {
            potatoHolderId = 0;
            MatchManager.singleton.SetMatchTimer(-1f);
            yield break;
        }

        // 1) congela todo mundo
        FreezeAll(true);

        // 2) prepara ordem visível e escolhe o vencedor no servidor
        ulong[] order = alivePlayers.Select(p => p.playerInfo.steamId).ToArray();
        PlayerData chosen = WeightedPick(alivePlayers);
        potatoHolderId = chosen != null ? chosen.playerInfo.steamId : 0;

        // 3) manda os clientes girarem até o escolhido
        RpcShowRoulette(order, potatoHolderId, selectionFreezeSeconds);

        // 4) espera a animação da roleta e solta geral
        yield return new WaitForSeconds(selectionFreezeSeconds);

        FreezeAll(false);

        // 5) inicia/renova o timer da batata para a posse atual
        MatchManager.singleton.SetMatchTimer(timeLimit);
    }

    [ClientRpc]
    private void RpcShowRoulette(ulong[] order, ulong winnerSteamId, float freezeSeconds)
    {
        // Cada cliente reconstrói a roleta com a MESMA ordem do servidor e gira até o vencedor
        if (roletaUI == null) return;

        roletaUI.SetEntriesFromSteamIds(order);
        roletaUI.SpinToWinner(winnerSteamId);

        // Obs: Se você quiser congelar input local (cliente) durante a rotação,
        // faça num outro componente de UI, usando 'freezeSeconds' como referência de duração.
    }

    // Congela/descongela todos os vivos
    [Server]
    private void FreezeAll(bool frozen)
    {
        foreach (var pd in alivePlayers)
        {
            if (pd == null) continue;
            var ps = pd.GetComponent<PlayerScript>();
            if (ps != null) ps.isFrozen = frozen;
        }
    }

    // Sorteio ponderado (por enquanto pesos = 1). Se quiser pesos reais, troque GetWeight(pd).
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
    private float GetWeight(PlayerData pd)
    {
        // TODO: ligar com alguma origem de pesos se desejar (ex.: placar, favoritismo, etc.)
        return 1f;
    }

    #endregion
    #region Mecânica de Passe / Eliminação

    [Server]
    public void OnPlayerPush(PlayerData attacker, PlayerData target)
    {
        if (!matchActive) return;
        if (attacker == null || target == null) return;
        if (attacker.playerInfo.steamId != potatoHolderId) return;
        if (!alivePlayers.Contains(target)) return;

        float distSqr = (attacker.transform.position - target.transform.position).sqrMagnitude;
        if (distSqr > passDistance * passDistance) return;

        // troca a posse e reinicia o timer
        potatoHolderId = target.playerInfo.steamId;
        MatchManager.singleton.SetMatchTimer(timeLimit);
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
            // fim da partida
            MatchManager.singleton.SetMatchTimer(-1f);
            potatoHolderId = alivePlayers.Count == 1 ? alivePlayers[0].playerInfo.steamId : 0;
            AssignFinalPoints();
        }
        else
        {
            // escolhe novo holder via roleta
            StartCoroutine(SelectHolderWithRoulette());
        }
    }

    #endregion
    #region Tempo / Pontuação

    [Server]
    public override void UpdateScores()
    {
        if (!matchActive) return;

        float current = MatchManager.singleton.MatchTimer;
        if (current > 0f)
        {
            current -= Time.deltaTime;
            MatchManager.singleton.SetMatchTimer(current);

            if (current <= 0f)
            {
                // tempo estourou: elimina quem está segurando
                var holder = alivePlayers.FirstOrDefault(p => p.playerInfo.steamId == potatoHolderId);
                if (holder != null)
                    Eliminate(holder);
            }
        }
    }

    public override void AssignFinalPoints()
    {
        finalScores.Clear();

        // vencedor
        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            finalScores[winner.playerInfo.steamId] = settingsData.firstPlaceBonus;
        }

        // demais posições (ordem de eliminação)
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

    #endregion
    #region UI / Observer

    private void OnHolderChanged(ulong oldVal, ulong newVal)
    {
        string name = string.Empty;

        if (newVal != 0)
        {
            var pd = PlayerList.singleton.players.FirstOrDefault(p => p.playerInfo.steamId == newVal);
            if (pd != null) name = pd.playerInfo.username;
        }

        if (hudso != null)
            hudso.PotatoHolderUpdate(name);
    }

    public void Atualizacao(ISubject subject) { /* implementação do seu padrão Observer, se necessário */ }
    #endregion
}
