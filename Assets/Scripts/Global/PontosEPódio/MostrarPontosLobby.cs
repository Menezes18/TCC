using TMPro;
using UnityEngine;
using System;

public class MostrarPontosLobby : MonoBehaviour, IObserverPontos
{
    public TMP_Text[] jogadores;
    public TMP_Text[] jogadoresPontos;
    
    void Awake()
    {
        MyNetworkManager.manager.Adicionar(this);
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
        
        var jogadoresPontuacao = new Tuple<string, int>[jogadoresNomes.Length];
        for (int i = 0; i < jogadoresNomes.Length && i < pontos.Length; i++)
        {
            jogadoresPontuacao[i] = new Tuple<string, int>(jogadoresNomes[i], pontos[i]);
        }
        
        Array.Sort(jogadoresPontuacao, (a, b) => b.Item2.CompareTo(a.Item2));
        
        for (int i = 0; i < jogadoresPontuacao.Length && i < jogadores.Length; i++)
        {
            if (jogadores[i] != null)
                jogadores[i].text = jogadoresPontuacao[i].Item1;
            
            if (jogadoresPontos[i] != null)
                jogadoresPontos[i].text = jogadoresPontuacao[i].Item2.ToString();
        }
    }
}