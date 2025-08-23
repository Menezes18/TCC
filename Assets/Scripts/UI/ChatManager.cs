using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lightweight in-game chat that works without scene/prefab edits.
// - Press T to toggle the chat panel.
// - Press Enter to send; chat hides automatically after sending.
// - Uses Mirror NetworkMessages, so no NetworkIdentity spawn is required.
public class ChatManager : MonoBehaviour
{
    // Network message payload
    public struct ChatMessage : NetworkMessage
    {
        public string text;
    }

    public static ChatManager Instance { get; private set; }

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.T;
    [Tooltip("Max number of lines to keep in chat text (0 = unlimited)")]
    public int maxLines = 200;

    [Header("UI (auto-created if null)")]
    public GameObject chatRoot; // container panel
    public TMP_Text chatText;
    public TMP_InputField inputField;
    public Button sendButton;

    bool _isOpen;
    bool _handlersRegistered;

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
        SetOpen(false);
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
        // Toggle panel with T
        if (Input.GetKeyDown(toggleKey))
        {
            SetOpen(!_isOpen);
        }

        // Optional quick hide
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
        }
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
    }

    void EnsureUI()
    {
        if (chatRoot != null && chatText != null && inputField != null)
            return;

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
        chatText.enableWordWrapping = true;
        chatText.richText = true;
        chatText.fontSize = 20;
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
        inputText.enableWordWrapping = false;
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

    void SetOpen(bool open)
    {
        _isOpen = open;
        if (chatRoot != null)
            chatRoot.SetActive(open);

        if (open && inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
        else if (!open && inputField != null)
        {
            inputField.DeactivateInputField();
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

        // Send to server; server will format and broadcast
        NetworkClient.Send(new ChatMessage { text = msg });

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        // Auto-hide after sending to keep gameplay focus
        SetOpen(false);
    }

    void AppendMessage(string msg)
    {
        if (chatText == null) return;

        if (!string.IsNullOrEmpty(chatText.text))
            chatText.text += "\n";
        chatText.text += msg;

        // Trim lines if needed
        if (maxLines > 0)
        {
            var lines = chatText.text.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length > maxLines)
            {
                int start = lines.Length - maxLines;
                chatText.text = string.Join("\n", lines, start, maxLines);
            }
        }

        Canvas.ForceUpdateCanvases();
    }
}
