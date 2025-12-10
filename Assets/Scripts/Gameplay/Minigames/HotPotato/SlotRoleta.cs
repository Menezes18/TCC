using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class SlotRoleta : NetworkBehaviour
{
    [Serializable]
    public class Entrada
    {
        public PlayerData playerData;
        public Sprite sprite;
        [Range(0.0001f, 1000f)] public float peso = 1f;

        public ulong steamId;
        public string alias;
        public int color;
    }

    [Header("Overlay/Global")]
    public CanvasGroup overlayGroup;

    [Header("UI")]
    public RectTransform viewport;
    public RectTransform content;

    [Header("Itens")]
    public List<Entrada> entradas;
    public float itemWidth = 120f;
    public float itemSpacing = 16f;

    [Header("Faixa")]
    public int loopsVisuais = 3;
    public float paddingStart = 80f;
    public float paddingEnd = 0f;

    [Header("Animação")]
    public float duracao = 3.2f;
    public float overshootPixels = 30f;

    [Header("Som")]
    public AudioSource tick;
    public float tickMinInterval = 0.04f;

    [Header("Debug")]
    public bool enableDebugHotkey = false;

    [Header("Seed")]
    public int fixedSeed = 0;

    [Header("Vitória (UI)")]
    public TextMeshProUGUI winText;
    public CanvasGroup winGroup;
    public float winShowTime = 0.75f;
    public float winStayTime = 0.75f;
    public float winHideTime = 0.45f;
    public LeanTweenType winEaseIn  = LeanTweenType.easeOutBack;
    public LeanTweenType winEaseOut = LeanTweenType.easeInQuad;
    
    [Header("Cores")]
    public Color winTextColor = new Color(1f, 0.3f, 0.1f); // Laranja/vermelho quente
    public Color subtextColor = new Color(1f, 0.85f, 0.2f); // Amarelo aviso

    [Header("Prefab do item")]
    public GameObject prefabUI;

    readonly List<RectTransform> _spawned = new();
    readonly List<int> _spawnedCatalogIndex = new();
    List<int> _perm;
    bool _girando;
    float _ultimoTick;
    System.Random _rng;
    readonly HashSet<ulong> _seen = new();

    public Action OnWinTextClosed;

    void Awake()
    {
        if (fixedSeed != 0) _rng = new System.Random(fixedSeed);

        if (overlayGroup)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }
    }

    public void StartRoletaNetwork()
    {
        if (isServer) ServerStartSpin();
        else CmdRequestStartSpin();
    }

    [Command(requiresAuthority = false)]
    void CmdRequestStartSpin() => ServerStartSpin();

    [Server]
    void ServerStartSpin()
    {
        if (_girando) return;

        var steamIds = ColetarSteamIdsValidosServidor();
        if (steamIds.Count == 0) return;

        int winnerIdx = UnityEngine.Random.Range(0, steamIds.Count);
        ulong winner = steamIds[winnerIdx];

        // monta snapshot simples (arrays paralelos)
        var sids = steamIds.ToArray();
        var aliases = new string[sids.Length];
        var colors = new int[sids.Length];

        for (int i = 0; i < sids.Length; i++)
        {
            ulong sid = sids[i];
            var pd =
                PlayerList.singleton?.players?.FirstOrDefault(p => p != null && p.playerInfo.steamId == sid) ??
                MyNetworkManager.manager?.allClients?.FirstOrDefault(p => p != null && p.playerInfo.steamId == sid);

            aliases[i] = (pd != null && !string.IsNullOrWhiteSpace(pd.alias))
                            ? pd.alias
                            : (pd != null && !string.IsNullOrWhiteSpace(pd.playerInfo.username)
                                ? pd.playerInfo.username
                                : $"Player {sid}");
            colors[i]  = pd != null ? pd.color : 0;
        }

        float total = TempoTotalUI();

        RpcSetOverlayVisible(true);
        RpcPrepareAndSpinSnapshot(sids, aliases, colors, winner, total);
        StartCoroutine(ServerHideAfter(total));
    }

    [ClientRpc]
    void RpcPrepareAndSpinSnapshot(ulong[] sids, string[] aliases, int[] colors, ulong winner, float totalTime)
    {
        SetupWinUI();

        entradas = new List<Entrada>(sids.Length);
        for (int i = 0; i < sids.Length; i++)
        {
            PlayerData pd = null;
            if (PlayerList.singleton?.players != null)
                pd = PlayerList.singleton.players.FirstOrDefault(p => p && p.playerInfo.steamId == sids[i]);

            entradas.Add(new Entrada {
                playerData = pd,
                sprite = pd ? pd.icon : null,
                peso = 1f,
                steamId = sids[i],
                alias = (aliases != null && i < aliases.Length) ? aliases[i] : $"Player {sids[i]}",
                color = (colors  != null && i < colors.Length)  ? colors[i]  : 0
            });
        }

        LimparFaixa();
        ConstruirFaixa();
        PosicionarInicioAleatorio();

        SpinToWinner(winner);

        OnWinTextClosed -= HandleClose;
        OnWinTextClosed += HandleClose;

        void HandleClose()
        {
            SetOverlayVisible(false);
            OnWinTextClosed -= HandleClose;
        }

        StartCoroutine(HideAfter(totalTime));
    }

    [Server]
    System.Collections.IEnumerator ServerHideAfter(float t)
    {
        yield return new WaitForSeconds(t);
        RpcSetOverlayVisible(false);
    }

    [ClientRpc]
    void RpcSetOverlayVisible(bool visible)
    {
        SetOverlayVisible(visible);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetOverlayVisible(bool visible)
    {
        RpcSetOverlayVisible(visible);
    }

    [ClientRpc]
    void RpcPrepareAndSpin(ulong[] steamIds, ulong winner, float totalTime)
    {
        SetupWinUI();
        SetEntriesFromSteamIds(steamIds);
        SpinToWinner(winner);

        OnWinTextClosed -= HandleClose;
        OnWinTextClosed += HandleClose;

        void HandleClose()
        {
            SetOverlayVisible(false);
            OnWinTextClosed -= HandleClose;
        }

        StartCoroutine(HideAfter(totalTime));
    }

    System.Collections.IEnumerator HideAfter(float t)
    {
        yield return new WaitForSeconds(t);
        SetOverlayVisible(false);
    }

    void SetOverlayVisible(bool visible)
    {
        if (!overlayGroup) return;

        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = visible;
        overlayGroup.blocksRaycasts = visible;
    }

    float TempoTotalUI()
    {
        return Mathf.Max(0.1f, duracao + winShowTime + 0.15f + winStayTime + winHideTime + 0.1f);
    }

    List<ulong> ColetarSteamIdsValidosServidor()
    {
        var set = new HashSet<ulong>();

        if (PlayerList.singleton != null && PlayerList.singleton.players != null)
            foreach (var pd in PlayerList.singleton.players)
                if (pd != null) set.Add(pd.playerInfo.steamId);

        if (MyNetworkManager.manager != null && MyNetworkManager.manager.allClients != null)
            foreach (var pd in MyNetworkManager.manager.allClients)
                if (pd != null) set.Add(pd.playerInfo.steamId);

        return set.ToList();
    }

    public void CriarEntrada()
    {
        entradas.Clear();

        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            if (pd == null) continue;
            ulong sid = pd.playerInfo.steamId;
            if (_seen.Contains(sid)) continue;
            _seen.Add(sid);

            var e = new Entrada
            {
                playerData = pd,
                sprite = pd.icon,
            };
            entradas.Add(e);
        }
    }

    void ConstruirFaixa()
    {
        if (viewport == null) { Debug.LogError("[SlotRoleta] viewport não atribuído."); return; }
        if (content  == null) { Debug.LogError("[SlotRoleta] content não atribuído.");  return; }
        if (entradas == null || entradas.Count == 0) { Debug.LogError("[SlotRoleta] preencha 'entradas'."); return; }

        LimparFaixa();

        FixAnchors(content, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));

        float step = itemWidth + itemSpacing;
        float halfView = Mathf.Max(1f, viewport.rect.width * 0.5f);

        float padL = Mathf.Max(paddingStart, halfView);
        float padR = Mathf.Max(paddingEnd,   halfView);

        int visiveis = Mathf.CeilToInt(viewport.rect.width / step);
        int bufferDireita = Mathf.CeilToInt((padR + overshootPixels + halfView) / step);

        int total = (loopsVisuais + 1) * entradas.Count + visiveis + bufferDireita + 2;

        List<int> ordem = new List<int>(total);
        int startOffset = UnityEngine.Random.Range(0, entradas.Count);
        while (ordem.Count < total)
        {
            List<int> perm = new List<int>(entradas.Count);
            for (int i = 0; i < entradas.Count; i++) perm.Add(i);
            Shuffle(perm);

            for (int i = 0; i < perm.Count && ordem.Count < total; i++)
            {
                int catIdx = perm[(i + startOffset) % perm.Count];
                ordem.Add(catIdx);
            }
            startOffset = 0;
        }

        float x = padL;
        for (int i = 0; i < ordem.Count; i++)
        {
            int catIdx = ordem[i];
            var e = entradas[catIdx];

            RectTransform rt = CreateTile(e, x);
            _spawned.Add(rt);
            _spawnedCatalogIndex.Add(catIdx);

            x += step;
        }

        content.sizeDelta = new Vector2(x - itemSpacing + padR, content.sizeDelta.y);
    }

    RectTransform CreateTile(Entrada e, float xLeftPadding)
    {
        var go = Instantiate(prefabUI, content, false);
        var rt = go.GetComponent<RectTransform>();
        FixAnchors(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = new Vector2(itemWidth, itemWidth);
        rt.anchoredPosition = new Vector2(xLeftPadding + itemWidth * 0.5f, 8f);

        var slot = go.GetComponent<UiSlotBatata>();
        if (slot != null)
        {
            string nome = NomeEntrada(e);
            int cor = (e.playerData != null) ? e.playerData.color : e.color;

            slot.Setup(nome, cor);
        }
        return rt;
    }

    public void SetEntriesFromSteamIds(IEnumerable<ulong> steamIds)
    {
        if (steamIds == null) return;

        entradas = new List<Entrada>();
        foreach (var sid in steamIds)
        {
            PlayerData pd = null;
            if (PlayerList.singleton?.players != null)
                pd = PlayerList.singleton.players.FirstOrDefault(p => p && p.playerInfo.steamId == sid);

            var alias = (pd != null && !string.IsNullOrWhiteSpace(pd.alias))
                            ? pd.alias
                            : (pd != null && !string.IsNullOrWhiteSpace(pd.playerInfo.username)
                                ? pd.playerInfo.username
                                : $"Player {sid}");

            var color = pd != null ? pd.color : 0;

            entradas.Add(new Entrada {
                playerData = pd,
                sprite = pd ? pd.icon : null,
                peso = 1f,
                steamId = sid,
                alias = alias,
                color = color
            });
        }

        LimparFaixa();
        ConstruirFaixa();
        PosicionarInicioAleatorio();
    }

    public void SpinToWinner(ulong steamId)
    {
        if (entradas == null || entradas.Count == 0) { Debug.LogWarning("Roleta sem entradas."); return; }

        int vencedorCatalog = entradas.FindIndex(e =>
            (e.playerData != null && e.playerData.playerInfo.steamId == steamId) ||
            (e.playerData == null && e.steamId == steamId)
        );
        if (vencedorCatalog < 0) { Debug.LogError("[Roleta] steamId não está nas entradas."); return; }

        HideWinTextImmediate();
        SpinToIndex(vencedorCatalog);
    }

    void SpinToIndex(int vencedorCatalog)
    {
        if (_girando || _spawned.Count == 0) return;

        _girando = true;
        LeanTween.cancel(content.gameObject);
        StopAllCoroutines();
        RebaseParaZero();

        int cicloAlvo = loopsVisuais + 1;
        int alvoIndex = -1, occ = 0;
        for (int i = 0; i < _spawnedCatalogIndex.Count; i++)
        {
            if (_spawnedCatalogIndex[i] == vencedorCatalog)
            {
                if (occ == cicloAlvo) { alvoIndex = i; break; }
                occ++;
            }
        }
        if (alvoIndex == -1)
            for (int i = _spawnedCatalogIndex.Count - 1; i >= 0; i--)
                if (_spawnedCatalogIndex[i] == vencedorCatalog) { alvoIndex = i; break; }

        float viewportCenter = viewport.rect.width * 0.5f;
        float itemCenterX = _spawned[alvoIndex].anchoredPosition.x;
        float startX = content.anchoredPosition.x;
        float targetX = -(itemCenterX - viewportCenter);

        float passo = itemWidth + itemSpacing;
        float cicloWidth = entradas.Count * passo;
        if (Mathf.Abs(targetX - startX) < cicloWidth * 0.75f && (alvoIndex + entradas.Count) < _spawned.Count)
        {
            alvoIndex += entradas.Count;
            itemCenterX = _spawned[alvoIndex].anchoredPosition.x;
            targetX = -(itemCenterX - viewportCenter);
        }

        float xOvershoot = targetX - Mathf.Abs(overshootPixels);
        float tMain = Mathf.Max(0.05f, duracao * 0.94f);
        float tBack = Mathf.Max(0.02f, duracao - tMain);

        _ultimoTick = -999f;
        if (tick != null) StartCoroutine(CoTickCentral());

        LeanTween.value(content.gameObject, startX, xOvershoot, tMain)
            .setEase(LeanTweenType.easeOutExpo)
            .setOnUpdate(x => content.anchoredPosition = new Vector2(x, 0f))
            .setOnComplete(() =>
            {
                LeanTween.value(content.gameObject, content.anchoredPosition.x, targetX, tBack)
                    .setEase(LeanTweenType.easeOutBack)
                    .setOnUpdate(x => content.anchoredPosition = new Vector2(x, 0f))
                    .setOnComplete(() =>
                    {
                        _girando = false;
                        content.anchoredPosition = new Vector2(targetX, 0f);
                        var vencedor = entradas[vencedorCatalog];
                        ShowWinText(NomeEntrada(vencedor));
                    });
            });
    }

    void PosicionarInicioAleatorio()
    {
        if (_spawned.Count == 0) { content.anchoredPosition = Vector2.zero; return; }

        int idx = NextInt(0, entradas.Count);
        float viewportCenter = viewport.rect.width * 0.5f;
        float chosenCenterX = _spawned[idx].anchoredPosition.x;
        float startAtCenter = -(chosenCenterX - viewportCenter);

        content.anchoredPosition = new Vector2(startAtCenter, 0f);
    }

    void LimparFaixa()
    {
        foreach (var rt in _spawned) if (rt) Destroy(rt.gameObject);
        _spawned.Clear();
        _spawnedCatalogIndex.Clear();
        _perm = null;
    }

    static void FixAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
    }

    int SortearCatalogIndexPonderado()
    {
        if (_rng == null) _rng = new System.Random(Environment.TickCount);

        double soma = 0;
        for (int i = 0; i < entradas.Count; i++) soma += Math.Max(0.0001, entradas[i].peso);

        double alvo = _rng.NextDouble() * soma;
        double acc = 0;
        for (int i = 0; i < entradas.Count; i++)
        {
            acc += Math.Max(0.0001, entradas[i].peso);
            if (alvo <= acc) return i;
        }
        return entradas.Count - 1;
    }

    public void Spin()
    {
        if (_girando || entradas == null || entradas.Count == 0 || _spawned.Count == 0) return;
        _girando = true;
        HideWinTextImmediate();
        LeanTween.cancel(content.gameObject);
        StopAllCoroutines();
        RebaseParaZero();

        int vencedorCatalog = SortearCatalogIndexPonderado();

        int cicloAlvo = loopsVisuais + 1;
        int alvoIndex = -1; int occ = 0;
        for (int i = 0; i < _spawnedCatalogIndex.Count; i++)
        {
            if (_spawnedCatalogIndex[i] == vencedorCatalog)
            {
                if (occ == cicloAlvo) { alvoIndex = i; break; }
                occ++;
            }
        }
        if (alvoIndex == -1)
            for (int i = _spawnedCatalogIndex.Count - 1; i >= 0; i--)
                if (_spawnedCatalogIndex[i] == vencedorCatalog) { alvoIndex = i; break; }

        float viewportCenter = viewport.rect.width * 0.5f;
        float itemCenterX = _spawned[alvoIndex].anchoredPosition.x;
        float startX = content.anchoredPosition.x;
        float targetX = -(itemCenterX - viewportCenter);

        float passo = itemWidth + itemSpacing;
        float cicloWidth = entradas.Count * passo;
        if (Mathf.Abs(targetX - startX) < cicloWidth * 0.75f && (alvoIndex + entradas.Count) < _spawned.Count)
        {
            alvoIndex += entradas.Count;
            itemCenterX = _spawned[alvoIndex].anchoredPosition.x;
            targetX = -(itemCenterX - viewportCenter);
        }

        float xOvershoot = targetX - Mathf.Abs(overshootPixels);

        float tMain = Mathf.Max(0.05f, duracao * 0.94f);
        float tBack = Mathf.Max(0.02f, duracao - tMain);

        _ultimoTick = -999f;
        if (tick != null) StartCoroutine(CoTickCentral());

        LeanTween.value(content.gameObject, startX, xOvershoot, tMain)
            .setEase(LeanTweenType.easeOutExpo)
            .setOnUpdate(x => content.anchoredPosition = new Vector2(x, 0f))
            .setOnComplete(() =>
            {
                LeanTween.value(content.gameObject, content.anchoredPosition.x, targetX, tBack)
                    .setEase(LeanTweenType.easeOutBack)
                    .setOnUpdate(x => content.anchoredPosition = new Vector2(x, 0f))
                    .setOnComplete(() =>
                    {
                        _girando = false;
                        content.anchoredPosition = new Vector2(targetX, 0f);
                        var vencedor = entradas[vencedorCatalog];
                        ShowWinText(NomeEntrada(vencedor));
                    });
            });
    }

    public void ShowOverlay(bool visible)
    {
        if (!overlayGroup) return;
        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = visible;
        overlayGroup.blocksRaycasts = visible;
    }

    void SetupWinUI()
    {
        if (winText == null) return;
        if (winGroup == null)
            winGroup = winText.GetComponent<CanvasGroup>() ?? winText.gameObject.AddComponent<CanvasGroup>();

        winGroup.alpha = 0f;
        winText.rectTransform.localScale = Vector3.one;
        winText.gameObject.SetActive(false);
    }

    void HideWinTextImmediate()
    {
        if (winText == null || winGroup == null) return;
        LeanTween.cancel(winText.gameObject);
        LeanTween.cancel(winGroup.gameObject);
        winGroup.alpha = 0f;
        winText.gameObject.SetActive(false);
    }

    void RebaseParaZero()
    {
        float shift = -content.anchoredPosition.x;
        if (Mathf.Approximately(shift, 0f)) return;
        for (int i = 0; i < _spawned.Count; i++)
            _spawned[i].anchoredPosition += new Vector2(shift, 0f);
        content.anchoredPosition = Vector2.zero;
    }

    void ShowWinText(string rotulo)
    {
        if (winText == null) return;

        string coloredName = $"<color=#{ColorUtility.ToHtmlStringRGB(winTextColor)}>{rotulo.ToUpper()}</color>";
        string coloredSubtext = $"<color=#{ColorUtility.ToHtmlStringRGB(subtextColor)}>Passe a banana rapidamente!</color>";
        
        winText.text = $"<size=115%><b>{coloredName} ESTÁ COM A BANANA QUENTE!</b></size>\n<size=75%>{coloredSubtext}</size>";
        winText.gameObject.SetActive(true);

        if (winGroup == null)
            winGroup = winText.GetComponent<CanvasGroup>() ?? winText.gameObject.AddComponent<CanvasGroup>();

        var rt = winText.rectTransform;

        LeanTween.cancel(winText.gameObject);
        LeanTween.cancel(winGroup.gameObject);
        winGroup.alpha = 0f;
        rt.localScale = Vector3.one * 0.6f;

        LTSeq seq = LeanTween.sequence();

        seq.append( LeanTween.alphaCanvas(winGroup, 1f, winShowTime * 0.6f) );
        seq.insert( LeanTween.scale(rt, Vector3.one * 1.15f, winShowTime).setEase(winEaseIn) );

        seq.append( LeanTween.scale(rt, Vector3.one, 0.15f).setEase(LeanTweenType.easeOutQuad) );
        seq.append( winStayTime );
        seq.append( LeanTween.alphaCanvas(winGroup, 0f, winHideTime).setEase(winEaseOut) );

        seq.append( () => {
            winText.gameObject.SetActive(false);
            OnWinTextClosed?.Invoke();
        } );
    }

    System.Collections.IEnumerator CoTickCentral()
    {
        while (_girando)
        {
            if (tick != null)
            {
                float now = Time.time;
                if (now - _ultimoTick >= tickMinInterval)
                {
                    _ultimoTick = now;
                    tick.Play();
                }
            }
            yield return null;
        }
    }


    string NomeEntrada(Entrada e)
    {
        if (e == null) return "Desconhecido";
        if (!string.IsNullOrWhiteSpace(e.alias)) return e.alias;
        if (e.playerData != null)
        {
            if (!string.IsNullOrWhiteSpace(e.playerData.alias)) return e.playerData.alias;
            if (!string.IsNullOrWhiteSpace(e.playerData.playerInfo.username)) return e.playerData.playerInfo.username;
        }
        return $"Player {e.steamId}";
    }

    public void PrepareEntriesSnapshot(ulong[] sids, string[] aliases, int[] colors)
    {
        if (sids == null || sids.Length == 0)
        {
            Debug.LogWarning("[SlotRoleta] Snapshot vazio.");
            return;
        }

        entradas = new List<Entrada>(sids.Length);

        for (int i = 0; i < sids.Length; i++)
        {
            ulong sid = sids[i];

            string alias =
                (aliases != null && i < aliases.Length && !string.IsNullOrWhiteSpace(aliases[i]))
                    ? aliases[i]
                    : $"Player {sid}";

            int color =
                (colors != null && i < colors.Length)
                    ? colors[i]
                    : 0;

            entradas.Add(new Entrada
            {
                playerData = null,
                sprite = null,
                peso = 1f,
                steamId = sid,
                alias = alias,
                color = color
            });
        }

        LimparFaixa();
        ConstruirFaixa();
        PosicionarInicioAleatorio();
    }

    int NextInt(int minInclusive, int maxExclusive)
    {
        if (_rng == null) return UnityEngine.Random.Range(minInclusive, maxExclusive);
        return _rng.Next(minInclusive, maxExclusive);
    }

    void Shuffle<T>(IList<T> list)
    {
        if (_rng == null) _rng = new System.Random(Environment.TickCount);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
