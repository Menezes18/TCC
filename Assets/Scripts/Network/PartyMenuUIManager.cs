using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PartyMenuUIManager : MonoBehaviour{
    public static PartyMenuUIManager Manager; 
    // Referência ao painel do menu que será ativado/desativado na cena.
    public GameObject[] partyMenuPanel;

    // Referência para o LobbyPlayer local que será definido no OnStartLocalPlayer.
    public MyClient localLobbyPlayer;

    private void Awake()
    {
        Manager = this;
    }

    void Start()
    {
        // if (partyMenuPanel != null)
        // {
        //     foreach (var menu in partyMenuPanel){
        //         menu.SetActive(false);
        //     }
        // }
    }

    void Update()
    {
        // Apenas o jogador dono pode abrir o menu, verifique a flag isPartyOwner.
        if (localLobbyPlayer != null && localLobbyPlayer.isPartyOwner)
        {
            // Exemplo: se pressionar "M", alterna a visibilidade do menu.
            if (Keyboard.current.mKey.wasReleasedThisFrame)
            {
                TogglePartyMenu();
                Debug.Log("input");
            }
        }
        else{
            Debug.Log("Erro localLobbyPlayer");
        }
    }

    public void SetLobbyPlayer(MyClient player)
    {
        localLobbyPlayer = player;
    }

    void TogglePartyMenu()
    {
        Debug.Log("TogglePartyMenu1");
        if (partyMenuPanel != null && partyMenuPanel.Length > 0)
        {
            Debug.Log("TogglePartyMenu2");

            // Verifica o estado do primeiro menu como referência
            bool isActive = partyMenuPanel[0].activeSelf;

            // Alterna o estado de todos os menus no vetor
            foreach (var menu in partyMenuPanel)
            {
                if (menu != null)
                {
                    menu.SetActive(!isActive);
                }
            }
        }
        else
        {
            Debug.LogWarning("partyMenuPanel está vazio ou não foi configurado.");
        }
    }

    // Função para ser vinculada ao botão "Iniciar Cena" na interface.
    public void OnStartSceneButtonClicked()
    {
        if (localLobbyPlayer != null && localLobbyPlayer.isPartyOwner)
        {
           
        }
    }

    // Você pode adicionar funções similares para "Convidar Jogadores" se sua lógica de convite estiver implementada.
}