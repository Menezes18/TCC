using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ChaoQuebrando : ChaoMae
{
    [SerializeField]
    private GameObject[] estadosChao;
    
    [SyncVar(hook = nameof(OnIndiceChanged))]
    private int indiceEstadoAtual = 0;
    
    private float tempoAcumulado = 0f;
    private readonly HashSet<Collider> collidersDeJogadoresNoTile = new HashSet<Collider>();
    private int ultimoIndiceVisual = 0;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity != null) collidersDeJogadoresNoTile.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            collidersDeJogadoresNoTile.Remove(other);
        }
    }

    private void Update()
    {
        // Apenas o servidor processa a lógica de progressão
        collidersDeJogadoresNoTile.RemoveWhere(playerCollider => playerCollider == null);
        if (isServer && collidersDeJogadoresNoTile.Count > 0 && !chaoTirado)
        {
            tempoAcumulado += Time.deltaTime;
            if (tempoAcumulado >= dataChao.tempo)
            {
                tempoAcumulado = 0;
                AtualizaEstado();
            }
        }
    }

    [Server]
    private void AtualizaEstado()
    {
        indiceEstadoAtual++;
        AtualizaVisualizacao(indiceEstadoAtual);

        if (indiceEstadoAtual >= estadosChao.Length)
        {
            tiraChao();
        }
    }

    // Hook chamado automaticamente quando o SyncVar muda (em todos os clientes)
    private void OnIndiceChanged(int oldIndice, int newIndice)
    {
        // Atualiza a visualização local baseado no novo índice
        AtualizaVisualizacao(newIndice);
    }

    private void AtualizaVisualizacao(int novoIndice)
    {
        if (novoIndice == 0) gameObject.SetActive(true);
        // Desativa o estado anterior
        if (ultimoIndiceVisual >= 0 && ultimoIndiceVisual < estadosChao.Length)
        {
            estadosChao[ultimoIndiceVisual].SetActive(false);
        }

        // Ativa o novo estado se válido
        if (novoIndice >= 0 && novoIndice < estadosChao.Length)
        {
            estadosChao[novoIndice].SetActive(true);
            ultimoIndiceVisual = novoIndice;
        }
        else if (novoIndice >= estadosChao.Length)
        {
            // Tile foi destruído
            DesativaTile();
        }
    }

    [Server]
    public override void tiraChao()
    {
        chaoTirado = true;
        indiceEstadoAtual = estadosChao.Length;
        DesativaTile();
    }

    private void DesativaTile()
    {
        gameObject.SetActive(false);
    }

    [Server]
    public override void poeChao()
    {
        chaoTirado = false;
        indiceEstadoAtual = 0;
        tempoAcumulado = 0f;
        
        gameObject.SetActive(true);
        transform.position = posIncial;
        
        collidersDeJogadoresNoTile.Clear();
        AtualizaVisualizacao(0);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Sincroniza a visualização inicial quando o cliente conecta
        AtualizaVisualizacao(indiceEstadoAtual);
    }
}
