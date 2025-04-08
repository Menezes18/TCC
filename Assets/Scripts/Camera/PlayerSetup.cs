using UnityEngine;
using Mirror;
using Unity.Cinemachine;

public class PlayerSetup : NetworkBehaviour
{
    // Referência à câmera principal
    public CinemachineVirtualCamera playerCamera;

    // Este método é chamado quando o objeto é instanciado na rede
    public override void OnStartLocalPlayer()
    {
        // Certifique-se de que a câmera não seja nula
        if (playerCamera == null)
        {
            Debug.LogError("Player camera is not assigned. Please assign it in the Inspector.");
            return;
        }

        // Ative a câmera apenas para o jogador local
        playerCamera.gameObject.SetActive(true);

        // Configure a câmera para seguir este jogador
        var smoothFollow = playerCamera.GetComponent<SmoothFollow>();
        if (smoothFollow != null)
        {
            smoothFollow.target = transform;
        }
    }

    // Desativa a câmera para jogadores remotos
    void Start()
    {
        if (!isLocalPlayer && playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }
}