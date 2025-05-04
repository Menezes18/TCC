using TMPro;
using UnityEngine;

public class MostrarPontosLobby : MonoBehaviour, IObserverPontos
{
    public TMP_Text[] jogadores;
    public TMP_Text[] jogadoresPontos;
    void Awake()
    {
        MyNetworkManager.manager.Adicionar(this);
    }

    void Start()
    {
        
    }

    public void Atualizacao(ISubjectPontos subject, int[] pontos, string[] jogadoresNomes)
    {
        for (int i = 0; i < jogadores.Length; i++)
        {
            if (jogadores[i] != null)
                jogadores[i].text = "";
            
            if (jogadoresPontos[i] != null)
                jogadoresPontos[i].text = "";
        }
        
        for (int i = 0; i < jogadoresNomes.Length && i < jogadores.Length; i++)
        {
            if (jogadores[i] != null)
                jogadores[i].text = jogadoresNomes[i];
            
            if (jogadoresPontos[i] != null && i < pontos.Length)
                jogadoresPontos[i].text = pontos[i].ToString();
        }
    }
}