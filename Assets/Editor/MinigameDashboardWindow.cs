using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


public class MinigameDashboardWindow : EditorWindow
{
    // Visual
    private Vector2 _scroll;
    private bool _autoRefresh = true;
    private double _lastRepaint;
    private double _refreshInterval = 0.5; // seconds

    // Foldouts
    private bool _foldSummary = true;
    private bool _foldGlobalScoreboard = true;
    private bool _foldCurrentMinigame = true;
    private bool _foldAllMinigames = false;
    private bool _foldLastResults = false;

    // Sorting
    private enum SortMode { ByPointsDesc, ByNameAsc }
    private SortMode _sortGlobal = SortMode.ByPointsDesc;
    private SortMode _sortMini = SortMode.ByPointsDesc;

    [MenuItem("Tools/TCC/Minigame Dashboard")] 
    public static void ShowWindow()
    {
        var win = GetWindow<MinigameDashboardWindow>(false, "Minigame Dashboard", true);
        win.minSize = new Vector2(640, 420);
        win.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    void EditorUpdate()
    {
        if (!_autoRefresh) return;
        if (!EditorApplication.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastRepaint > _refreshInterval)
        {
            _lastRepaint = now;
            Repaint();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Entre em Play Mode para visualizar pontuações e tempo em tempo real.", MessageType.Info);
        }

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;
            DrawSummarySection();
            DrawGlobalScoreboardSection();
            DrawCurrentMinigameSection();
            DrawAllMinigamesSection();
            DrawLastResultsSection();
            GUILayout.Space(12);
        }
    }

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Minigame Dashboard", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, new GUIContent("Auto Refresh"), EditorStyles.toolbarButton);

            GUILayout.Space(6);
            GUILayout.Label("Intervalo", GUILayout.Width(60));
            _refreshInterval = Math.Max(0.1, EditorGUILayout.DoubleField(_refreshInterval, GUILayout.Width(64)));

            GUILayout.Space(6);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Repaint();

            if (GUILayout.Button("Exportar CSV", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ExportCsv();
        }
    }

    void DrawSummarySection()
    {
        _foldSummary = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSummary, "Resumo");
        if (_foldSummary)
        {
            DrawBox(() =>
            {
                var mgr = FindObjectOfTypeSafe<MatchManager>();
                var net = MyNetworkManager.manager;
                var mc = FindObjectOfTypeSafe<MinigameController>();

                int totalPlayers = net?.scoreboard?.players?.Count ?? 0;
                string miniName = mc != null ? mc.GetType().Name : "(nenhum)";
                float matchTimer = mgr != null ? GetMatchTimerSafe(mgr) : -1f;
                float freezeTimer = mgr != null ? GetFreezeTimerViaReflection(mgr) : -1f;

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawStatCard("Jogadores", totalPlayers.ToString(), new Color(0.20f, 0.60f, 1f));
                    DrawStatCard("Minigame", miniName, new Color(0.50f, 0.90f, 0.50f));
                    DrawStatCard("Match", FormatTimer(matchTimer), new Color(1.00f, 0.50f, 0.20f));
                    DrawStatCard("Freeze", FormatTimer(freezeTimer), new Color(0.95f, 0.80f, 0.20f));
                }
            });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawGlobalScoreboardSection()
    {
        _foldGlobalScoreboard = EditorGUILayout.BeginFoldoutHeaderGroup(_foldGlobalScoreboard, "Scoreboard Global (Total)");
        if (_foldGlobalScoreboard)
        {
            DrawBox(() =>
            {
                var net = MyNetworkManager.manager;
                var db = FindObjectOfTypeSafe<Database>();
                var list = net?.scoreboard?.players ?? new List<DataPlayer>();

                IEnumerable<DataPlayer> ordered = list;
                if (_sortGlobal == SortMode.ByPointsDesc)
                    ordered = ordered.OrderByDescending(p => p.points).ThenBy(p => p.playerName);
                else
                    ordered = ordered.OrderBy(p => p.playerName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Ordenar:", GUILayout.Width(52));
                    _sortGlobal = (SortMode)EditorGUILayout.EnumPopup(_sortGlobal, GUILayout.Width(140));
                }

                DrawTableHeader("#", 24, "Jogador", 220, "Pontos", 70, "Cor", 60);

                int rank = 1;
                foreach (var p in ordered)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(rank.ToString(), GUILayout.Width(24));
                        GUILayout.Label(p.playerName ?? p.steamID.ToString(), GUILayout.Width(220));
                        GUILayout.Label(p.points.ToString(), GUILayout.Width(70));
                        Color col = db != null ? db.GetColor(p.color) : Color.white;
                        DrawColorSwatch(col, 56, 16);
                    }
                    rank++;
                }

                if (list.Count == 0)
                    EditorGUILayout.HelpBox("Scoreboard vazio (aguardando jogadores/partida).", MessageType.Info);
            });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawCurrentMinigameSection()
    {
        _foldCurrentMinigame = EditorGUILayout.BeginFoldoutHeaderGroup(_foldCurrentMinigame, "Minigame Atual (Live)");
        if (_foldCurrentMinigame)
        {
            DrawBox(() =>
            {
                var mc = FindObjectOfTypeSafe<MinigameController>();
                if (mc == null)
                {
                    EditorGUILayout.HelpBox("Nenhum MinigameController encontrado na cena.", MessageType.Warning);
                    return;
                }

                GUILayout.Label(mc.GetType().Name, EditorStyles.boldLabel);
                var live = SafeCall(mc.GetLiveScores);
                if (live == null || live.Count == 0)
                {
                    EditorGUILayout.HelpBox("Sem pontuação ao vivo disponível.", MessageType.Info);
                }
                else
                {
                    var ordered = _sortMini == SortMode.ByPointsDesc
                        ? live.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.ToString())
                        : live.OrderBy(kv => kv.Key.ToString());

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Ordenar:", GUILayout.Width(52));
                        _sortMini = (SortMode)EditorGUILayout.EnumPopup(_sortMini, GUILayout.Width(140));
                    }

                    DrawTableHeader("#", 24, "Jogador", 220, "Pontos", 70);
                    int idx = 1;
                    foreach (var kv in ordered)
                    {
                        string name = ResolvePlayerName(kv.Key);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(idx.ToString(), GUILayout.Width(24));
                            GUILayout.Label(name, GUILayout.Width(220));
                            GUILayout.Label(kv.Value.ToString(), GUILayout.Width(70));
                        }
                        idx++;
                    }
                }

                GUILayout.Space(6);
                if (GUILayout.Button("Atualizar", GUILayout.Width(100))) Repaint();
            });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawAllMinigamesSection()
    {
        _foldAllMinigames = EditorGUILayout.BeginFoldoutHeaderGroup(_foldAllMinigames, "Todos Minigames na Cena");
        if (_foldAllMinigames)
        {
            DrawBox(() =>
            {
                var all = FindObjectsOfTypeSafe<MinigameController>();
                if (all == null || all.Length == 0)
                {
                    EditorGUILayout.HelpBox("Nenhum MinigameController encontrado.", MessageType.Info);
                    return;
                }

                foreach (var mc in all)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        GUILayout.Label(mc.GetType().Name, EditorStyles.boldLabel);
                        var live = SafeCall(mc.GetLiveScores) ?? new Dictionary<ulong, int>();
                        if (live.Count == 0)
                        {
                            GUILayout.Label("(Sem pontuação ao vivo)");
                        }
                        else
                        {
                            DrawTableHeader("#", 24, "Jogador", 220, "Pontos", 70);
                            int idx = 1;
                            foreach (var kv in live.OrderByDescending(k => k.Value))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    GUILayout.Label(idx.ToString(), GUILayout.Width(24));
                                    GUILayout.Label(ResolvePlayerName(kv.Key), GUILayout.Width(220));
                                    GUILayout.Label(kv.Value.ToString(), GUILayout.Width(70));
                                }
                                idx++;
                            }
                        }
                    }
                }
            });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawLastResultsSection()
    {
        _foldLastResults = EditorGUILayout.BeginFoldoutHeaderGroup(_foldLastResults, "Último Minigame (Resultados Finais)");
        if (_foldLastResults)
        {
            DrawBox(() =>
            {
                var net = MyNetworkManager.manager;
                var res = net != null ? net.lastGameResults : null;
                if (res == null || res.Count == 0)
                {
                    EditorGUILayout.HelpBox("Sem resultados armazenados do último minigame.", MessageType.Info);
                    return;
                }
                DrawTableHeader("#", 24, "Jogador", 220, "Ganho", 70);
                int i = 1;
                foreach (var kv in res.OrderByDescending(k => k.Value))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(i.ToString(), GUILayout.Width(24));
                        GUILayout.Label(ResolvePlayerName(kv.Key), GUILayout.Width(220));
                        GUILayout.Label(kv.Value.ToString(), GUILayout.Width(70));
                    }
                    i++;
                }
            });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // Helpers
    static T FindObjectOfTypeSafe<T>() where T : UnityEngine.Object
    {
        // Find active or inactive in Editor
        var arr = Resources.FindObjectsOfTypeAll<T>();
        if (arr != null && arr.Length > 0)
        {
            // Prefer active in scene
            foreach (var o in arr)
            {
                var go = o as GameObject;
                if (o is Component c)
                    go = c.gameObject;
                if (go == null || EditorUtility.IsPersistent(go))
                    continue; // skip assets/prefabs
                return o;
            }
            return arr[0];
        }
        return null;
    }

    static T[] FindObjectsOfTypeSafe<T>() where T : UnityEngine.Object
    {
        var arr = Resources.FindObjectsOfTypeAll<T>();
        if (arr == null) return Array.Empty<T>();
        return arr.Where(o =>
        {
            var go = o as GameObject;
            if (o is Component c) go = c.gameObject;
            return go == null || !EditorUtility.IsPersistent(go);
        }).ToArray();
    }

    static float GetMatchTimerSafe(MatchManager mgr)
    {
        try { return mgr.MatchTimer; }
        catch { return -1f; }
    }

    static float GetFreezeTimerViaReflection(MatchManager mgr)
    {
        try
        {
            var fi = typeof(MatchManager).GetField("_freezeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null)
            {
                object v = fi.GetValue(mgr);
                if (v is float f) return f;
            }
        }
        catch {}
        return -1f;
    }

    static string FormatTimer(float t)
    {
        if (t < 0) return "--:--";
        int sec = Mathf.CeilToInt(t);
        int m = sec / 60; int s = sec % 60;
        return $"{m:00}:{s:00}";
    }

    static void DrawTableHeader(params object[] cells)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < cells.Length; i += 2)
            {
                string label = cells[i] as string;
                float w = Convert.ToSingle(cells[i + 1]);
                GUILayout.Label(label, EditorStyles.miniBoldLabel, GUILayout.Width(w));
            }
        }
        var rect = GUILayoutUtility.GetRect(1, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));
    }

    static void DrawColorSwatch(Color c, float w, float h)
    {
        var r = GUILayoutUtility.GetRect(w, h);
        EditorGUI.DrawRect(r, c);
        Handles.color = new Color(0, 0, 0, 0.5f);
        Handles.DrawAAPolyLine(2, new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax), new Vector3(r.xMin, r.yMin));
    }

    static void DrawBox(Action inner)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            inner?.Invoke();
        }
    }

    static void DrawStatCard(string title, string value, Color accent)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.MinWidth(120)))
        {
            var titleStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            var valueStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };

            var line = GUILayoutUtility.GetRect(1, 3);
            EditorGUI.DrawRect(line, accent);

            GUILayout.Space(2);
            GUILayout.Label(title, titleStyle);
            GUILayout.Label(value, valueStyle);
        }
    }

    static string ResolvePlayerName(ulong steamId)
    {
        try
        {
            var pd = PlayerList.singleton?.players?.FirstOrDefault(p => p.playerInfo.steamId == steamId);
            if (pd != null) return string.IsNullOrEmpty(pd.alias) ? steamId.ToString() : pd.alias;
            var net = MyNetworkManager.manager;
            if (net != null)
            {
                var data = net.scoreboard.players.FirstOrDefault(p => p.steamID == steamId);
                if (!string.IsNullOrEmpty(data.playerName)) return data.playerName;
            }
        }
        catch {}
        return steamId.ToString();
    }

    static Dictionary<ulong, int> SafeCall(Func<Dictionary<ulong, int>> getter)
    {
        try { return getter?.Invoke(); }
        catch { return new Dictionary<ulong, int>(); }
    }

    void ExportCsv()
    {
        var net = MyNetworkManager.manager;
        if (net == null || net.scoreboard == null || net.scoreboard.players.Count == 0)
        {
            EditorUtility.DisplayDialog("Exportar CSV", "Scoreboard vazio.", "Ok");
            return;
        }
        string dir = Path.Combine(Application.dataPath, "Editor/Reports");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"scoreboard_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using (var sw = new StreamWriter(path))
        {
            sw.WriteLine("Rank;SteamID;Nome;Pontos;CorIndex");
            int i = 1;
            foreach (var p in net.scoreboard.players.OrderByDescending(p => p.points))
            {
                sw.WriteLine($"{i};{p.steamID};{EscapeCsv(p.playerName)};{p.points};{p.color}");
                i++;
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(path);
    }

    static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(";") || s.Contains("\"") || s.Contains("\n"))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}

