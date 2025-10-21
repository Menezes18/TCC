using UnityEngine;
using Mirror;
using System.Collections.Generic;
public class ChaoQuebrandoSimples : MonoBehaviour
{
    [SerializeField] private GameObject[] estadosChao;
    [SerializeField] private ChaoMaeSo dataChao;
    [SerializeField] private bool mostrarLogs = false;
    [SerializeField] private float raioDeteccao = 1.5f;
    
    private Vector3 posInicial;
    private int tileId = -1;
    private FloorBreakingManager manager;
    
    private float tempoAcumulado = 0f;
    private int indiceEstadoAtual = 0;
    private bool chaoDestruido = false;
    private bool foiPisado = false;

    private void Awake()
    {
        posInicial = transform.position;
        
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
    
    public bool FoiPisado() => foiPisado;
    
    public void AtivarTile()
    {
        if (!foiPisado)
        {
            foiPisado = true;
            tempoAcumulado = 0f;
            
            if (mostrarLogs)
            {
                Debug.Log($"[Tile {tileId}] Ativado remotamente");
            }
        }
    }

    private void Update()
    {
        // SERVIDOR: processa lógica do tile
        if (NetworkServer.active)
        {
            if (!chaoDestruido)
            {
                DetectarJogadores();
            }

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
        // CLIENTES: detecta localmente e avisa o servidor
        else if (NetworkClient.active)
        {
            if (!chaoDestruido && !foiPisado)
            {
                DetectarJogadorLocal();
            }
        }
    }
    
    // Servidor detecta todos os jogadores
    private void DetectarJogadores()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, raioDeteccao);
        
        foreach (Collider col in colliders)
        {
            Transform root = col.transform.root;
            if (root.CompareTag("Player") || col.CompareTag("Player"))
            {
                if (!foiPisado)
                {
                    foiPisado = true;
                    tempoAcumulado = 0f;
                    
                    if (mostrarLogs)
                    {
                        Debug.Log($"[Tile {tileId}] (SERVIDOR) Jogador detectado: {root.name}");
                    }
                }
                return;
            }
        }
    }
    
    private void DetectarJogadorLocal()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, raioDeteccao);
        
        foreach (Collider col in colliders)
        {
            Transform root = col.transform.root;
            
            NetworkIdentity netIdentity = root.GetComponent<NetworkIdentity>();
            if (netIdentity != null && netIdentity.isOwned)
            {
                if (root.CompareTag("Player") || col.CompareTag("Player"))
                {
                    if (manager != null)
                    {
                        manager.NotificarTilePisadoPorCliente(tileId);
                        
                        if (mostrarLogs)
                        {
                            Debug.Log($"[Tile {tileId}] (CLIENTE) Meu jogador pisou, notificando servidor");
                        }
                        
                        foiPisado = true;
                    }
                    return;
                }
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
            
            if (mostrarLogs)
            {
                Debug.Log($"[Tile {tileId}] (CLIENTE) Tile destruído remotamente");
            }
        }
        else
        {
            if (!foiPisado && novoIndice > 0)
            {
                foiPisado = true;
                
                if (mostrarLogs)
                {
                    Debug.Log($"[Tile {tileId}] (CLIENTE) Tile ativado remotamente por outro jogador");
                }
            }
            
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
        
        gameObject.SetActive(true);
        transform.position = posInicial;
        AtualizarVisualizacao(0);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = foiPisado ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, raioDeteccao);
    }
}
