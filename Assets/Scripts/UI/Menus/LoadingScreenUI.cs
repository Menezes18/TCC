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
    
    // Nome da cena offline para auto-hide
    private const string OFFLINE_SCENE_NAME = "MainMenu";
    private static readonly string[] OFFLINE_SCENE_NAMES = { "offline" };

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
            
            // Subscribe to scene changes to auto-hide on offline scene
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// Auto-esconde a tela de loading quando uma cena offline é carregada.
    /// Isso garante que o cliente não fique preso na tela de loading se desconectar.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Verifica se é uma cena offline
        bool isOfflineScene = IsOfflineScene(scene.name);
        
        // Se não está conectado a nenhum servidor e está numa cena offline, esconde o loading
        if (!NetworkClient.active && !NetworkServer.active)
        {
            if (panel != null && panel.activeSelf)
            {
                Debug.Log($"[LoadingUI] Auto-hiding on scene '{scene.name}' - not connected to network");
                Hide();
            }
        }
        else if (isOfflineScene && panel != null && panel.activeSelf)
        {
            Debug.Log($"[LoadingUI] Auto-hiding on offline scene '{scene.name}'");
            Hide();
        }
    }
    
    private bool IsOfflineScene(string sceneName)
    {
        foreach (var offlineName in OFFLINE_SCENE_NAMES)
        {
            if (sceneName.Contains(offlineName))
                return true;
        }
        
        // Também verifica se é a offlineScene configurada no NetworkManager
        if (NetworkManager.singleton != null && !string.IsNullOrEmpty(NetworkManager.singleton.offlineScene))
        {
            if (sceneName.Contains(NetworkManager.singleton.offlineScene) || 
                NetworkManager.singleton.offlineScene.Contains(sceneName))
                return true;
        }
        
        return false;
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
        
        // Reset player progress display
        _loadedPlayers = 0;
        _totalPlayers = 0;
        _playerStatusMessage = "";
        
        Debug.Log("[LoadingUI] Hide");
    }

    // Player progress tracking for synchronized loading
    private int _loadedPlayers = 0;
    private int _totalPlayers = 0;
    private string _playerStatusMessage = "";
    [SerializeField] private TextMeshProUGUI playerProgressText;

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
            // Show combined progress with player status if available
            if (_totalPlayers > 0)
            {
                progressText.text = $"{(int)(normalizedProgress * 100)}%";
            }
            else
            {
                progressText.text = $"{(int)(normalizedProgress * 100)}%";
            }
        }
    }
    
    /// <summary>
    /// Atualiza o progresso de jogadores carregados.
    /// Chamado pelo SceneTransitionManager para feedback visual sincronizado.
    /// </summary>
    /// <param name="loadedPlayers">Número de jogadores que já carregaram</param>
    /// <param name="totalPlayers">Número total de jogadores</param>
    /// <param name="statusMessage">Mensagem de status opcional</param>
    public void SetPlayerProgress(int loadedPlayers, int totalPlayers, string statusMessage = null)
    {
        EnsureRuntimeUI();
        
        _loadedPlayers = loadedPlayers;
        _totalPlayers = totalPlayers;
        _playerStatusMessage = statusMessage ?? $"Aguardando jogadores... ({loadedPlayers}/{totalPlayers} prontos)";
        
        // Update UI
        if (playerProgressText != null)
        {
            playerProgressText.text = _playerStatusMessage;
        }
        else if (progressText != null)
        {
            // Fallback: use progress text if player progress text not set
            float currentProgress = progressBar != null ? progressBar.value : 0f;
            if (currentProgress >= 0.9f)
            {
                // Scene loaded, show player waiting status
                progressText.text = _playerStatusMessage;
            }
        }
        
        Debug.Log($"[LoadingUI] Player progress: {loadedPlayers}/{totalPlayers} - {_playerStatusMessage}");
    }
    
    /// <summary>
    /// Retorna o status atual de carregamento de jogadores
    /// </summary>
    public (int loaded, int total, string status) GetPlayerProgress()
    {
        return (_loadedPlayers, _totalPlayers, _playerStatusMessage);
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
        img.color = new Color(0f, 0f, 0f, 1f);
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
        slider.interactable = false;
        // background
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(1f,1f,1f, 1f);
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

        // Percent text
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
        
        // Player progress text (e.g., "Aguardando jogadores... (2/4 prontos)")
        TextMeshProUGUI playerTmp = null;
        try
        {
            var playerTextGO = new GameObject("PlayerProgressText");
            playerTextGO.transform.SetParent(panelGO.transform, false);
            playerTmp = playerTextGO.AddComponent<TextMeshProUGUI>();
            playerTmp.alignment = TextAlignmentOptions.Center;
            playerTmp.fontSize = 24f;
            playerTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var ptrt = playerTextGO.GetComponent<RectTransform>();
            ptrt.anchorMin = new Vector2(0.2f, 0.35f);
            ptrt.anchorMax = new Vector2(0.8f, 0.43f);
            ptrt.offsetMin = Vector2.zero; ptrt.offsetMax = Vector2.zero;
            playerTmp.text = "";
        }
        catch { }

        panel = panelGO;
        progressBar = slider;
        progressText = tmp;
        playerProgressText = playerTmp;
        panel.SetActive(false);
    }
}
