using Mirror;
using UnityEngine;

public class CheckMudarCena : NetworkBehaviour
{
    public int jogadoresPlataforma = 0;

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
            if (jogadoresPlataforma == GameManager.manager.jogadoresAtuais)
            {
                TrocarCena("MiniGame");
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
