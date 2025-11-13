using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class ChegadaPodio : MonoBehaviour, IObserver
{
    public List<PlayerData> podio = new List<PlayerData>();
    [SerializeField] private SettingsMiniGameData settingsData;
    public int pontosBase = 4; // legado; mantenho para compatibilidade mas não usamos mais
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
            // scene progression centralized elsewhere; server can decide when to change scene
            // NetworkManager.singleton.ServerChangeScene("Vitoria");
        }
    }

    [ServerCallback]
    private void DistribuirPontos()
    {
        // Monta ranking conforme tipo:
        // - Corrida (isKillGame == false): ordem de chegada = podio
        // - Sobrevivência (isKillGame == true): todos eliminados em 'podio' (ordem de morte) recebem do último ao primeiro
        var ranked = new List<PlayerData>();

        if (!isKillGame)
        {
            ranked.AddRange(podio);
        }
        else
        {
            // tempo não acabou: todos morreram -> survivors = 0; usa ordem inversa de eliminação
            for (int i = podio.Count - 1; i >= 0; i--)
                ranked.Add(podio[i]);
        }

        for (int i = 0; i < ranked.Count; i++)
        {
            int pts = PointsForPlacement(i);
            if (pts > 0)
                MyNetworkManager.manager.AddPoints(ranked[i].playerInfo.steamId, pts);
        }
    }
    
    [Server]
    public void Atualizacao(ISubject subject)
    {
        if (isKillGame && subject is ContadorTempo)
        {
            Debug.Log("Tempo esgotado! Distribuindo pontos por ranking de sobrevivência...");

            // Ranking: sobreviventes primeiro (empatados em ordem de iteração), depois eliminados do último para o primeiro
            var ranked = new List<PlayerData>();
            var all = MyNetworkManager.manager.allClients;

            foreach (var pd in all)
                if (!podio.Contains(pd)) ranked.Add(pd);

            for (int i = podio.Count - 1; i >= 0; i--)
                ranked.Add(podio[i]);

            for (int i = 0; i < ranked.Count; i++)
            {
                int pts = PointsForPlacement(i);
                if (pts > 0)
                    MyNetworkManager.manager.AddPoints(ranked[i].playerInfo.steamId, pts);
            }

            // Final scene flow should be orchestrated centrally (e.g., MatchManager)
            MyNetworkManager.manager.ServerChangeSceneSynchronized("Vitoria");
        }
    }

    private int PointsForPlacement(int placementIndex)
    {
        // 0=1º, 1=2º, 2=3º, 3=4º
        if (settingsData == null) return Mathf.Max(0, pontosBase - placementIndex);
        return placementIndex switch
        {
            0 => settingsData.firstPlaceBonus,
            1 => settingsData.secondPlaceBonus,
            2 => settingsData.thirdPlaceBonus,
            3 => settingsData.fourthPlaceBonus,
            _ => 0
        };
    }
}
