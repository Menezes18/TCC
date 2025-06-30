using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering;

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
    
    [SerializeField] private GameObject celularGameObject;

    [Header("DEBUG ANIMAÇÃO")]
    public bool toggleCelular = false;
    private bool previousToggle = false;

    private bool celularAberto = false;
    private bool animando = false;
    public bool menuCelular = true;
    private void Awake()
    {
        instance = this;
        celularAberto = true;
        ToggleCelular();

    }

    private void Start()
    {
        // if(state == MenuState.Home)
        //     Invoke("ToggleCelular", 0.5f);
    }

    public bool startCelular = true;
    private void Update()
    {
        if (state == MenuState.InParty){
            menuCelular = false;
            Debug.LogError("AAAAAAAA");
        }
        // if (startCelular){
        //     if (state == MenuState.InParty){
        //         menuCelular = false;
        //         Debug.LogError("AAAAAAAA");
        //     }
        //     startCelular = false;
        // }
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
       // if(state == MenuState.InParty) ToggleCelular();
    }

    public void CreateParty()
    {
        PopupManager.instance.Popup_Show("Creating Party", true);
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        SteamLobby.instance.CreateLobby();
        
    }

    public void StartSinglePlayer()
    {
        LobbyController.singleton.StartGameSolo();
    }

    public void LeaveParty()
    {
        if (!NetworkClient.active) return;

        if (NetworkClient.localPlayer.isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();

        NetworkClient.Shutdown();
        NetworkManager.ResetStatics();
        SteamLobby.instance.Leave();
        Application.Quit();
    }

    public void FindMatch()
    {
        SteamLobby.instance.FindMatch();
    }

    public void StartGame()
    {
        LobbyController.singleton.StartGameWithParty();
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
        NetworkClient.localPlayer.GetComponent<PlayerData>().ToggleReady();
    }

    public void UpdateReadyButton(bool value)
    {
        readyButton_Text.text = value ? "Ready" : "Not Ready";
        readyButton_Image.color = value ? readyColor : notReadyColor;
    }
    public void ShowCelularUI()
    {
        if (celularAberto) return;

        animando = true;
        celularGameObject.SetActive(true);

        
    }


    public void HideCelularUI()
    {
        if (!celularAberto) return;

        animando = true;
        celularGameObject.SetActive(false);
        celularAberto = false;
        animando = false;
        
    }

    public void ToggleCelular()
    {
        if(menuCelular) return;
        celularAberto = !celularAberto;
        celularGameObject.SetActive(!celularGameObject.activeSelf);
        // if (celularAberto)
        //     HideCelularUI();
        // else
        //     ShowCelularUI();
    }

    public void OnCelularButtonPressed()
    {
        ToggleCelular();
    }
}
