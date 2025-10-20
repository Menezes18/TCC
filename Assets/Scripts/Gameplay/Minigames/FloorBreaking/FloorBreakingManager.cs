using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FloorBreakingManager : NetworkBehaviour
{
    [System.Serializable]
    public struct TileState
    {
        public int indiceEstado;
        public bool destruido;
    }

    [SerializeField] private ChaoQuebrandoSimples[] tiles;
    
    private readonly SyncList<TileState> tileStates = new SyncList<TileState>();
    
    private Dictionary<ChaoQuebrandoSimples, int> tileToIdMap = new Dictionary<ChaoQuebrandoSimples, int>();

    private void Start()
    {
        StartCoroutine(InicializarTilesGradualmente());
    }

    private System.Collections.IEnumerator InicializarTilesGradualmente()
    {
        int lote = 100;
        
        for (int i = 0; i < tiles.Length; i++)
        {
            tileToIdMap[tiles[i]] = i;
            tiles[i].SetTileId(i);
            tiles[i].SetManager(this);

            if (i % lote == 0 && i > 0)
            {
                yield return null;
            }
        }

        if (isServer)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                tileStates.Add(new TileState { indiceEstado = 0, destruido = false });
                
                if (i % lote == 0 && i > 0)
                {
                    yield return null;
                }
            }
        }

        tileStates.Callback += OnTileStatesChanged;
    }

    [Server]
    public void NotificarProgressaoTile(int tileId, int novoIndice)
    {
        if (tileId >= 0 && tileId < tileStates.Count)
        {
            var state = tileStates[tileId];
            state.indiceEstado = novoIndice;
            tileStates[tileId] = state;
            
            RpcAtualizarTileClientes(tileId, novoIndice, false);
        }
    }

    [Server]
    public void NotificarDestruicaoTile(int tileId)
    {
        if (tileId >= 0 && tileId < tileStates.Count)
        {
            var state = tileStates[tileId];
            state.destruido = true;
            tileStates[tileId] = state;
            
            RpcAtualizarTileClientes(tileId, state.indiceEstado, true);
        }
    }

    [ClientRpc]
    private void RpcAtualizarTileClientes(int tileId, int indice, bool destruido)
    {
        if (!isServer && tileId >= 0 && tileId < tiles.Length)
        {
            tiles[tileId].AtualizarVisualizacaoRemota(indice, destruido);
        }
    }

    [Server]
    public void ResetarTile(int tileId)
    {
        if (tileId >= 0 && tileId < tileStates.Count)
        {
            tileStates[tileId] = new TileState { indiceEstado = 0, destruido = false };
            tiles[tileId].ResetarTile();
        }
    }

    [Server]
    public void ResetarTodosTiles()
    {
        StartCoroutine(ResetarTilesGradualmente());
    }

    private System.Collections.IEnumerator ResetarTilesGradualmente()
    {
        int lote = 50;
        
        for (int i = 0; i < tileStates.Count; i++)
        {
            tileStates[i] = new TileState { indiceEstado = 0, destruido = false };
            tiles[i].ResetarTile();
            
            if (i % lote == 0 && i > 0)
            {
                yield return null;
            }
        }
    }

    private void OnTileStatesChanged(SyncList<TileState>.Operation op, int index, TileState oldItem, TileState newItem)
    {
        if (!isServer && index < tiles.Length)
        {
            tiles[index].AtualizarVisualizacaoRemota(newItem.indiceEstado, newItem.destruido);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!isServer)
        {
            StartCoroutine(SincronizarClienteGradualmente());
        }
    }

    private System.Collections.IEnumerator SincronizarClienteGradualmente()
    {
        int lote = 100;
        
        for (int i = 0; i < tileStates.Count && i < tiles.Length; i++)
        {
            tiles[i].AtualizarVisualizacaoRemota(tileStates[i].indiceEstado, tileStates[i].destruido);
            
            if (i % lote == 0 && i > 0)
            {
                yield return null;
            }
        }
    }
}
