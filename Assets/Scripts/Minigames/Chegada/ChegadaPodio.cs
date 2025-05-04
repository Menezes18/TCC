using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class ChegadaPodio : MonoBehaviour, IObserver
{
    public List<MyClient> podio = new List<MyClient>();
    public int pontosBase = 4;
    public bool isKillGame = false;
    
    private void Awake()
    {
        ContadorTempo contador = FindObjectOfType<ContadorTempo>();
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
            MyClient clienteJogador = other.GetComponent<MyClient>();
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
            NetworkManager.singleton.ServerChangeScene("Vitoria");
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
            
            Debug.Log($"Jogador {podio[i].playerInfo.steamId} na posição {i+1} recebeu {pontuacao} pontos");
        }
    }
    
    [Server]
    public void Atualizacao(ISubject subject)
    {
        if (isKillGame && subject is ContadorTempo)
        {
            Debug.Log("Tempo esgotado! Distribuindo pontos para sobreviventes...");
            
            List<MyClient> jogadoresVivos = new List<MyClient>();
            
            foreach (MyClient cliente in MyNetworkManager.manager.allClients)
            {
                if (!podio.Contains(cliente))
                {
                    jogadoresVivos.Add(cliente);
                }
            }
            
            foreach (MyClient sobrevivente in jogadoresVivos)
            {
                MyNetworkManager.manager.AddPoints(sobrevivente.playerInfo.steamId, pontosBase);
                
                Debug.Log($"SOBREVIVENTE {sobrevivente.playerInfo.steamId} recebeu {pontosBase} pontos");
            }
            
            NetworkManager.singleton.ServerChangeScene("Vitoria");
        }
    }
}