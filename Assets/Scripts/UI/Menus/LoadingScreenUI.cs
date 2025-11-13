using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using Mirror; // added

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    public static LoadingScreenUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("__LoadingScreenUI");
            Instance = go.AddComponent<LoadingScreenUI>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    [Header("Referências de UI")]
    [SerializeField] private GameObject panel;   
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    private AsyncOperation op;
    private string targetSceneName;
    private Coroutine progressRoutine; // added

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // If panel was assigned in scene, detach and persist
            if (panel != null)
            {
                panel.transform.SetParent(null);
                DontDestroyOnLoad(panel);
                panel.SetActive(false);
            }
        }
        else Destroy(gameObject);
    }

    public void SetMirrorTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }

    /// <summary>
    /// Inicia o carregamento assíncrono com timeout de 10s.
    /// </summary>
    public void Show(string sceneName)
    {
        EnsureRuntimeUI();
        targetSceneName = sceneName;
        if (panel != null) panel.SetActive(true);
        StartCoroutine(LoadWithTimeout());
        Debug.Log($"[LoadingUI] Show local scene load '{sceneName}'");
    }

    private IEnumerator LoadWithTimeout()
    {
        op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        float maxWait = 120f;

        while (op.progress < 0.9f && elapsed < maxWait)
        {
            elapsed += Time.unscaledDeltaTime;
            float prog = op.progress / 0.9f;
            if (progressBar != null) progressBar.value = prog;
            if (progressText != null) progressText.text = $"{(int)(prog * 100)}%";
            yield return null;
        }

        if (elapsed >= maxWait)
        {
            if (progressBar != null) progressBar.value = 1f;
            if (progressText != null) progressText.text = "100%";
        }

        if (panel != null) panel.SetActive(false);
        op.allowSceneActivation = true;
    }

    public void ShowForMirror()
    {
        EnsureRuntimeUI();
        if (panel != null)
        {
            if (progressBar != null) progressBar.value = 0f;
            if (progressText != null) progressText.text = "0%";
            panel.SetActive(true);
        }

        if (progressRoutine != null)
            StopCoroutine(progressRoutine);

        progressRoutine = StartCoroutine(TrackMirrorLoad());
        Debug.Log($"[LoadingUI] Show for Mirror scene '{targetSceneName}'");
    }

    private IEnumerator TrackMirrorLoad()
    {
        float elapsed = 0f;
        float maxWait = 120f;

        // Aguarda Mirror criar a AsyncOperation
        while (NetworkManager.loadingSceneAsync == null && elapsed < maxWait)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        var async = NetworkManager.loadingSceneAsync;
        if (async == null)
        {
            Debug.Log("[LoadingUI] Mirror had no loadingSceneAsync, hiding panel");
            Hide();
            progressRoutine = null;
            yield break;
        }

        while (!async.isDone && elapsed < maxWait)
        {
            elapsed += Time.unscaledDeltaTime;
            float raw = async.progress;
            float prog = raw < 0.9f ? raw / 0.9f : 1f;
            if (progressBar != null) progressBar.value = prog;
            if (progressText != null) progressText.text = $"{(int)(prog * 100)}%";
            yield return null;
        }

        progressRoutine = null;
    }

    public void Hide()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
        if (panel != null) panel.SetActive(false);
        Debug.Log("[LoadingUI] Hide");
    }

    /// <summary>
    /// Atualiza o progresso da barra de loading manualmente.
    /// Usado pelo SceneTransitionManager para mostrar progresso real.
    /// </summary>
    /// <param name="progress">Progresso de 0 a 1 (0% a 100%)</param>
    public void SetProgress(float progress)
    {
        EnsureRuntimeUI();
        float normalizedProgress = Mathf.Clamp01(progress);
        
        if (progressBar != null)
        {
            progressBar.value = normalizedProgress;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{(int)(normalizedProgress * 100)}%";
        }
    }

    private void EnsureRuntimeUI()
    {
        if (panel != null && progressBar != null)
            return;

        // Create a minimal overlay UI if not present
        var canvasGO = new GameObject("__LoadingUICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // force on top
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var sliderGO = new GameObject("Progress");
        sliderGO.transform.SetParent(panelGO.transform, false);
        var srt = sliderGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.2f, 0.45f);
        srt.anchorMax = new Vector2(0.8f, 0.55f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f;
        // background
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(1f,1f,1f,0.2f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f); bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.offsetMin = new Vector2(0f, 0f); bgRT.offsetMax = new Vector2(0f, 0f);
        slider.targetGraphic = bg;
        // fill
        var fillArea = new GameObject("Fill");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillImg = fillArea.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        var fillRT = fillArea.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f); fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = new Vector2(0f, 0f);
        slider.fillRect = fillRT;

        // optional percent text
        TextMeshProUGUI tmp = null;
        try
        {
            var textGO = new GameObject("PercentText");
            textGO.transform.SetParent(panelGO.transform, false);
            tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 32f;
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.3f, 0.57f);
            trt.anchorMax = new Vector2(0.7f, 0.67f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            tmp.text = "0%";
        }
        catch { }

        panel = panelGO;
        progressBar = slider;
        progressText = tmp;
        panel.SetActive(false);
    }
}
