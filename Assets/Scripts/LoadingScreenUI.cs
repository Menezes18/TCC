using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using Mirror; // added

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [Header("UI References")]
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

            // If no panel assigned from a scene, create a basic UI
            if (panel == null)
                CreateDefaultUI();

            // Ensure panel is under a Canvas and persist both across scenes
            var parentCanvas = panel.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                var canvasGO = new GameObject("LoadingScreen_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32767; // top-most
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                panel.transform.SetParent(canvasGO.transform, false);
                DontDestroyOnLoad(canvasGO);
            }
            else
            {
                parentCanvas.sortingOrder = 32767; // ensure on top
                DontDestroyOnLoad(parentCanvas.gameObject);
            }

            // If external panel exists but no slider/text, create them under panel
            if (progressBar == null)
                progressBar = CreateProgressBar(panel.transform);
            if (progressText == null)
                progressText = CreateProgressText(panel.transform);

            DontDestroyOnLoad(panel);
            panel.SetActive(false);
        }
        else Destroy(gameObject);
    }

    // Begin an internal async load (non-Mirror)
    public void Show(string sceneName)
    {
        targetSceneName = sceneName;
        if (panel != null) panel.SetActive(true);
        if (progressBar != null) progressBar.gameObject.SetActive(true);
        StartCoroutine(LoadWithTimeout());
    }

    private IEnumerator LoadWithTimeout()
    {
        op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        float maxWait = 120f;

        // Update progress up to 90% or until timeout
        while (op.progress < 0.9f && elapsed < maxWait)
        {
            elapsed += Time.unscaledDeltaTime;
            float prog = op.progress / 0.9f;
            if (progressBar != null) progressBar.value = prog;
            if (progressText != null) progressText.text = $"{(int)(prog * 100)}%";
            yield return null;
        }

        // If timeout, force UI to 100%
        if (elapsed >= maxWait)
        {
            if (progressBar != null) progressBar.value = 1f;
            if (progressText != null) progressText.text = "100%";
        }

        // Hide panel before activating
        if (panel != null) panel.SetActive(false);
        op.allowSceneActivation = true;
    }

    // NEW: Mostrar a UI e acompanhar progresso quando o Mirror muda de cena
    public void ShowForMirror()
    {
        if (panel != null)
        {
            if (progressBar != null) progressBar.value = 0f;
            if (progressText != null) progressText.text = "0%";
            panel.SetActive(true);
        }

        if (progressRoutine != null)
            StopCoroutine(progressRoutine);

        progressRoutine = StartCoroutine(TrackMirrorLoad());
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
        if (async != null)
        {
            while (!async.isDone && elapsed < maxWait)
            {
                elapsed += Time.unscaledDeltaTime;
                float raw = async.progress;
                float prog = raw < 0.9f ? raw / 0.9f : 1f;
                if (progressBar != null) progressBar.value = prog;
                if (progressText != null) progressText.text = $"{(int)(prog * 100)}%";
                yield return null;
            }
        }

        // Mantém o painel visível; o MyNetworkManager ocultará ao terminar o load
        progressRoutine = null;
    }

    /// <summary>
    /// Caso você queira esconder manualmente em outro ponto.
    /// </summary>
    public void Hide()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
        if (panel != null) panel.SetActive(false);
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }
        if (progressText != null) progressText.text = string.Empty;
    }

    private void CreateDefaultUI()
    {
        // Root canvas
        var canvasGO = new GameObject("LoadingScreen_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(gameObject.transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Background panel
        var panelGO = new GameObject("Panel", typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Progress bar with background and fill
        var bar = CreateProgressBar(panelGO.transform);

        // Progress text
        var tmp = CreateProgressText(panelGO.transform);

        // Assign references
        panel = panelGO;
        progressBar = bar;
        progressText = tmp;

        DontDestroyOnLoad(canvasGO);
        DontDestroyOnLoad(panelGO);
    }

    private Slider CreateProgressBar(Transform parent)
    {
        var barGO = new GameObject("ProgressBar", typeof(Slider));
        barGO.transform.SetParent(parent, false);
        var bar = barGO.GetComponent<Slider>();
        bar.transition = Selectable.Transition.None;
        bar.navigation = new Navigation { mode = Navigation.Mode.None };
        bar.minValue = 0f; bar.maxValue = 1f; bar.value = 0f;
        bar.direction = Slider.Direction.LeftToRight;
        var barRect = barGO.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.sizeDelta = new Vector2(600f, 24f);
        barRect.anchoredPosition = new Vector2(0f, -20f);

        // Background image
        var bgGO = new GameObject("Background", typeof(Image));
        bgGO.transform.SetParent(barGO.transform, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Fill area
        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(barGO.transform, false);
        var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        // Fill image
        var fillGO = new GameObject("Fill", typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;

        // Connect slider visuals
        bar.fillRect = fillRect;
        bar.targetGraphic = fillImg;
        return bar;
    }

    private TextMeshProUGUI CreateProgressText(Transform parent)
    {
        var textGO = new GameObject("ProgressText", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28f;
        tmp.text = string.Empty;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 20f);
        textRect.sizeDelta = new Vector2(800f, 60f);
        return tmp;
    }
}
