using System.Collections.Generic;
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

    static readonly List<KeyValuePair<ulong,int>> _tmpOrdered = new(32);
    void SendResults(Dictionary<ulong, int> results)
    {
        if (results == null) return;
        _tmpOrdered.Clear();
        foreach (var kv in results) _tmpOrdered.Add(kv);
        _tmpOrdered.Sort((a,b)=> b.Value.CompareTo(a.Value));

        int count = _tmpOrdered.Count;
        string[] names = new string[count];
        int[] pts = new int[count];
        int[] colors = new int[count];

        var list = PlayerList.singleton?.players;
        for (int i = 0; i < count; i++)
        {
            ulong id = _tmpOrdered[i].Key;
            int score = _tmpOrdered[i].Value;
            PlayerData pd = null;
            if (list != null)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].playerInfo.steamId == id) { pd = list[j]; break; }
                }
            }
            names[i] = pd != null ? pd.alias : id.ToString();
            colors[i] = pd != null ? pd.color : -1;
            pts[i] = score;
        }

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
                activeSlots[i].Refresh(i + 1, names[i], points[i], c);
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