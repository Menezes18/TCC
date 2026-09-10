using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FloorBreakingManager : NetworkBehaviour
{
    [System.Serializable]
    public struct TileState
    {
        public byte indiceEstado; // byte para economizar memória (0-255 estados)
        public bool destruido;
    }

    // Estrutura para batching de atualizações
    [System.Serializable]
    public struct TileUpdate
    {
        public ushort tileId; // ushort suporta até 65535 tiles
        public byte indiceEstado;
        public bool destruido;
    }

    [SerializeField] private ChaoQuebrandoSimples[] tiles;
    [SerializeField] private float intervaloBatch = 0.05f; // Reduzido para 50ms - mais responsivo
    [SerializeField] private int tamanhoBatchMaximo = 20; // Reduzido de 50 para 20 - envia mais rápido
    [SerializeField] private bool syncInstantaneo = true; // Se true, envia destruições imediatamente
    [SerializeField] private bool mostrarLogs = false;
    
    // Armazena estado no servidor (sem sincronização automática)
    private TileState[] tileStates;
    
    // Buffer para acumular atualizações antes de enviar
    private List<TileUpdate> updateBuffer = new List<TileUpdate>();
    private float tempoUltimoBatch = 0f;
    
    private Coroutine resetCoroutine;
    private readonly Dictionary<int, double> snapshotRequestTimes = new Dictionary<int, double>();
    private const double SnapshotRequestCooldown = 5d;

    private void Awake()
    {
        tiles ??= System.Array.Empty<ChaoQuebrandoSimples>();
        if (tiles.Length > ushort.MaxValue + 1)
            throw new System.InvalidOperationException("Floor tile IDs exceed the network format.");

        // Bind before OnStartClient can request a snapshot or any tile can Update.
        tileStates = new TileState[tiles.Length];
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;
            tiles[i].SetTileId(i);
            tiles[i].SetManager(this);
        }
    }

    private void Update()
    {
        // Processar batch de atualizações periodicamente
        if (isServer && updateBuffer.Count > 0)
        {
            tempoUltimoBatch += Time.deltaTime;
            
            if (tempoUltimoBatch >= intervaloBatch)
            {
                EnviarBatchDeAtualizacoes();
                tempoUltimoBatch = 0f;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void NotificarTilePisadoPorCliente(int tileId, NetworkConnectionToClient sender = null)
    {
        if (sender != null && sender.identity != null && tileId >= 0 && tileId < tiles.Length
            && tiles[tileId] != null && tiles[tileId].IsPlayerInRange(sender.identity))
        {
            if (!tiles[tileId].FoiPisado())
            {
                tiles[tileId].AtivarTile();
                
                if (mostrarLogs)
                {
                    Debug.Log($"[FloorBreakingManager] Tile {tileId} ativado por cliente");
                }
            }
        }
    }

    [Server]
    public void NotificarProgressaoTile(int tileId, int novoIndice)
    {
        if (tileId >= 0 && tileId < tileStates.Length && tiles[tileId] != null)
        {
            if (novoIndice < 0 || novoIndice > byte.MaxValue || tileStates[tileId].destruido) return;
            tileStates[tileId].indiceEstado = (byte)novoIndice;
            
            updateBuffer.Add(new TileUpdate 
            { 
                tileId = (ushort)tileId, 
                indiceEstado = (byte)novoIndice, 
                destruido = false 
            });
            
            if (updateBuffer.Count >= tamanhoBatchMaximo)
            {
                EnviarBatchDeAtualizacoes();
            }
        }
    }

    [Server]
    public void NotificarDestruicaoTile(int tileId)
    {
        if (tileId >= 0 && tileId < tileStates.Length && tiles[tileId] != null)
        {
            tileStates[tileId].destruido = true;
            
            if (syncInstantaneo)
            {
                // Flush older progress first so it cannot resurrect a destroyed tile.
                EnviarBatchDeAtualizacoes();
                RpcAtualizarTileSingle(tileId, tileStates[tileId].indiceEstado, true);
            }
            else
            {
                updateBuffer.Add(new TileUpdate 
                { 
                    tileId = (ushort)tileId, 
                    indiceEstado = tileStates[tileId].indiceEstado, 
                    destruido = true 
                });
                
                if (updateBuffer.Count >= tamanhoBatchMaximo)
                {
                    EnviarBatchDeAtualizacoes();
                }
            }
        }
    }
    
    [ClientRpc]
    private void RpcAtualizarTileSingle(int tileId, byte indice, bool destruido)
    {
        if (isServer) return;
        
        if (tileId >= 0 && tileId < tiles.Length && tiles[tileId] != null)
        {
            tiles[tileId].AtualizarVisualizacaoRemota(indice, destruido);
            
            if (mostrarLogs)
            {
                Debug.Log($"[FloorBreakingManager] Tile {tileId} atualizado instantaneamente (destruído: {destruido})");
            }
        }
    }

    [Server]
    private void EnviarBatchDeAtualizacoes()
    {
        if (updateBuffer.Count == 0) return;
        
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Enviando batch de {updateBuffer.Count} atualizações");
        
        RpcAtualizarTilesBatch(updateBuffer.ToArray());
        updateBuffer.Clear();
        tempoUltimoBatch = 0f;
    }

    [ClientRpc]
    private void RpcAtualizarTilesBatch(TileUpdate[] updates)
    {
        if (isServer) return; 
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Cliente recebeu batch de {updates.Length} atualizações");
        
        foreach (var update in updates)
        {
            if (update.tileId < tiles.Length && tiles[update.tileId] != null)
            {
                tiles[update.tileId].AtualizarVisualizacaoRemota(update.indiceEstado, update.destruido);
            }
        }
    }

    [Server]
    public void ResetarTile(int tileId)
    {
        if (tileId >= 0 && tileId < tileStates.Length && tiles[tileId] != null)
        {
            tileStates[tileId] = new TileState { indiceEstado = 0, destruido = false };
            tiles[tileId].ResetarTile();
            
            updateBuffer.Add(new TileUpdate 
            { 
                tileId = (ushort)tileId, 
                indiceEstado = 0, 
                destruido = false 
            });
        }
    }

    [Server]
    public void ResetarTodosTiles()
    {
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Resetando todos os tiles...");
            
        if (resetCoroutine != null) StopCoroutine(resetCoroutine);
        EnviarBatchDeAtualizacoes();
        resetCoroutine = StartCoroutine(ResetarTilesGradualmente());
    }

    private System.Collections.IEnumerator ResetarTilesGradualmente()
    {
        int lote = 50;
        List<TileUpdate> resetUpdates = new List<TileUpdate>();
        
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (tiles[i] == null) continue;
            tileStates[i] = new TileState { indiceEstado = 0, destruido = false };
            tiles[i].ResetarTile();
            
            resetUpdates.Add(new TileUpdate 
            { 
                tileId = (ushort)i, 
                indiceEstado = 0, 
                destruido = false 
            });
            
            if (resetUpdates.Count >= 200)
            {
                RpcAtualizarTilesBatch(resetUpdates.ToArray());
                resetUpdates.Clear();
                yield return null;
            }
            
            if (i % lote == 0 && i > 0)
            {
                EnviarBatchDeAtualizacoes();
                if (resetUpdates.Count > 0)
                {
                    RpcAtualizarTilesBatch(resetUpdates.ToArray());
                    resetUpdates.Clear();
                }
                yield return null;
            }
        }
        
        if (resetUpdates.Count > 0)
        {
            RpcAtualizarTilesBatch(resetUpdates.ToArray());
        }
        
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Reset completo!");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!isServer)
        {
            CmdSolicitarEstadoInicial();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSolicitarEstadoInicial(NetworkConnectionToClient sender = null)
    {
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Cliente solicitou estado inicial");
            
        if (sender == null || !sender.isReady) return;
        if (snapshotRequestTimes.TryGetValue(sender.connectionId, out double lastRequest) &&
            NetworkTime.time - lastRequest < SnapshotRequestCooldown)
            return;
        snapshotRequestTimes[sender.connectionId] = NetworkTime.time;
        EnviarBatchDeAtualizacoes();
        EnviarEstadoInicialParaCliente(sender);
    }

    private void EnviarEstadoInicialParaCliente(NetworkConnectionToClient cliente)
    {
        int lote = 200;
        List<TileUpdate> updates = new List<TileUpdate>();
        
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (tileStates[i].indiceEstado != 0 || tileStates[i].destruido)
            {
                updates.Add(new TileUpdate 
                { 
                    tileId = (ushort)i, 
                    indiceEstado = tileStates[i].indiceEstado, 
                    destruido = tileStates[i].destruido 
                });
            }
            
            if (updates.Count >= lote)
            {
                TargetEnviarEstadoInicial(cliente, updates.ToArray());
                updates.Clear();
            }
        }
        
        if (updates.Count > 0)
        {
            TargetEnviarEstadoInicial(cliente, updates.ToArray());
        }
        
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Estado inicial enviado ao cliente");
    }

    [TargetRpc]
    private void TargetEnviarEstadoInicial(NetworkConnection target, TileUpdate[] updates)
    {
        if (mostrarLogs)
            Debug.Log($"[FloorBreakingManager] Recebendo estado inicial: {updates.Length} tiles alterados");
        
        foreach (var update in updates)
        {
            if (update.tileId < tiles.Length && tiles[update.tileId] != null)
            {
                tiles[update.tileId].AtualizarVisualizacaoRemota(update.indiceEstado, update.destruido);
            }
        }
    }
}
