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
            rowComp.SetupForAnimation(names[i], gains[i], totals[i], i + 1, rowColor);

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
}
