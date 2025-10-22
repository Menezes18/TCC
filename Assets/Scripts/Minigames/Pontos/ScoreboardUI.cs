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

        var soccer = FindAnyObjectByType<SoccerMinigameController>();
        for (int i = 0; i < ordered.Count; i++)
        {
            ulong id = ordered[i].Key;
            int score = ordered[i].Value;
            var pd = PlayerList.singleton.players.FirstOrDefault(p => p.playerInfo.steamId == id);
            names[i] = pd != null ? pd.alias : id.ToString();
            if (soccer != null)
            {
                int team = soccer.GetTeamOf(id);
                if (team == 0) names[i] = $"{names[i]} [Azul]";
                else if (team == 1) names[i] = $"{names[i]} [Vermelho]";
            }
            colors[i] = pd != null ? pd.color : -1;
            pts[i] = score;
        }

        // Dispatch the results via the Networked controller (spawned), not from this UI
        controller?.RpcUpdateScoreboard(names, pts, colors);
    }

    public void UpdateUI(string[] names, int[] points, int[] colors)
    {
        EnsureSlots(names.Length);

        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (i < names.Length)
            {
                Color c = Color.white;
                if (database != null && colors[i] >= 0 && colors[i] < database.playerColors.Count)
                    c = database.playerColors[colors[i]].color;
                activeSlots[i].gameObject.SetActive(true);
                string label;
                if (controller != null && controller.UseAliveStatusOnScoreboard)
                {
                    // Any non-zero treated as alive
                    label = points[i] != 0 ? "Vivo" : "Morto";
                }
                else
                {
                    label = points[i].ToString();
                }
                activeSlots[i].Refresh(i + 1, names[i], label, c);
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
