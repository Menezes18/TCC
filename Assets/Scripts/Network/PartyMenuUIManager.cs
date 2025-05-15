using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PartyMenuUIManager : MonoBehaviour{
    public static PartyMenuUIManager Manager; 
    public GameObject[] partyMenuPanel;

    public PlayerData localLobbyPlayer;

    private void Awake()
    {
        Manager = this;
    }
    

    void Update()
    {
        if (localLobbyPlayer != null && localLobbyPlayer.isPartyOwner)
        {
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

    public void SetLobbyPlayer(PlayerData player)
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

}