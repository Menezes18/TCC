using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ResultsUI : MonoBehaviour
{

    #region Singleton Setup

    public static ResultsUI singleton;
    private void Awake()
    {
        singleton = this;
    }
    #endregion
    [SerializeField] CanvasGroup overlay; 
    [SerializeField] TMP_Text acabouText;
    [SerializeField] RectTransform listRoot; 
    [SerializeField] GameObject rowPrefab;

    [SerializeField] bool showAcabou = true;
    [SerializeField] Database db;

    private readonly List<GameObject> _spawned = new();

    [Header("Config Animação (Cards)")]
    [SerializeField, Min(0f)] float itemSlideDuration = 0.45f;
    [SerializeField, Min(0f)] float itemFadeDuration = 0.15f;
    [SerializeField, Min(0f)] float itemPopDuration = 0.26f;
    [SerializeField, Min(0f)] float itemInterval = 0.33f;
    [SerializeField, Min(0f)] float itemPopOvershoot = 1.15f;
    [SerializeField] float itemStartScale = 0.92f;
    [SerializeField] float slideExtraOffset = 120f;

    [Header("Números (Gain/Total)")]
    [SerializeField, Min(0f)] float gainCountDuration = 1.2f;
    [SerializeField, Min(0f)] float delayBetweenGainAndTotal = 0.30f;
    [SerializeField, Min(0f)] float totalCountDuration = 1.3f;

    [Header("Saída / Contagem para Sair")]
    [SerializeField] bool showExitTimer = true;
    [Min(0f)] public float exitTimerSeconds = 10f;
    [SerializeField] TMP_Text exitTimerText;

    [Header("Fase Global")]
    [SerializeField] bool showGlobalPhase = true;
    [SerializeField] string globalTitle = "GLOBAL";
    [SerializeField, Min(0f)] float delayBeforeGlobalPhase = 0.6f;
    [SerializeField, Min(0f)] float globalSlideOutDuration = 0.22f;
    [SerializeField, Min(0f)] float globalSlideInDuration = 0.30f;
    [SerializeField, Min(0f)] float globalItemStagger = 0.06f;
    [SerializeField] float globalWinnerPopScale = 1.1f;
    [SerializeField, Min(0f)] float globalWinnerPopUp = 0.12f;
    [SerializeField, Min(0f)] float globalWinnerPopDown = 0.10f;


    void Start()
    {
        Debug.Log("ResultsUI started " + gameObject.name);
    }

    private RectTransform EnsureAnimRoot(GameObject row)
    {
        var rowRT = (RectTransform)row.transform;
        var t = row.transform.Find("AnimRoot") as RectTransform;
        if (t == null)
        {
            var go = new GameObject("AnimRoot", typeof(RectTransform));
            t = go.GetComponent<RectTransform>();
            t.SetParent(rowRT, false);
            t.anchorMin = Vector2.zero;
            t.anchorMax = Vector2.one;
            t.pivot = new Vector2(0.5f, 0.5f);
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
            t.anchoredPosition = Vector2.zero;

            // Reparenta filhos existentes para dentro do AnimRoot
            var temp = new List<Transform>();
            for (int i = 0; i < rowRT.childCount; i++)
                temp.Add(rowRT.GetChild(i));
            foreach (var c in temp)
            {
                if (c == t) continue;
                c.SetParent(t, false);
            }
        }
        return t;
    }

    float _showStartTime;

    private int[] CalculatePositions(int[] totals)
    {
        int count = totals.Length;
        int[] positions = new int[count];
        
        if (count == 0) return positions;
        

        int currentPosition = 1;
        
        for (int i = 0; i < count; i++)
        {
            if (i > 0 && totals[i] < totals[i - 1])
            {

                currentPosition = i + 1;
            }
            positions[i] = currentPosition;
        }
        
        return positions;
    }

    public void Show(string[] names, int[] totals, int[] gains, Color32[] colors = null)
    {
        _showStartTime = Time.time;
        StopAllCoroutines();
        StartCoroutine(DoSequence(names, totals, gains, colors));
    }

    private IEnumerator DoSequence(string[] names, int[] totals, int[] gains, Color32[] colors = null)
    {
        if (listRoot == null || rowPrefab == null)
        {
            Debug.LogWarning("[ResultsUI] listRoot/rowPrefab não atribuídos. Configure no Inspector.");
            yield break;
        }

        foreach (var go in _spawned)
            if (go) Destroy(go);
        _spawned.Clear();

        if (overlay != null)
        {
            overlay.alpha = 0f;
            LeanTween.alphaCanvas(overlay, 1f, 0.2f).setEaseOutQuad();
        }

        if (showAcabou && acabouText != null)
        {
            var rt = (RectTransform)acabouText.transform;
            acabouText.alpha = 0f;
            rt.localScale = Vector3.one * 0.92f;
            LeanTween.value(acabouText.gameObject, 0f, 1f, 0.35f).setOnUpdate((float a) => acabouText.alpha = a).setEaseOutQuad();
            LeanTween.scale(rt, Vector3.one, 0.4f).setEaseOutBack();
            yield return new WaitForSeconds(0.9f);
            LeanTween.value(acabouText.gameObject, 1f, 0f, 0.2f).setOnUpdate((float a) => acabouText.alpha = a).setEaseInQuad();
            yield return new WaitForSeconds(0.21f);
        }

        int count = Mathf.Min(names?.Length ?? 0, totals?.Length ?? 0);
        if (gains == null || gains.Length != count)
        {
            var fixedG = new int[count];
            if (gains != null)
                System.Array.Copy(gains, fixedG, Mathf.Min(gains.Length, count));
            gains = fixedG;
        }

        // Determina as posições baseadas nos totais (considera empates)
        int[] positions = CalculatePositions(totals);
        
        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(rowPrefab, listRoot);
            _spawned.Add(row);

            Color rowColor = Color.white;
            
            // Prioriza cor do parâmetro (sincronizada via RPC)
            if (colors != null && i < colors.Length)
            {
                rowColor = colors[i];
            }
            else
            {
                // Fallback: tenta obter do scoreboard local
                try
                {
                    var manager = MyNetworkManager.manager;
                    if (manager != null && manager.scoreboard != null && db != null && db.playerColors != null)
                    {
                        var sb = manager.scoreboard.players;
                        if (i < sb.Count)
                        {
                            int ci = sb[i].color;
                            if (ci >= 0 && ci < db.playerColors.Count)
                                rowColor = db.playerColors[ci].color;
                        }
                    }
                }
                catch { }
            }

            var rowComp = row.GetComponent<ResultsRow>();
            rowComp.SetupForAnimation(names[i], gains[i], totals[i], positions[i], rowColor);
            
            TryLoadPlayerCustomization(rowComp, names[i]);

            var animRT = EnsureAnimRoot(row);
            var cg = animRT.GetComponent<CanvasGroup>();
            if (cg == null) cg = animRT.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            animRT.localScale = Vector3.one * itemStartScale;

            LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
            float containerW = Mathf.Max(listRoot.rect.width, ((RectTransform)row.transform).rect.width);
            float off = containerW + slideExtraOffset; 
            Vector2 targetPos = Vector2.zero;
            animRT.anchoredPosition = new Vector2(-off, 0f);

            LeanTween.alphaCanvas(cg, 1f, itemFadeDuration).setEaseOutQuad();
            LeanTween.move(animRT, targetPos, itemSlideDuration)
                .setEaseOutCubic()
                .setOnComplete(() =>
                {
                    _pendingNumberAnims++;
                    StartCoroutine(RunRowNumbers(rowComp));
                });
            LeanTween.scale(animRT, Vector3.one, itemPopDuration).setEaseOutBack().setOvershoot(itemPopOvershoot);

            yield return new WaitForSeconds(itemInterval);
        }

        while (_pendingNumberAnims > 0)
            yield return null;

        // Sequência única de resultados: não entra mais na fase global separada

        if (showExitTimer)
            yield return DoExitCountdown(exitTimerSeconds);
    }

    private int _pendingNumberAnims;

    private IEnumerator RunRowNumbers(ResultsRow row)
    {
        yield return row.PlayNumberSequence(gainCountDuration, delayBetweenGainAndTotal, totalCountDuration);
        _pendingNumberAnims = Mathf.Max(0, _pendingNumberAnims - 1);
    }

    private IEnumerator DoExitCountdown(float seconds)
    {
        var text = exitTimerText != null ? exitTimerText : acabouText;
        if (text == null)
            yield break;

        var rt = (RectTransform)text.transform;
        text.alpha = 0f;
        rt.localScale = Vector3.one * 0.98f;
        LeanTween.value(text.gameObject, 0f, 1f, 0.25f).setOnUpdate((float a) => text.alpha = a).setEaseOutQuad();
        LeanTween.scale(rt, Vector3.one, 0.25f).setEaseOutBack();

        float t = Mathf.Max(0f, seconds);
        while (t > 0f)
        {
            int s = Mathf.CeilToInt(t);
            text.text = $"Sair em {s}s";
            t -= Time.deltaTime;
            yield return null;
        }

        LeanTween.value(text.gameObject, 1f, 0f, 0.2f).setOnUpdate((float a) => text.alpha = a).setEaseInQuad();
        yield return new WaitForSeconds(0.21f);
        MatchManager.singleton.StartCoroutine(MatchManager.singleton.WaitAndReturnToLobby(1f));
    }

    private IEnumerator RunGlobalPhase(string[] names, int[] totals, Color32[] colors)
    {
        if (listRoot == null || _spawned.Count == 0) yield break;

        if (!string.IsNullOrWhiteSpace(globalTitle) && acabouText != null)
        {
            yield return new WaitForSeconds(delayBeforeGlobalPhase);
            var rt = (RectTransform)acabouText.transform;
            acabouText.text = globalTitle;
            acabouText.alpha = 0f;
            rt.localScale = Vector3.one * 0.92f;
            LeanTween.value(acabouText.gameObject, 0f, 1f, 0.28f).setOnUpdate((float a) => acabouText.alpha = a).setEaseOutQuad();
            LeanTween.scale(rt, Vector3.one, 0.3f).setEaseOutBack();
            yield return new WaitForSeconds(0.45f);
        }

        int count = Mathf.Min(names?.Length ?? 0, totals?.Length ?? 0);
        var indices = new List<int>(count);
        for (int i = 0; i < count; i++) indices.Add(i);
        indices.Sort((a, b) => totals[b].CompareTo(totals[a]));

        LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
        float containerW = listRoot.rect.width;
        float off = containerW + slideExtraOffset;

        for (int i = 0; i < _spawned.Count; i++)
        {
            var row = _spawned[i];
            if (row == null) continue;
            var animRT = EnsureAnimRoot(row);
            var cg = animRT.GetComponent<CanvasGroup>();
            if (cg == null) cg = animRT.gameObject.AddComponent<CanvasGroup>();
            LeanTween.alphaCanvas(cg, 0.0f, globalSlideOutDuration).setEaseInQuad();
            LeanTween.move(animRT, new Vector2(off, 0f), globalSlideOutDuration).setEaseInCubic();
            yield return new WaitForSeconds(globalItemStagger);
        }

        var newOrder = new List<GameObject>(count);
        foreach (var idx in indices)
        {
            if (idx >= 0 && idx < _spawned.Count)
                newOrder.Add(_spawned[idx]);
        }

        for (int i = 0; i < newOrder.Count; i++)
        {
            var row = newOrder[i];
            if (row == null) continue;
            row.transform.SetSiblingIndex(i);
            var comp = row.GetComponent<ResultsRow>();
            if (comp != null) comp.SetPosition(i + 1);
        }
        _spawned.Clear();
        _spawned.AddRange(newOrder);

        for (int i = 0; i < _spawned.Count; i++)
        {
            var row = _spawned[i];
            if (row == null) continue;
            var animRT = EnsureAnimRoot(row);
            var cg = animRT.GetComponent<CanvasGroup>();
            if (cg == null) cg = animRT.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            animRT.anchoredPosition = new Vector2(-off, 0f);
            LeanTween.alphaCanvas(cg, 1f, globalSlideInDuration).setEaseOutQuad();
            LeanTween.move(animRT, Vector2.zero, globalSlideInDuration).setEaseOutCubic();
            yield return new WaitForSeconds(globalItemStagger);
        }

        if (_spawned.Count > 0)
        {
            var leader = _spawned[0];
            var animRT = EnsureAnimRoot(leader);
            LeanTween.scale(animRT, Vector3.one * Mathf.Max(1f, globalWinnerPopScale), Mathf.Max(0f, globalWinnerPopUp)).setEaseOutBack();
            if (globalWinnerPopUp > 0f) yield return new WaitForSeconds(globalWinnerPopUp);
            LeanTween.scale(animRT, Vector3.one, Mathf.Max(0f, globalWinnerPopDown)).setEaseInQuad();
            if (globalWinnerPopDown > 0f) yield return new WaitForSeconds(globalWinnerPopDown);
        }
    }
    
    private void TryLoadPlayerCustomization(ResultsRow row, string playerName)
    {
        if (row == null) return;
        
        var bannerModel = row.GetComponent<ResultsBannerModel>();
        if (bannerModel == null)
        {
            bannerModel = row.GetComponentInChildren<ResultsBannerModel>();
        }
        
        if (bannerModel == null)
        {
            Debug.LogWarning($"[ResultsUI] ResultsBannerModel não encontrado para {playerName}");
            return;
        }
        
        PlayerCustomizationData customization = FindPlayerCustomization(playerName);
        
        if (customization != null)
        {
            bannerModel.LoadModel(customization);
            Debug.Log($"[ResultsUI] Customização carregada para {playerName}: {customization}");
        }
        else
        {
            bannerModel.LoadModel(null);
            Debug.LogWarning($"[ResultsUI] Customização não encontrada para {playerName}, usando padrão");
        }
    }
    
    private PlayerCustomizationData FindPlayerCustomization(string playerName)
    {
        var allPlayers = FindObjectsByType<PlayerCustomizationSync>(FindObjectsSortMode.None);
        
        Debug.Log($"[ResultsUI] Procurando customização para '{playerName}'. Total de players na cena: {allPlayers.Length}");
        
        foreach (var playerSync in allPlayers)
        {
            var playerData = playerSync.GetComponent<PlayerData>();
            if (playerData != null)
            {
                string syncName = !string.IsNullOrEmpty(playerData.alias) ? playerData.alias : playerData.playerInfo.username;
                
                Debug.Log($"[ResultsUI] Comparando: Procurando='{playerName}' | PlayerSync='{syncName}' | alias='{playerData.alias}' | username='{playerData.playerInfo.username}' | isLocal={playerData.isLocalPlayer}");
                
                bool match = string.Equals(syncName, playerName, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(playerData.alias, playerName, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(playerData.playerInfo.username, playerName, System.StringComparison.OrdinalIgnoreCase);
                
                if (match)
                {
                    var customization = playerSync.GetCustomization();
                    Debug.Log($"[ResultsUI] ✓ MATCH! Player '{syncName}' | Hat={customization.hatIndex}, Glasses={customization.glassesIndex}, Shirt={customization.shirtIndex}");
                    return customization;
                }
            }
            else
            {
                Debug.LogWarning($"[ResultsUI] PlayerCustomizationSync sem PlayerData: {playerSync.gameObject.name}");
            }
        }
        
        Debug.LogWarning($"[ResultsUI] Nenhum PlayerCustomizationSync encontrado para '{playerName}'. Tentando fallback local...");
        
        if (CustomizationManager.Instance != null)
        {
            var localCustomization = CustomizationManager.Instance.GetCurrentCustomization();
            if (localCustomization != null)
            {
                var localPlayerName = GetLocalPlayerName();
                Debug.Log($"[ResultsUI] Fallback: Nome local='{localPlayerName}' vs Procurando='{playerName}'");
                
                if (string.Equals(localPlayerName, playerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[ResultsUI] Usando customização local: Hat={localCustomization.hatIndex}");
                    return localCustomization;
                }
            }
        }
        
        Debug.LogError($"[ResultsUI] FALHA TOTAL: Nenhuma customização encontrada para '{playerName}'");
        return null;
    }
    
    private string GetLocalPlayerName()
    {
        var playerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        
        Debug.Log($"[ResultsUI] Procurando player local. Total PlayerData encontrados: {playerDatas.Length}");
        
        foreach (var data in playerDatas)
        {
            Debug.Log($"[ResultsUI] PlayerData: name='{data.gameObject.name}' | alias='{data.alias}' | username='{data.playerInfo.username}' | isLocal={data.isLocalPlayer}");
            
            if (data.isLocalPlayer)
            {
                string name = !string.IsNullOrEmpty(data.alias) ? data.alias : data.playerInfo.username;
                Debug.Log($"[ResultsUI] Player local encontrado: '{name}'");
                return name;
            }
        }
        
        Debug.LogWarning("[ResultsUI] NENHUM player local encontrado!");
        return string.Empty;
    }
}
