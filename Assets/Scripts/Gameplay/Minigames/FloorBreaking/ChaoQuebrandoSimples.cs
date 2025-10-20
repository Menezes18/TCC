using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ChaoQuebrandoSimples : MonoBehaviour
{
    [SerializeField] private GameObject[] estadosChao;
    [SerializeField] private ChaoMaeSo dataChao;
    [SerializeField] private bool detectarPulandoTambem = true;
    [SerializeField] private bool mostrarLogs = false;
    
    private Vector3 posInicial;
    private int tileId = -1;
    private FloorBreakingManager manager;
    
    private float tempoAcumulado = 0f;
    private HashSet<Collider> jogadoresNoTile = new HashSet<Collider>();
    private int indiceEstadoAtual = 0;
    private bool chaoDestruido = false;
    private bool foiPisado = false; // Flag para garantir que pisa pelo menos uma vez

    private void Awake()
    {
        posInicial = transform.position;
        
        // Garante que apenas o estado 0 está ativo no início
        if (estadosChao != null && estadosChao.Length > 0)
        {
            for (int i = 0; i < estadosChao.Length; i++)
            {
                if (estadosChao[i] != null)
                {
                    estadosChao[i].SetActive(i == 0);
                }
            }
        }
    }

    private void Start()
    {
        // Confirma estado inicial no servidor
        if (NetworkServer.active || !NetworkClient.active)
        {
            AtualizarVisualizacao(0);
        }
    }

    public void SetTileId(int id) => tileId = id;
    public void SetManager(FloorBreakingManager mgr) => manager = mgr;

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active && NetworkClient.active) return;
        
        if (other.CompareTag("Player") && !chaoDestruido)
        {
            jogadoresNoTile.Add(other);
            
            if (!foiPisado)
            {
                foiPisado = true;
                tempoAcumulado = 0f;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!NetworkServer.active && NetworkClient.active) return;
        
        if (other.CompareTag("Player") && !chaoDestruido)
        {
            jogadoresNoTile.Add(other);
            
            if (!foiPisado)
            {
                foiPisado = true;
                tempoAcumulado = 0f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active && NetworkClient.active) return;
        
        if (other.CompareTag("Player"))
        {
            jogadoresNoTile.Remove(other);
        }
    }

    private void Update()
    {
        if (!NetworkServer.active && NetworkClient.active) return;

        if (foiPisado && !chaoDestruido)
        {
            tempoAcumulado += Time.deltaTime;
            
            if (tempoAcumulado >= dataChao.tempo)
            {
                tempoAcumulado = 0;
                ProgrediEstado();
            }
        }
    }

    private void ProgrediEstado()
    {
        indiceEstadoAtual++;

        if (mostrarLogs)
        {
            Debug.Log($"[Tile {tileId}] Progredindo para estado {indiceEstadoAtual}/{estadosChao.Length}");
        }

        if (indiceEstadoAtual < estadosChao.Length)
        {
            AtualizarVisualizacao(indiceEstadoAtual);
            
            if (manager != null)
            {
                manager.NotificarProgressaoTile(tileId, indiceEstadoAtual);
            }
            else if (mostrarLogs)
            {
                Debug.LogWarning($"[Tile {tileId}] Manager é null!");
            }
        }
        else
        {
            if (mostrarLogs)
            {
                Debug.Log($"[Tile {tileId}] Destruindo tile (índice {indiceEstadoAtual} >= {estadosChao.Length})");
            }
            DestruirTile();
        }
    }

    private void DestruirTile()
    {
        chaoDestruido = true;
        gameObject.SetActive(false);
        
        if (manager != null)
        {
            manager.NotificarDestruicaoTile(tileId);
        }
    }

    public void AtualizarVisualizacaoRemota(int novoIndice, bool destruido)
    {
        if (destruido)
        {
            chaoDestruido = true;
            gameObject.SetActive(false);
        }
        else
        {
            indiceEstadoAtual = novoIndice;
            AtualizarVisualizacao(novoIndice);
        }
    }

    private void AtualizarVisualizacao(int indice)
    {
        if (estadosChao == null || estadosChao.Length == 0)
        {
            Debug.LogError($"[Tile {tileId}] Array de estados está vazio!");
            return;
        }

        if (mostrarLogs)
        {
            Debug.Log($"[Tile {tileId}] Atualizando visualização para índice {indice}");
        }

        for (int i = 0; i < estadosChao.Length; i++)
        {
            if (estadosChao[i] != null)
            {
                estadosChao[i].SetActive(false);
            }
        }

        if (indice >= 0 && indice < estadosChao.Length && estadosChao[indice] != null)
        {
            estadosChao[indice].SetActive(true);
            
            if (mostrarLogs)
            {
                Debug.Log($"[Tile {tileId}] Estado {indice} ({estadosChao[indice].name}) ativado");
            }
        }
        else if (mostrarLogs)
        {
            Debug.LogWarning($"[Tile {tileId}] Índice {indice} fora do range ou objeto null");
        }
    }

    public void ResetarTile()
    {
        chaoDestruido = false;
        indiceEstadoAtual = 0;
        tempoAcumulado = 0f;
        foiPisado = false;
        jogadoresNoTile.Clear();
        
        gameObject.SetActive(true);
        transform.position = posInicial;
        AtualizarVisualizacao(0);
    }
}
