using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;

// Runtime Dev Console (IMGUI) to control match flow and inspect state.
// Toggle: BackQuote (`) or F1.
// Auto-injected in all scenes.
public class DevConsole : MonoBehaviour
{
    private static DevConsole _instance;
    private readonly List<string> _log = new List<string>(256);
    private Vector2 _scroll;
    private string _input = string.Empty;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private bool _visible = false;
    private bool _autoScroll = true;
    private float _opacity = 0.92f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Ensure()
    {
        if (_instance != null) return;
        var go = new GameObject("__DevConsole");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<DevConsole>();
    }

    private void Awake()
    {
        Application.logMessageReceived += OnLog;
        Log("DevConsole pronto. Pressione ` ou F1 para abrir.");
        Log("Digite 'help' para ver comandos disponíveis.");
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F1))
        {
            _visible = !_visible;
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;
        var prev = GUI.color;
        var area = new Rect(12, 12, Screen.width - 24, Mathf.Min(Screen.height - 24, 360));
        var bg = new Color(0.10f, 0.10f, 0.12f, _opacity);
        GUI.color = bg; GUI.Box(area, GUIContent.none); GUI.color = prev;

        GUILayout.BeginArea(area);
        DrawToolbar();
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var line in _log)
            GUILayout.Label(line);
        if (_autoScroll)
            _scroll.y = 999999f;
        GUILayout.EndScrollView();

        GUI.SetNextControlName("DevConsoleInput");
        _input = GUILayout.TextField(_input);
        GUI.FocusControl("DevConsoleInput");

        var e = Event.current;
        if (e.isKey && e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                Submit(_input);
                _input = string.Empty;
                _historyIndex = -1;
                e.Use();
            }
            else if (e.keyCode == KeyCode.UpArrow)
            {
                if (_history.Count > 0)
                {
                    _historyIndex = Mathf.Clamp(_historyIndex < 0 ? _history.Count - 1 : _historyIndex - 1, 0, _history.Count - 1);
                    _input = _history[_historyIndex];
                }
                e.Use();
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                if (_history.Count > 0)
                {
                    if (_historyIndex >= 0) _historyIndex++;
                    if (_historyIndex >= _history.Count) { _historyIndex = -1; _input = string.Empty; }
                    else _input = _history[_historyIndex];
                }
                e.Use();
            }
        }

        GUILayout.EndArea();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"DevConsole | Net: srv={(NetworkServer.active ? 1 : 0)} cli={(NetworkClient.isConnected ? 1 : 0)} | Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        GUILayout.FlexibleSpace();
        _autoScroll = GUILayout.Toggle(_autoScroll, "AutoScroll", GUILayout.Width(90));
        GUILayout.Label("Opacidade", GUILayout.Width(70));
        _opacity = GUILayout.HorizontalSlider(_opacity, 0.3f, 1f, GUILayout.Width(120));
        if (GUILayout.Button("Limpar", GUILayout.Width(80))) _log.Clear();
        if (GUILayout.Button("X", GUILayout.Width(26))) _visible = false;
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void Submit(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;
        _history.Add(cmd);
        Log($"> {cmd}");
        try { Execute(cmd); }
        catch (Exception ex) { Log($"Erro: {ex.Message}"); }
    }

    private void Execute(string cmdLine)
    {
        var tokens = SplitArgs(cmdLine);
        if (tokens.Count == 0) return;
        string cmd = tokens[0].ToLowerInvariant();
        string Arg(int i, string def = "") => (i >= 0 && i < tokens.Count) ? tokens[i] : def;

        switch (cmd)
        {
            case "help":
                Log("Comandos:");
                Log(" - help");
                Log(" - clear");
                Log(" - start  (inicia/prepare partida)");
                Log(" - end    (encerra partida atual)");
                Log(" - restart (reinicia rotação/pontos)");
                Log(" - scoreboard  (pontos totais)");
                Log(" - live        (placar ao vivo do minigame)");
                Log(" - results     (último minigame ganhos)");
                Log(" - status      (vivos/mortos/frozen)");
                Log(" - freeze on|off");
                Log(" - timer <segundos>");
                Log(" - tp <all|nome|steamId> <x> <y> <z> [yRot]");
                Log(" - team        (times no Soccer)");
                Log(" - points add <alvo> <delta> | points set <alvo> <valor>");
                break;

            case "clear":
                _log.Clear();
                break;

            case "start":
                ServerAction("start", () =>
                {
                    var mm = FindObjectOfType<MatchManager>();
                    if (mm == null) { Log("MatchManager não encontrado"); return; }
                    // Chama o método privado via reflexão (mantém lógica de preparação)
                    var mi = typeof(MatchManager).GetMethod("InternalPrepareMath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null) mi.Invoke(mm, null);
                    else mm.InternalStartMatch();
                    Log("Partida preparada/iniciada.");
                }, () =>
                {
                    var mm = FindObjectOfType<MatchManager>();
                    if (mm != null) mm.CmdPrepareMath();
                    Log("Solicitado start ao servidor.");
                });
                break;

            case "end":
                RequireServer("end", () =>
                {
                    var mm = FindObjectOfType<MatchManager>();
                    if (mm == null) { Log("MatchManager não encontrado"); return; }
                    mm.InternalEndMatch();
                    Log("Partida encerrada.");
                });
                break;

            case "restart":
                RequireServer("restart", () =>
                {
                    MyNetworkManager.manager?.ReiniciarJogo();
                    Log("Jogo reiniciado: lista e pontos reset.");
                });
                break;

            case "scoreboard":
                PrintScoreboard();
                break;

            case "live":
                PrintLiveScores();
                break;

            case "results":
                PrintLastResults();
                break;

            case "status":
                PrintPlayersStatus();
                break;

            case "freeze":
                var mode = Arg(1, "");
                if (mode != "on" && mode != "off") { Log("Uso: freeze on|off"); break; }
                bool frozen = mode == "on";
                RequireServer("freeze", () =>
                {
                    foreach (var pd in PlayerList.singleton.players)
                    {
                        var ps = pd.GetComponent<PlayerScript>();
                        if (ps != null) ps.isFrozen = frozen;
                    }
                    Log($"freeze={(frozen ? 1 : 0)} aplicado a todos");
                });
                break;

            case "timer":
                if (!float.TryParse(Arg(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var t))
                { Log("Uso: timer <segundos>"); break; }
                RequireServer("timer", () =>
                {
                    var mm = FindObjectOfType<MatchManager>();
                    if (mm == null) { Log("MatchManager não encontrado"); return; }
                    mm.SetMatchTimer(t);
                    Log($"Timer setado para {t:0.##}s");
                });
                break;

            case "tp":
                if (tokens.Count < 5) { Log("Uso: tp <all|nome|steamId> <x> <y> <z> [yRot]"); break; }
                RequireServer("tp", () =>
                {
                    string who = Arg(1);
                    if (!float.TryParse(Arg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                        !float.TryParse(Arg(3), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                        !float.TryParse(Arg(4), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    { Log("Coordenadas inválidas"); return; }
                    float yRot = 0f; float.TryParse(Arg(5, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out yRot);

                    TeleportCommand(who, new Vector3(x, y, z), Quaternion.Euler(0, yRot, 0));
                });
                break;

            case "team":
                PrintSoccerTeams();
                break;

            case "points":
                HandlePoints(tokens);
                break;

            default:
                Log($"Comando desconhecido: {cmd}");
                break;
        }
    }

    private void ServerAction(string name, Action serverAction, Action clientAction)
    {
        if (NetworkServer.active)
        {
            serverAction?.Invoke();
        }
        else
        {
            clientAction?.Invoke();
        }
    }

    private void RequireServer(string name, Action serverAction)
    {
        if (!NetworkServer.active)
        {
            Log($"'{name}' é apenas para o Host/Servidor.");
            return;
        }
        serverAction?.Invoke();
    }

    private void PrintScoreboard()
    {
        var net = MyNetworkManager.manager;
        if (net == null || net.scoreboard == null || net.scoreboard.players.Count == 0)
        {
            Log("Scoreboard vazio.");
            return;
        }
        Log("#  Nome (SteamId) - Pontos [Cor]");
        int i = 1;
        foreach (var p in net.scoreboard.players.OrderByDescending(p => p.points))
        {
            Log($"{i,2}. {p.playerName} ({p.steamID}) - {p.points} [c{p.color}]");
            i++;
        }
    }

    private void HandlePoints(List<string> tokens)
    {
        if (tokens.Count < 4)
        {
            Log("Uso: points add <alvo> <delta> | points set <alvo> <valor>");
            return;
        }
        string mode = tokens[1].ToLowerInvariant();
        string who = tokens[2];
        if (!int.TryParse(tokens[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            Log("Valor inválido");
            return;
        }
        RequireServer("points", () =>
        {
            var net = MyNetworkManager.manager;
            if (net == null) { Log("MyNetworkManager nulo"); return; }
            var targets = ResolveTargets(who);
            int count = 0;
            foreach (var pd in targets)
            {
                var sbEntry = net.scoreboard.players.FirstOrDefault(p => p.steamID == pd.playerInfo.steamId);
                if (mode == "add")
                {
                    net.AddPoints(pd.playerInfo.steamId, value);
                    count++;
                }
                else if (mode == "set")
                {
                    int current = sbEntry.playerName != null ? sbEntry.points : 0;
                    int delta = value - current;
                    net.AddPoints(pd.playerInfo.steamId, delta);
                    count++;
                }
                else
                {
                    Log("Uso: points add|set <alvo> <valor>");
                    return;
                }
            }
            Log($"points/{mode} aplicado a {count} jogador(es).");
        });
    }

    private List<PlayerData> ResolveTargets(string token)
    {
        var list = PlayerList.singleton != null ? new List<PlayerData>(PlayerList.singleton.players) : new List<PlayerData>();
        if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            return new List<PlayerData>(list);
        if (ulong.TryParse(token, out var sid))
            return list.Where(p => p.playerInfo.steamId == sid).ToList();
        return list.Where(p =>
        {
            var name = (string.IsNullOrWhiteSpace(p.alias) ? p.playerInfo.username : p.alias) ?? string.Empty;
            return name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }).ToList();
    }

    private void PrintLiveScores()
    {
        var mc = GameObject.FindObjectOfType<MinigameController>();
        if (mc == null) { Log("Nenhum MinigameController ativo."); return; }
        var live = mc.GetLiveScores();
        if (live == null || live.Count == 0) { Log("Sem placar ao vivo disponível."); return; }
        Log($"Live - {mc.GetType().Name}");
        int i = 1;
        foreach (var kv in live.OrderByDescending(k => k.Value))
        {
            Log($"{i,2}. {ResolveName(kv.Key)} ({kv.Key}) - {kv.Value}");
            i++;
        }
    }

    private void PrintLastResults()
    {
        var res = MyNetworkManager.manager?.lastGameResults;
        if (res == null || res.Count == 0) { Log("Sem resultados do último minigame."); return; }
        Log("Último Minigame - Ganhos");
        int i = 1;
        foreach (var kv in res.OrderByDescending(k => k.Value))
        {
            Log($"{i,2}. {ResolveName(kv.Key)} ({kv.Key}) +{kv.Value}");
            i++;
        }
    }

    private void PrintPlayersStatus()
    {
        var list = PlayerList.singleton?.players;
        if (list == null || list.Count == 0) { Log("Sem jogadores."); return; }
        Log("Jogadores: nome (id) | dead | frozen | cor | cena");
        foreach (var pd in list)
        {
            if (pd == null) continue;
            var ps = pd.GetComponent<PlayerScript>();
            bool dead = ps != null && ps.IsDead;
            bool frozen = ps != null && ps.isFrozen;
            Log($" - {pd.alias} ({pd.playerInfo.steamId}) | {(dead?"DEAD":"alive")} | {(frozen?"FROZEN":"free")} | c{pd.color} | {pd.gameObject.scene.name}");
        }
    }

    private void TeleportCommand(string who, Vector3 pos, Quaternion rot)
    {
        var list = PlayerList.singleton != null ? new List<PlayerData>(PlayerList.singleton.players) : new List<PlayerData>();
        int count = 0;
        foreach (var pd in list)
        {
            if (pd == null) continue;
            if (!Match(pd, who)) continue;
            var ps = pd.GetComponent<PlayerScript>();
            if (ps == null) continue;
            var conn = pd.GetComponent<NetworkIdentity>()?.connectionToClient;
            if (conn == null) continue;
            ps.TargetRpcTeleport(conn, pos, rot);
            count++;
        }
        Log($"Teleporte aplicado a {count} jogador(es).");
    }

    private bool Match(PlayerData pd, string token)
    {
        if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase)) return true;
        if (ulong.TryParse(token, out var id)) return pd.playerInfo.steamId == id;
        var name = (string.IsNullOrWhiteSpace(pd.alias) ? pd.playerInfo.username : pd.alias) ?? string.Empty;
        return name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void PrintSoccerTeams()
    {
        var soccer = GameObject.FindObjectOfType<SoccerMinigameController>();
        if (soccer == null)
        {
            Log("SoccerMinigameController não encontrado.");
            return;
        }
        var a = soccer.teamAIds.ToList();
        var b = soccer.teamBIds.ToList();
        Log("Time A:");
        foreach (var sid in a)
            Log(" - " + ResolveName(sid) + " (" + sid + ")");
        Log("Time B:");
        foreach (var sid in b)
            Log(" - " + ResolveName(sid) + " (" + sid + ")");
    }

    private string ResolveName(ulong steamId)
    {
        try
        {
            var pd = PlayerList.singleton?.players?.FirstOrDefault(p => p.playerInfo.steamId == steamId);
            if (pd != null) return string.IsNullOrWhiteSpace(pd.alias) ? pd.playerInfo.username : pd.alias;
            var net = MyNetworkManager.manager;
            if (net != null)
            {
                var dp = net.scoreboard.players.FirstOrDefault(p => p.steamID == steamId);
                if (!string.IsNullOrWhiteSpace(dp.playerName)) return dp.playerName;
            }
        }
        catch { }
        return steamId.ToString();
    }

    private List<string> SplitArgs(string input)
    {
        List<string> result = new();
        if (string.IsNullOrEmpty(input)) return result;
        var cur = new StringBuilder();
        bool quote = false;
        foreach (char c in input)
        {
            if (c == '"') { quote = !quote; continue; }
            if (!quote && char.IsWhiteSpace(c))
            {
                if (cur.Length > 0) { result.Add(cur.ToString()); cur.Clear(); }
            }
            else cur.Append(c);
        }
        if (cur.Length > 0) result.Add(cur.ToString());
        return result;
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        string tag = type switch
        {
            LogType.Error => "[ERR] ",
            LogType.Warning => "[WRN] ",
            LogType.Exception => "[EXC] ",
            _ => string.Empty
        };
        Log(tag + condition);
    }

    private void Log(string msg)
    {
        _log.Add(msg);
        if (_log.Count > 500) _log.RemoveRange(0, _log.Count - 500);
    }
}
