using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GlassTile : NetworkBehaviour
{
    [Tooltip("Índice da linha (0..N) do percurso")] public int rowIndex = 0;
    [Tooltip("0 = esquerda, 1 = direita")] public int side = 0;

    [SerializeField] private SkinnedMeshRenderer[] renderers;
    [SerializeField] private Collider[] colliders;

    [SyncVar] private bool _isSafe;
    [SyncVar] private bool _isBroken;
    private float _restoreDelay = 2.0f;
    private GlassMinigameController _controller;

    public void ServerBindController(GlassMinigameController ctrl) => _controller = ctrl;
    [Server] public void ServerSetSafe(bool safe) => _isSafe = safe;
    [Server] public void ServerSetRestoreDelay(float delay) => _restoreDelay = delay;

    [ServerCallback]
    private void Awake()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;

        if (_isSafe)
        {
            _controller?.ServerOnSafeTileStepped(pd, rowIndex);
        }
        else
        {
            if (!_isBroken)
                StartCoroutine(ServerBreakAndRestore());
        }
    }

    [Server]
    private IEnumerator ServerBreakAndRestore()
    {
        _isBroken = true;
        SetTileEnabled(false);
        RpcSetTileEnabled(false);
        yield return new WaitForSeconds(_restoreDelay);
        SetTileEnabled(true);
        RpcSetTileEnabled(true);
        _isBroken = false;
    }

    [Server]
    private void SetTileEnabled(bool enabled)
    {
        if (renderers != null)
            foreach (var r in renderers) if (r != null) r.enabled = enabled;
        if (colliders != null)
            foreach (var c in colliders) if (c != null) c.enabled = enabled;
    }

    [ClientRpc]
    private void RpcSetTileEnabled(bool enabled)
    {
        if (renderers != null)
            foreach (var r in renderers) if (r != null) r.enabled = enabled;
        if (colliders != null)
            foreach (var c in colliders) if (c != null) c.enabled = enabled;
    }

#if UNITY_EDITOR
    [Header("Editor Preview")] public bool previewSafe;

    private void OnDrawGizmos()
    {
        // Desenhar indicação de seguro/perigoso no editor
        Gizmos.matrix = transform.localToWorldMatrix;
        var size = new Vector3(0.9f, 0.02f, 0.9f);
        Color c = previewSafe ? new Color(0f, 0.8f, 0.2f, 0.35f) : new Color(0.9f, 0f, 0f, 0.35f);
        Gizmos.color = c;
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = new Color(0f, 0f, 0f, 0.7f);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
#endif
}
