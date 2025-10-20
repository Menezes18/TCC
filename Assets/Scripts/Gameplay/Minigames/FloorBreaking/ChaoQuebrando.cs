using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoQuebrando : ChaoMae
{
    [SerializeField]
    private GameObject[] estadosChao;
    
    [SyncVar(hook = nameof(OnIndiceChanged))]
    private int indiceEstadoAtual = 0;
    
    private float tempoAcumulado = 0f;
    private bool jogadorNoTile = false;
    private int ultimoIndiceVisual = 0;
    
    // Para uso com FloorBreakingManager (opcional)
    private int tileId = -1;
    private FloorBreakingManager manager;
    
    public void SetTileId(int id) => tileId = id;
    public void SetManager(FloorBreakingManager mgr) => manager = mgr;
    public void AtualizarVisualizacaoRemota(int novoIndice, bool destruido)
    {
        if (destruido)
        {
            chaoTirado = true;
            DesativaTile();
        }
        else
        {
            indiceEstadoAtual = novoIndice;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = false;
        }
    }

    private void Update()
    {
        // Apenas o servidor processa a lógica de progressão
        if (isServer && jogadorNoTile && !chaoTirado)
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
        
        // O hook OnIndiceChanged vai atualizar a visualização automaticamente
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Sincroniza a visualização inicial quando o cliente conecta
        AtualizaVisualizacao(indiceEstadoAtual);
    }
}
