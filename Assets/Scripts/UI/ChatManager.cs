using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ChatManager : MonoBehaviour
{
    // Network message payload
    public struct ChatMessage : NetworkMessage
    {
        public string text;
    }

    public static ChatManager Instance { get; private set; }

    [Header("Settings")]
    public Key toggleKey = Key.T;
    [Tooltip("Max number of lines to keep in chat text (0 = unlimited)")]
    public int maxLines = 200;

    [Header("UI (auto-created if null)")]
    public GameObject chatRoot; // container panel
    public TMP_Text chatText;
    public TMP_InputField inputField;
    public Button sendButton;

    // Popup/Toast settings
    [Header("Popups")]
    [Tooltip("Container for popup toasts; auto-created if null")] public RectTransform toastRoot;
    [Tooltip("How long a popup stays fully visible before fading")] public float toastLifetime = 4f;
    [Tooltip("Fade duration at the end of lifetime")] public float toastFadeDuration = 0.75f;
    [Tooltip("Max active popups visible; 0 = unlimited")] public int toastMaxActive = 5;

    // Link para o Player local para travar/destravar movimento/visão
    private PlayerScript _localPlayer;

    bool _isOpen;
    bool _handlersRegistered;
    readonly Queue<string> _lines = new Queue<string>();
    readonly StringBuilder _sb = new StringBuilder(2048);

    // Toast state
    class Toast
    {
        public GameObject go;
        public CanvasGroup cg;
        public float bornTime;
        public float life;
        public TextMeshProUGUI text;
    }
    readonly List<Toast> _toasts = new List<Toast>(8);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("ChatManager");
            go.AddComponent<ChatManager>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUI();
        EnsureToastRoot();
        SetOpen(false);
        TryFindLocalPlayer();
    }

    void OnEnable()
    {
        RegisterHandlers();
    }

    void OnDisable()
    {
        UnregisterHandlers();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (_localPlayer == null)
            TryFindLocalPlayer();

        // Toggle com T sempre (mesmo se o input estiver focado)
        if (kb[toggleKey].wasPressedThisFrame && !_isOpen)
        {
            SetOpen(true);
            return;
        }

        // Fechar com ESC
        if (_isOpen && kb.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
            return;
        }

        UpdateToasts();
    }

    void RegisterHandlers()
    {
        if (_handlersRegistered)
            return;

        // Client receives messages
        NetworkClient.RegisterHandler<ChatMessage>(OnClientReceive);

        // Server relays messages to everyone
        NetworkServer.RegisterHandler<ChatMessage>(OnServerReceive, false);

        _handlersRegistered = true;
    }

    void UnregisterHandlers()
    {
        if (!_handlersRegistered)
            return;

        // Mirror doesn't have an explicit Unregister; re-registering on enable is safe.
        _handlersRegistered = false;
    }

    // Called on server when any client sends a message
    void OnServerReceive(NetworkConnectionToClient conn, ChatMessage msg)
    {
        string playerName = "Player";
        try
        {
            // Try to get a friendly name from PlayerData if available
            var identity = conn?.identity;
            if (identity != null)
            {
                var pd = identity.GetComponent<PlayerData>();
                if (pd != null && !string.IsNullOrWhiteSpace(pd.alias))
                    playerName = pd.alias;
            }
        }
        catch { /* best effort only */ }

        string raw = (msg.text ?? string.Empty).Replace("\r", string.Empty);
        if (string.IsNullOrWhiteSpace(raw)) return;

        string formatted = $"[{playerName}]: {raw}";
        NetworkServer.SendToAll(new ChatMessage { text = formatted });
    }

    // Called on clients when server broadcasts a chat message
    void OnClientReceive(ChatMessage msg)
    {
        AppendMessage(msg.text);
        ShowToast(msg.text);
    }

    void EnsureUI()
    {
        if (chatRoot != null && chatText != null && inputField != null)
        {
            // Garantir alinhamento para mostrar as mensagens mais recentes
            chatText.alignment = TextAlignmentOptions.BottomLeft;
            return;
        }

        // Create a minimal canvas + panel UI hierarchy at runtime
        var canvasGO = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        chatRoot = new GameObject("ChatPanel", typeof(RectTransform), typeof(Image));
        chatRoot.transform.SetParent(canvasGO.transform, false);
        var rootRT = (RectTransform)chatRoot.transform;
        rootRT.anchorMin = new Vector2(0, 0);
        rootRT.anchorMax = new Vector2(0, 0);
        rootRT.pivot = new Vector2(0, 0);
        rootRT.sizeDelta = new Vector2(540, 260);
        rootRT.anchoredPosition = new Vector2(20, 20);
        var bg = chatRoot.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.55f);

        // Scroll area
        var scrollGO = new GameObject("ScrollArea", typeof(RectTransform), typeof(Mask), typeof(Image));
        scrollGO.transform.SetParent(chatRoot.transform, false);
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.sizeDelta = new Vector2(-20, -70);
        scrollRT.anchoredPosition = new Vector2(10, 35);
        var scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.25f);
        scrollGO.GetComponent<Mask>().showMaskGraphic = false;

        // Text content
        var textGO = new GameObject("ChatText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(scrollGO.transform, false);
        var textRT = (RectTransform)textGO.transform;
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0, 1);
        textRT.offsetMin = new Vector2(8, 8);
        textRT.offsetMax = new Vector2(-8, -8);
        chatText = textGO.GetComponent<TextMeshProUGUI>();
        chatText.textWrappingMode = TextWrappingModes.Normal;
        chatText.richText = true;
        chatText.fontSize = 20;
        chatText.alignment = TextAlignmentOptions.BottomLeft; // recentes sempre visíveis
        chatText.text = string.Empty;

        // Input row
        var inputGO = new GameObject("InputField", typeof(RectTransform));
        inputGO.transform.SetParent(chatRoot.transform, false);
        var inputRT = (RectTransform)inputGO.transform;
        inputRT.anchorMin = new Vector2(0, 0);
        inputRT.anchorMax = new Vector2(1, 0);
        inputRT.pivot = new Vector2(0.5f, 0);
        inputRT.sizeDelta = new Vector2(-120, 40);
        inputRT.anchoredPosition = new Vector2(10, 10);

        // Background for input
        var inputBgGO = new GameObject("InputBG", typeof(RectTransform), typeof(Image));
        inputBgGO.transform.SetParent(inputGO.transform, false);
        var inputBgRT = (RectTransform)inputBgGO.transform;
        inputBgRT.anchorMin = new Vector2(0, 0);
        inputBgRT.anchorMax = new Vector2(1, 1);
        inputBgRT.offsetMin = Vector2.zero;
        inputBgRT.offsetMax = Vector2.zero;
        var inputBg = inputBgGO.GetComponent<Image>();
        inputBg.color = new Color(1, 1, 1, 0.08f);

        // Actual TMP_InputField
        var inputTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextGO.transform.SetParent(inputBgGO.transform, false);
        var inputTextRT = (RectTransform)inputTextGO.transform;
        inputTextRT.anchorMin = new Vector2(0, 0);
        inputTextRT.anchorMax = new Vector2(1, 1);
        inputTextRT.offsetMin = new Vector2(10, 6);
        inputTextRT.offsetMax = new Vector2(-10, -6);
        var inputText = inputTextGO.GetComponent<TextMeshProUGUI>();
        inputText.textWrappingMode = TextWrappingModes.NoWrap;
        inputText.fontSize = 20;

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(inputBgGO.transform, false);
        var placeholderRT = (RectTransform)placeholderGO.transform;
        placeholderRT.anchorMin = new Vector2(0, 0);
        placeholderRT.anchorMax = new Vector2(1, 1);
        placeholderRT.offsetMin = new Vector2(10, 6);
        placeholderRT.offsetMax = new Vector2(-10, -6);
        var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Digite a mensagem e pressione Enter...";
        placeholder.fontSize = 20;
        placeholder.color = new Color(1, 1, 1, 0.4f);

        inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterLimit = 140;
        inputField.onSubmit.AddListener(_ => OnSendClicked());

        // Send button
        var sendBtnGO = new GameObject("SendButton", typeof(RectTransform), typeof(Image), typeof(Button));
        sendBtnGO.transform.SetParent(chatRoot.transform, false);
        var sendRT = (RectTransform)sendBtnGO.transform;
        sendRT.anchorMin = new Vector2(1, 0);
        sendRT.anchorMax = new Vector2(1, 0);
        sendRT.pivot = new Vector2(1, 0);
        sendRT.sizeDelta = new Vector2(90, 40);
        sendRT.anchoredPosition = new Vector2(-10, 10);
        sendButton = sendBtnGO.GetComponent<Button>();
        sendButton.onClick.AddListener(OnSendClicked);
        var sendImg = sendBtnGO.GetComponent<Image>();
        sendImg.color = new Color(1, 1, 1, 0.15f);

        var sendLblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        sendLblGO.transform.SetParent(sendBtnGO.transform, false);
        var sendLblRT = (RectTransform)sendLblGO.transform;
        sendLblRT.anchorMin = new Vector2(0, 0);
        sendLblRT.anchorMax = new Vector2(1, 1);
        sendLblRT.offsetMin = Vector2.zero;
        sendLblRT.offsetMax = Vector2.zero;
        var sendLbl = sendLblGO.GetComponent<TextMeshProUGUI>();
        sendLbl.alignment = TextAlignmentOptions.Center;
        sendLbl.text = "Enviar";
        sendLbl.fontSize = 20;
    }

    // Cria container de toasts (popups) ancorado no canto inferior esquerdo
    void EnsureToastRoot()
    {
        if (toastRoot != null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Se ainda não existe (primeira cena), cria um canvas básico
            var canvasGO = new GameObject("ChatToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            DontDestroyOnLoad(canvasGO);
        }

        var rootGO = new GameObject("ChatToasts", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        rootGO.transform.SetParent(canvas.transform, false);
        toastRoot = (RectTransform)rootGO.transform;
        toastRoot.anchorMin = new Vector2(0, 0);
        toastRoot.anchorMax = new Vector2(0, 0);
        toastRoot.pivot = new Vector2(0, 0);
        toastRoot.anchoredPosition = new Vector2(20, 300);
        toastRoot.sizeDelta = new Vector2(560, 0);

        var vlg = rootGO.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerLeft;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.spacing = 22f;
        var fitter = rootGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    // Cria um toast para a mensagem recebida
    void ShowToast(string msg)
    {
        if (string.IsNullOrEmpty(msg) || toastRoot == null) return;

        // Limita quantidade
        if (toastMaxActive > 0 && _toasts.Count >= toastMaxActive)
        {
            // Remove o mais antigo imediatamente
            var oldest = _toasts[0];
            if (oldest != null && oldest.go != null) Destroy(oldest.go);
            _toasts.RemoveAt(0);
        }

        var itemGO = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        itemGO.transform.SetParent(toastRoot, false);
        var rt = (RectTransform)itemGO.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(0, 0);

        var bg = itemGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        var cg = itemGO.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Texto
        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(itemGO.transform, false);
        var trt = (RectTransform)txtGO.transform;
        trt.anchorMin = new Vector2(0, 0);
        trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(10, 6);
        trt.offsetMax = new Vector2(-10, -6);
        var tmp = txtGO.GetComponent<TextMeshProUGUI>();
        tmp.text = msg;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Left;

        // Força ficar como último filho para aparecer em baixo e empurrar os antigos para cima
        itemGO.transform.SetAsLastSibling();

        _toasts.Add(new Toast
        {
            go = itemGO,
            cg = cg,
            bornTime = Time.unscaledTime,
            life = Mathf.Max(0.1f, toastLifetime),
            text = tmp
        });
    }

    void UpdateToasts()
    {
        if (_toasts.Count == 0) return;

        float now = Time.unscaledTime;
        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            var t = _toasts[i];
            float age = now - t.bornTime;
            float fadeStart = Mathf.Max(0.01f, t.life - toastFadeDuration);

            if (age >= t.life + toastFadeDuration)
            {
                if (t.go != null) Destroy(t.go);
                _toasts.RemoveAt(i);
                continue;
            }

            if (age >= fadeStart)
            {
                float k = Mathf.InverseLerp(t.life + toastFadeDuration, fadeStart, age);
                t.cg.alpha = Mathf.Clamp01(k);
            }
            else
            {
                t.cg.alpha = 1f;
            }
        }
    }

    // Localiza o Player local (dono) para sinalizar estado de chat
    void TryFindLocalPlayer()
    {
        if (NetworkClient.localPlayer != null)
        {
            _localPlayer = NetworkClient.localPlayer.GetComponent<PlayerScript>();
            if (_localPlayer != null) return;
        }

        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != null && p.isOwned) { _localPlayer = p; break; }
        }
    }

    void SetOpen(bool open)
    {
        _isOpen = open;
        if (chatRoot != null)
            chatRoot.SetActive(open);

        if (open)
        {
            _localPlayer?.OnChatOpen();
            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }
        else
        {
            _localPlayer?.OnChatClose();
            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }
        }
    }

    void OnSendClicked()
    {
        if (!NetworkClient.active)
        {
            AppendMessage("[Local]: rede indisponível para enviar mensagens.");
            return;
        }

        string msg = (inputField != null ? inputField.text : string.Empty).Trim();
        if (string.IsNullOrEmpty(msg)) return;

        NetworkClient.Send(new ChatMessage { text = msg });

        if (inputField != null)
        {
            inputField.text = string.Empty;
        }

        // Agora fecha o chat após enviar. O popup aparecerá via broadcast do servidor.
        SetOpen(false);
    }

    void AppendMessage(string msg)
    {
        if (chatText == null) return;

        if (!string.IsNullOrEmpty(msg))
            _lines.Enqueue(msg);

        if (maxLines > 0)
        {
            while (_lines.Count > maxLines)
                _lines.Dequeue();
        }

        // Sempre exibir os mais recentes. Como o alinhamento é BottomLeft, montar na ordem natural funciona.
        _sb.Clear();
        foreach (var line in _lines)
            _sb.AppendLine(line);

        chatText.text = _sb.ToString();
        Canvas.ForceUpdateCanvases();
    }

    // Public static to show toast from any script (will auto-create if needed)
    public static void ShowToastGlobal(string message)
    {
        if (Instance == null)
        {
            var go = new GameObject("ChatManager");
            Instance = go.AddComponent<ChatManager>();
            DontDestroyOnLoad(go);
        }
        if (!string.IsNullOrEmpty(message))
        {
            Instance.EnsureToastRoot();
            Instance.ShowToast(message);
        }
    }
}
