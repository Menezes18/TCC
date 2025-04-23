using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum MenuState { Home, InParty }

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    public MenuState state = MenuState.Home;
    [SerializeField] private GameObject homeUI, partyUI;

    [Header("Ready Button")]
    [SerializeField] private Image readyButton_Image;
    [SerializeField] private TMP_Text readyButton_Text;
    public Color readyColor, notReadyColor;

    [Header("Celular UI")]
    [SerializeField] private RectTransform celularUI;
    [SerializeField] private CanvasGroup celularCanvasGroup;

    [Header("DEBUG ANIMAÇÃO")]
    public bool toggleCelular = false;
    private bool previousToggle = false;

    private bool celularAberto = false;
    private bool animando = false;

    private void Awake()
    {
        instance = this;
        celularCanvasGroup.alpha = 0;
        celularAberto = true;
        ToggleCelular();
    }

    private void Start()
    {
        Invoke("ToggleCelular", 0.5f);
    }

    private void Update()
    {
        if (toggleCelular != previousToggle)
        {
            previousToggle = toggleCelular;

            ToggleCelular();
        }
    }

    public void SetMenuState(MenuState state)
    {
        this.state = state;
        homeUI.SetActive(state == MenuState.Home);
        partyUI.SetActive(state == MenuState.InParty);
        if(state == MenuState.InParty) ToggleCelular();
    }

    public void CreateParty()
    {
        PopupManager.instance.Popup_Show("Creating Party", true);
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        SteamLobby.instance.CreateLobby();
        
    }

    public void StartSinglePlayer()
    {
        LobbyController.instance.StartGameSolo();
    }

    public void LeaveParty()
    {
        if (!NetworkClient.active) return;

        if (NetworkClient.localPlayer.isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();

        SteamLobby.instance.Leave();
    }

    public void FindMatch()
    {
        SteamLobby.instance.FindMatch();
    }

    public void StartGame()
    {
        LobbyController.instance.StartGameWithParty();
    }

    public void StartLocalClient()
    {
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        NetworkManager.singleton.StartClient();
    }

    public void StartLocalHost()
    {
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        NetworkManager.singleton.StartHost();
    }

    public void ToggleReady()
    {
        if (!NetworkClient.active) return;
        NetworkClient.localPlayer.GetComponent<MyClient>().ToggleReady();
    }

    public void UpdateReadyButton(bool value)
    {
        readyButton_Text.text = value ? "Ready" : "Not Ready";
        readyButton_Image.color = value ? readyColor : notReadyColor;
    }
    public void ShowCelularUI()
    {
        if (celularAberto || animando) return;

        animando = true;

        celularCanvasGroup.alpha = 1;

        celularUI.anchoredPosition = new Vector2(0, -Screen.height);
        celularUI.localScale = new Vector3(0.85f, 0.85f, 1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(celularUI.DOAnchorPosY(0f, 0.6f).SetEase(Ease.OutBack));
        seq.Join(celularUI.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            celularAberto = true;
            animando = false;
        });
    }


    public void HideCelularUI()
    {
        if (!celularAberto || animando) return;

        animando = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(celularUI.DOAnchorPosY(-Screen.height, 0.4f).SetEase(Ease.InBack));
        seq.Join(celularUI.DOScale(new Vector3(0.8f, 0.8f, 1f), 0.3f).SetEase(Ease.InSine));
        seq.OnComplete(() =>
        {
            celularCanvasGroup.alpha = 0;
            celularAberto = false;
            animando = false;
        });
    }

    public void ToggleCelular()
    {
        if (celularAberto)
            HideCelularUI();
        else
            ShowCelularUI();
    }

    public void OnCelularButtonPressed()
    {
        ToggleCelular();
    }
}
