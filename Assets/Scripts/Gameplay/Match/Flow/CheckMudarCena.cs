using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkIdentity))]
public class CheckMudarCena : NetworkBehaviour
{
    [SerializeField] private string mudarCena = string.Empty;
    [SerializeField, Min(0)] private int jogadoresPlataforma = 0;
    [Server]
    void TrocarCena(string cena)
    {
        NetworkManager.singleton.ServerChangeScene(cena);
    }

    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            jogadoresPlataforma++;
            Debug.Log("Jogadores na plataforma: " + jogadoresPlataforma);
            if (jogadoresPlataforma == MyNetworkManager.manager.allClients.Count)
            {
                TrocarCena(mudarCena);
            }
        }
    }

    [ServerCallback]
    public void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            jogadoresPlataforma--;
            Debug.Log("Jogadores na plataforma: " + jogadoresPlataforma);

        }
    }
}
