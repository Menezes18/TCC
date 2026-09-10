using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ScoreboardUI : NetworkBehaviour, IObserver
{
    [SerializeField] GameObject slotPrefab;
    [SerializeField] Transform slotsParent;
    [SerializeField] Database database;
    [SerializeField] MinigameController controller;

    readonly List<ScoreboardSlot> activeSlots = new();
    private MinigameController pendingController;
    private double nextScoreboardUpdate;

    void Awake()
    {
        if (controller == null)
            controller = FindAnyObjectByType<MinigameController>();

        if (controller != null)
            controller.Adicionar(this);
    }

    void Start()
    {
        if (isServer)
        {
            Debug.Log("📊 [SCOREBOARD] Inicializando scoreboard no servidor");
            var liveScores = controller != null ? controller.GetLiveScores() : null;
            var results = liveScores != null && liveScores.Count > 0
                ? liveScores
                : MyNetworkManager.manager != null ? MyNetworkManager.manager.lastGameResults : null;
            if (results != null && results.Count > 0)
                SendResults(results);
        }
    }

    public override void OnStopClient()
    {
        if (controller != null)
            controller.Retira(this);
        base.OnStopClient();
    }

    public void Atualizacao(ISubject subject)
    {
        var mc = subject as MinigameController;
        if (mc == null) return;
        pendingController = mc;
    }

    [ServerCallback]
    private void LateUpdate()
    {
        if (pendingController == null || NetworkTime.time < nextScoreboardUpdate) return;
        var source = pendingController;
        pendingController = null;
        nextScoreboardUpdate = NetworkTime.time + 0.1;
        SendResults(source.GetLiveScores());
    }

    [Server]
    void SendResults(Dictionary<ulong, int> results)
    {
        if (results == null) return;
        var ordered = results.OrderByDescending(kv => kv.Value).ToList();
        string[] names = new string[ordered.Count];
        int[] pts = new int[ordered.Count];
        int[] colors = new int[ordered.Count];
        bool[] aliveStates = new bool[ordered.Count];
        ulong[] steamIds = new ulong[ordered.Count];
        bool useTeamColors = controller != null && controller.UseTeamColorsOnScoreboard;
        int[] teamIds = useTeamColors ? new int[ordered.Count] : null;

        var playersById = new Dictionary<ulong, PlayerData>();
        if (PlayerList.singleton != null)
            foreach (var player in PlayerList.singleton.players)
                if (player != null) playersById[player.playerInfo.steamId] = player;
        for (int i = 0; i < ordered.Count; i++)
        {
            ulong id = ordered[i].Key;
            int score = ordered[i].Value;
            playersById.TryGetValue(id, out var pd);
            names[i] = pd != null ? pd.alias : id.ToString();
            steamIds[i] = id;
            if (teamIds != null) teamIds[i] = -1;
            if (useTeamColors)
            {
                int team = controller.GetScoreboardTeam(id);
                if (team == 0) names[i] = $"{names[i]} [Azul]";
                else if (team == 1) names[i] = $"{names[i]} [Vermelho]";
                if (teamIds != null) teamIds[i] = team;
            }
            colors[i] = pd != null ? pd.color : -1;
            pts[i] = score;
            var playerScript = pd != null ? pd.GetComponent<PlayerScript>() : null;
            aliveStates[i] = playerScript == null || !playerScript.IsDead;
        }

        // Dispatch the results via the Networked controller (spawned), not from this UI
        controller?.RpcUpdateScoreboard(names, pts, colors, aliveStates, steamIds, teamIds);
    }

    public void UpdateUI(string[] names, int[] points, int[] colors, bool[] aliveStates, ulong[] steamIds, int[] teamIds)
    {
        EnsureSlots(names.Length);

        bool useTeamColors = controller != null && controller.UseTeamColorsOnScoreboard;
        Color teamBlue = new Color(0.1f, 0.3f, 0.9f, 1f);
        Color teamRed = new Color(1.0f, 0.2f, 0.2f, 1f);
        bool usePercentage = controller != null && controller.UsePercentageOnScoreboard;
        float minProgress = usePercentage && points.Length > 0 ? points.Min() : 0;
        float maxProgress = usePercentage && points.Length > 0 ? points.Max() : 0;

        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (i < names.Length)
            {
                Color c = Color.white;
                Color nameColor = Color.white;
                if (useTeamColors)
                {
                    if (teamIds != null && i < teamIds.Length && teamIds[i] >= 0)
                    {
                        int team = teamIds[i];
                        c = team == 0 ? teamBlue : team == 1 ? teamRed : Color.white;
                        nameColor = c;
                    }
                    else if (controller != null)
                    {
                        int team = controller.GetScoreboardTeam(steamIds[i]);
                        c = team == 0 ? teamBlue : team == 1 ? teamRed : Color.white;
                        nameColor = c;
                    }
                }
                else
                {
                    if (database != null && colors[i] >= 0 && colors[i] < database.playerColors.Count)
                        c = database.playerColors[colors[i]].color;
                }
                activeSlots[i].gameObject.SetActive(true);
                bool useAliveStatus = controller != null && controller.UseAliveStatusOnScoreboard;
                bool isAlive = useAliveStatus
                    ? (aliveStates != null && i < aliveStates.Length ? aliveStates[i] : true)
                    : true;
                
                string label;
                Color pointsColor = Color.white;
                
                if (useAliveStatus)
                {
                    label = isAlive ? "Vivo" : "Morto";
                }
                else if (usePercentage)
                {
                    // Mostra porcentagem para Race
                    label = $"{points[i]}%";
                    
                    // Determina cor baseada no progresso relativo
                    float currentProgress = points[i];
                    
                    if (maxProgress - minProgress > 0)
                    {
                        float normalizedRelativeProgress = (currentProgress - minProgress) / (maxProgress - minProgress);
                        if (normalizedRelativeProgress >= 0.7f) // Top 30%
                            pointsColor = Color.green;
                        else if (normalizedRelativeProgress >= 0.3f) // Middle 40%
                            pointsColor = Color.yellow;
                        else // Bottom 30%
                            pointsColor = Color.red;
                    }
                    else
                    {
                        // Todos com mesmo progresso
                        pointsColor = Color.white;
                    }
                }
                else
                {
                    label = points[i].ToString();
                }
                
                activeSlots[i].Refresh(i + 1, names[i], label, c, isAlive, useTeamColors, nameColor, pointsColor);
            }
            else
            {
                activeSlots[i].gameObject.SetActive(false);
            }
        }
    }

    void EnsureSlots(int required)
    {
        while (activeSlots.Count < required)
        {
            var go = Instantiate(slotPrefab, slotsParent);
            var slot = go.GetComponent<ScoreboardSlot>();
            activeSlots.Add(slot);
        }
    }
}
