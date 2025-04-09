using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class ChegadaPodio : MonoBehaviour
{
    public List<MyClient> podio;
    public int pontosDoJogo = 4;

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            podio.Add(other.GetComponent<MyClient>());
        }
        if(podio.Count == MyNetworkManager.manager.allClients.Count){
            darPontos();
            NetworkManager.singleton.ServerChangeScene("MiniGame");
        }
    }

    [ServerCallback]
    public void darPontos(){
        for(int i = 0; i < podio.Count; i++){
            MyNetworkManager.manager.AddPoints(podio[i].playerInfo.steamId, pontosDoJogo);
            pontosDoJogo--;
        }
    }
}
