using Mirror;
using TMPro;
using UnityEngine;
using System;

public class MostrarPontosLobby : NetworkBehaviour, IObserverPontos
{
    public TMP_Text[] jogadores;
    public TMP_Text[] jogadoresPontos;

    void Awake()
    {
        if (MyNetworkManager.manager != null)
            MyNetworkManager.manager.Adicionar(this);
    }

    void OnDestroy()
    {
        if (MyNetworkManager.manager != null)
            MyNetworkManager.manager.Retira(this);
    }

    [ClientRpc]
    public void RpcAtualizarPontos(int[] pontos, string[] jogadoresNomes)
    {
        AtualizarUI(pontos, jogadoresNomes);
    }

    private void AtualizarUI(int[] pontos, string[] jogadoresNomes)
    {
        for (int i = 0; i < jogadores.Length; i++)
        {
            if (jogadores[i] != null) jogadores[i].text = "";
            if (jogadoresPontos[i] != null) jogadoresPontos[i].text = "";
        }

        var lista = new Tuple<string, int>[jogadoresNomes.Length];
        for (int i = 0; i < jogadoresNomes.Length && i < pontos.Length; i++)
            lista[i] = Tuple.Create(jogadoresNomes[i], pontos[i]);

        Array.Sort(lista, (a, b) => b.Item2.CompareTo(a.Item2));

        for (int i = 0; i < lista.Length && i < jogadores.Length; i++)
        {
            if (jogadores[i] != null) jogadores[i].text = lista[i].Item1;
            if (jogadoresPontos[i] != null) jogadoresPontos[i].text = lista[i].Item2.ToString();
        }
    }

    public void Atualizacao(ISubjectPontos subject, int[] pontos, string[] jogadores)
    {
        RpcAtualizarPontos(pontos, jogadores);
    }
    public override void OnStopClient()
    {
        if (MyNetworkManager.manager != null)
            MyNetworkManager.manager.Retira(this);
        base.OnStopClient();
    }
}