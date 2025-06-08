using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System.Collections;
using UnityEngine.UI;

public class PlayerInputReadyHandler : NetworkBehaviour
{
    public GameObject AperteP_txt;
    [SerializeField] private float _timer = 5f;
    void Start()
    {
        StartCoroutine(TemporizadorP());
    }
    IEnumerator TemporizadorP()
    {
        yield return new WaitForSeconds(_timer);
        AperteP_txt.SetActive(false);
    }
    void Update()
    {
        //if (!isLocalPlayer) return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            AperteP_txt.SetActive(false);
            if (MainMenu.instance)
            {
                MainMenu.instance.UpdateReadyButton(true);
                //Tem que achar um jeito de dar o toggle do celular;
            }
            // Alterna o estado de pronto
                NetworkClient.localPlayer.GetComponent<PlayerData>().ToggleReady();

            Debug.Log("Jogador pressionou P para ficar pronto.");

            // Aguarda um pequeno tempo e verifica se todos estão prontos
            Invoke(nameof(CheckIfAllPlayersAreReady), 0.5f);
        }
    }

    private void CheckIfAllPlayersAreReady()
{
    // Verificação segura
    var netManager = NetworkManager.singleton as MyNetworkManager;
    if (netManager == null)
    {
        Debug.LogError("NetworkManager.singleton não está inicializado ou não é do tipo MyNetworkManager.");
        return;
    }

    if (netManager.allClients == null)
    {
        Debug.LogError("A lista allClients não está inicializada.");
        return;
    }

    bool allReady = true;
    foreach (var player in netManager.allClients)
    {
        if (!player.IsReady)
        {
            allReady = false;
            break;
        }
    }

    if (allReady)
    {
        Debug.Log("Todos os jogadores estão prontos! Iniciando o jogo...");
        LobbyController.singleton.StartGameWithParty();
    }
    else
    {
        Debug.Log("Ainda há jogadores não prontos.");
    }
}

    // Verifica se todos os jogadores estão prontos
    private bool AllPlayersReady()
    {
        foreach (PlayerData client in ((MyNetworkManager)NetworkManager.singleton).allClients)
        {
            if (!client.IsReady)
                return false;
        }
        return true;
    }
}
