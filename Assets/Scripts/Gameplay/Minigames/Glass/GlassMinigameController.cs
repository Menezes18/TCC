using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GlassMinigameController : MinigameController
{
    [SerializeField] private SettingsMiniGameData settingsData;
    [SerializeField] private float brokenRestoreDelay = 2.0f;
    [Header("Random Path")]
    [Tooltip("Se marcado, ignora o PathData e gera um caminho aleatório.")]
    [SerializeField] private bool randomizeEachMatch = true;
    [Header("Binding")]
    [Tooltip("Se preencher, usa exatamente estes tiles nesta ordem; caso vazio, procura nos filhos do controller e, por fim, na cena inteira.")]
    [SerializeField] private List<GlassTile> tilesByInspector = new();
    [Header("Path Config")]
    [SerializeField] private GlassPathData pathData; // caminho pré-definido (Left/Right por linha)

    private bool _matchActive;
    private readonly Dictionary<int, (GlassTile left, GlassTile right)> _rows = new();
    private readonly Dictionary<int, int> _safeSideByRow = new(); // 0 = left, 1 = right

    private readonly Dictionary<ulong, int> _lastRowByPlayer = new();
    private readonly Dictionary<ulong, int> _finalPointsByPlayer = new();
    private readonly Dictionary<ulong, int> _liveScoresByPlayer = new();
    private readonly List<ulong> _finishOrder = new();

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerAutoBindTiles();
        if (!randomizeEachMatch)
        {
            ServerBuildPathFromAssetOrRandom();
            ServerApplySafeFlagsToTiles();
        }
    }

    [Server]
    private void ServerAutoBindTiles()
    {
        _rows.Clear();
        IEnumerable<GlassTile> tilesEnum = null;
        if (tilesByInspector != null && tilesByInspector.Count > 0)
        {
            tilesEnum = tilesByInspector.Where(t => t != null);
        }
        else
        {
            var childTiles = GetComponentsInChildren<GlassTile>(true);
            if (childTiles != null && childTiles.Length > 0)
                tilesEnum = childTiles;
            else
                tilesEnum = FindObjectsByType<GlassTile>(FindObjectsSortMode.None);
        }

        foreach (var t in tilesEnum)
        {
            t.ServerBindController(this);
            if (!_rows.TryGetValue(t.rowIndex, out var pair))
                pair = (null, null);
            if (t.side == 0) pair.left = t; else pair.right = t;
            _rows[t.rowIndex] = pair;
        }
    }

    [ContextMenu("Rebuild Tiles From Children (Server)")]
    private void ContextRebuildFromChildren()
    {
        if (!isServer) return;
        tilesByInspector.Clear();
        var childTiles = GetComponentsInChildren<GlassTile>(true);
        if (childTiles != null && childTiles.Length > 0)
            tilesByInspector.AddRange(childTiles);
        ServerAutoBindTiles();
        ServerBuildPathFromAssetOrRandom();
        ServerApplySafeFlagsToTiles();
    }

    [Server]
    private void ServerBuildPathFromAssetOrRandom()
    {
        _safeSideByRow.Clear();
        if (!randomizeEachMatch && pathData != null)
        {
            foreach (var row in _rows.Keys)
                _safeSideByRow[row] = Mathf.Clamp(pathData.GetSafeSide(row), 0, 1);
            var orderedRowsPd = _rows.Keys.OrderBy(k => k).ToList();
            var seqPd = string.Join("", orderedRowsPd.Select(r => (Mathf.Clamp(pathData.GetSafeSide(r), 0, 1) == 0) ? "L" : "R"));
            Debug.Log($"[Glass] Sequência (PathData) L/R: {seqPd}");
        }
        else
        {
            foreach (var row in _rows.Keys)
                _safeSideByRow[row] = Random.value < 0.5f ? 0 : 1;

            var orderedRows = _rows.Keys.OrderBy(k => k).ToList();
            
            var seq = string.Join("", orderedRows.Select(r => (_safeSideByRow.TryGetValue(r, out var s) ? s : 0) == 0 ? "L" : "R"));
            Debug.Log($"[Glass] Sequência aleatória L/R: {seq}");
        }

    }

    [Server]
    private void ServerApplySafeFlagsToTiles()
    {
        foreach (var kv in _rows)
        {
            int row = kv.Key;
            int safeSide = _safeSideByRow.TryGetValue(row, out var s) ? s : 0;
            var pair = kv.Value;
            if (pair.left != null) pair.left.ServerSetSafe(safeSide == 0);
            if (pair.right != null) pair.right.ServerSetSafe(safeSide == 1);
            if (pair.left != null) pair.left.ServerSetRestoreDelay(brokenRestoreDelay);
            if (pair.right != null) pair.right.ServerSetRestoreDelay(brokenRestoreDelay);
        }
    }

    [Server]
    public override void StartMatch()
    {
        base.StartMatch();
        if (randomizeEachMatch)
        {
            ServerBuildPathFromAssetOrRandom();
            ServerApplySafeFlagsToTiles();
        }
        _matchActive = true;
        _finishOrder.Clear();
        _finalPointsByPlayer.Clear();
        _liveScoresByPlayer.Clear();
        _lastRowByPlayer.Clear();
        Notifica();
    }

    [Server]
    public override void EndMatch()
    {
        _matchActive = false;
        AssignFinalPoints();
        base.EndMatch();
    }

    [Server]
    public override void UpdateScores()
    {
        if (!_matchActive) return;
        _liveScoresByPlayer.Clear();
        foreach (var pd in PlayerList.singleton.players)
        {
            ulong id = pd.playerInfo.steamId;
            _liveScoresByPlayer[id] = _lastRowByPlayer.TryGetValue(id, out var r) ? r : 0;
        }
        Notifica();
    }

    [Server]
    public override void AssignFinalPoints()
    {
        _finalPointsByPlayer.Clear();
        for (int i = 0; i < _finishOrder.Count; i++)
        {
            int pts = i switch
            {
                0 => settingsData?.firstPlaceBonus ?? 0,
                1 => settingsData?.secondPlaceBonus ?? 0,
                2 => settingsData?.thirdPlaceBonus ?? 0,
                3 => settingsData?.fourthPlaceBonus ?? 0,
                _ => 0
            };
            _finalPointsByPlayer[_finishOrder[i]] = pts;
        }
        // quem não terminou não recebe pontos
        foreach (var pd in PlayerList.singleton.players)
        {
            ulong id = pd.playerInfo.steamId;
            if (!_finalPointsByPlayer.ContainsKey(id))
                _finalPointsByPlayer[id] = 0;
        }
        Notifica();
    }

    public override Dictionary<ulong, int> GetResults() =>
        _finalPointsByPlayer.Count > 0 ? _finalPointsByPlayer : _liveScoresByPlayer;
    public override Dictionary<ulong, int> GetLiveScores() => _liveScoresByPlayer;

    // ===== Eventos dos Tiles/Finish =====
    [Server]
    public void ServerOnSafeTileStepped(PlayerData pd, int rowIndex)
    {
        if (!_matchActive || pd == null) return;
        ulong id = pd.playerInfo.steamId;
        if (!_lastRowByPlayer.TryGetValue(id, out var cur) || rowIndex > cur)
            _lastRowByPlayer[id] = rowIndex;
    }

    [Server]
    public void ServerOnPlayerFinish(PlayerData pd)
    {
        if (!_matchActive || pd == null) return;
        ulong id = pd.playerInfo.steamId;
        if (_finishOrder.Contains(id)) return;
        _finishOrder.Add(id);
        MatchManager.singleton?.AddWinnerPlayer(pd);
    }
}
