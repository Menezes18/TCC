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
            Dictionary<ulong, int> results = controller != null && controller.GetLiveScores().Count > 0
                ? controller.GetLiveScores()
                : MyNetworkManager.manager.lastGameResults;
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
        SendResults(mc.GetLiveScores());
    }

    void SendResults(Dictionary<ulong, int> results)
    {
        var ordered = results.OrderByDescending(kv => kv.Value).ToList();
        string[] names = new string[ordered.Count];
        int[] pts = new int[ordered.Count];
        int[] colors = new int[ordered.Count];
        bool[] aliveStates = new bool[ordered.Count];
        ulong[] steamIds = new ulong[ordered.Count];
        bool useTeamColors = controller is SoccerMinigameController;
        int[] teamIds = useTeamColors ? new int[ordered.Count] : null;

        var soccer = FindAnyObjectByType<SoccerMinigameController>();
        for (int i = 0; i < ordered.Count; i++)
        {
            ulong id = ordered[i].Key;
            int score = ordered[i].Value;
            var pd = PlayerList.singleton.players.FirstOrDefault(p => p.playerInfo.steamId == id);
            names[i] = pd != null ? pd.alias : id.ToString();
            steamIds[i] = id;
            if (teamIds != null) teamIds[i] = -1;
            if (useTeamColors && soccer != null)
            {
                int team = soccer.GetTeamOf(id);
                if (team == 0) names[i] = $"{names[i]} [Azul]";
                else if (team == 1) names[i] = $"{names[i]} [Vermelho]";
                if (teamIds != null) teamIds[i] = team;
            }
            colors[i] = pd != null ? pd.color : -1;
            pts[i] = score;
            aliveStates[i] = GetAliveStatus(id);
        }

        // Dispatch the results via the Networked controller (spawned), not from this UI
        controller?.RpcUpdateScoreboard(names, pts, colors, aliveStates, steamIds, teamIds);
    }

    private bool GetAliveStatus(ulong steamId)
    {
        var pd = PlayerList.singleton != null
            ? PlayerList.singleton.players.FirstOrDefault(p => p != null && p.playerInfo.steamId == steamId)
            : null;
        if (pd == null) return true;
        var ps = pd.GetComponent<PlayerScript>();
        return ps == null || !ps.IsDead;
    }

    public void UpdateUI(string[] names, int[] points, int[] colors, bool[] aliveStates, ulong[] steamIds, int[] teamIds)
    {
        EnsureSlots(names.Length);

        bool useTeamColors = controller is SoccerMinigameController;
        Color teamBlue = new Color(0.1f, 0.3f, 0.9f, 1f);
        Color teamRed = new Color(1.0f, 0.2f, 0.2f, 1f);
        SoccerMinigameController soccer = null;
        if (useTeamColors && (teamIds == null || teamIds.All(t => t < 0)))
            soccer = FindAnyObjectByType<SoccerMinigameController>();

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
                    else if (soccer != null)
                    {
                        int team = soccer.GetTeamOf(steamIds[i]);
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
                if (useAliveStatus)
                {
                    label = isAlive ? "Vivo" : "Morto";
                }
                else
                {
                    label = points[i].ToString();
                }
                activeSlots[i].Refresh(i + 1, names[i], label, c, isAlive, useTeamColors, nameColor);
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
