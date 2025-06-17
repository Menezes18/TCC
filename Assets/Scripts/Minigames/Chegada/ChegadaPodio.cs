using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class ChegadaPodio : MonoBehaviour, IObserver
{
    public List<PlayerData> podio = new List<PlayerData>();
    public int pontosBase = 4;
    public bool isKillGame = false;
    
    private void Awake()
    {
        ContadorTempo contador = FindFirstObjectByType<ContadorTempo>();
        if (contador != null)
        {
            contador.Adicionar(this);
        }
    }
    
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerData clienteJogador = other.GetComponent<PlayerData>();
            if (clienteJogador != null && !podio.Contains(clienteJogador))
            {
                podio.Add(clienteJogador);
                VerificarFimDeJogo();
            }
        }
    }
    
    [ServerCallback]
    private void VerificarFimDeJogo()
    {
        int jogadoresTotal = MyNetworkManager.manager.allClients.Count;
        int jogadoresEsperados = isKillGame ? jogadoresTotal : jogadoresTotal - 1;
        
        if (podio.Count >= jogadoresEsperados)
        {
            DistribuirPontos();
            //NetworkManager.singleton.ServerChangeScene("Vitoria");
        }
    }

    [ServerCallback]
    private void DistribuirPontos()
    {
        int jogadoresNoRanking = podio.Count;
        
        for (int i = 0; i < jogadoresNoRanking; i++)
        {
            int pontuacao;
            
            if (isKillGame)
            {
                pontuacao = 1 + i;
            }
            else
            {
                pontuacao = pontosBase - i;
                if (pontuacao < 1) pontuacao = 1;
            }
            
            MyNetworkManager.manager.AddPoints(podio[i].playerInfo.steamId, pontuacao);
            
        }
    }
    
    [Server]
    public void Atualizacao(ISubject subject)
    {
        if (isKillGame && subject is ContadorTempo)
        {
            Debug.Log("Tempo esgotado! Distribuindo pontos para sobreviventes...");
            
            List<PlayerData> jogadoresVivos = new List<PlayerData>();
            
            foreach (PlayerData cliente in MyNetworkManager.manager.allClients)
            {
                if (!podio.Contains(cliente))
                {
                    jogadoresVivos.Add(cliente);
                }
            }
            
            foreach (PlayerData sobrevivente in jogadoresVivos)
            {
                MyNetworkManager.manager.AddPoints(sobrevivente.playerInfo.steamId, pontosBase);
                
                Debug.Log($"SOBREVIVENTE {sobrevivente.playerInfo.steamId} recebeu {pontosBase} pontos");
            }
            
            NetworkManager.singleton.ServerChangeScene("Vitoria");
        }
    }
}