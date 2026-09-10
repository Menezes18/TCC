using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ChaoDerrete : ChaoMae
{
    [SerializeField] private Renderer chaoRenderer;
    [SerializeField] private string alphaCutoffPropertyName = "_AlphaClip";
    [SerializeField] private float tempoAteComecarDerreter = 1f;
    [SerializeField] private float tempoParaSumirTotalmente = 2f;

    // A timestamp replaces one alpha-cutoff RPC per frame per melting tile.
    [SyncVar] private double meltStartedAt = -1;
    private readonly HashSet<Collider> occupants = new();
    private float occupiedTime;
    private Material instancedMaterial;
    private Collider[] tileColliders;
    private bool[] initialColliderStates;
    private bool initiallyVisible;
    private int cutoffProperty;
    private float lastProgress = -1;
    private bool? lastRemoved;
    private static readonly System.Predicate<Collider> MissingOccupant = IsMissingOccupant;

    protected override void Awake()
    {
        base.Awake();
        if (chaoRenderer == null) chaoRenderer = GetComponent<Renderer>();
        if (chaoRenderer == null || chaoRenderer.sharedMaterial == null)
        {
            Debug.LogError("Melting floor requires a renderer and material.", this);
            enabled = false;
            return;
        }
        instancedMaterial = new Material(chaoRenderer.sharedMaterial);
        chaoRenderer.sharedMaterial = instancedMaterial;
        cutoffProperty = Shader.PropertyToID(alphaCutoffPropertyName);
        initiallyVisible = chaoRenderer.enabled;
        tileColliders = GetComponentsInChildren<Collider>(true);
        initialColliderStates = new bool[tileColliders.Length];
        for (int i = 0; i < tileColliders.Length; i++)
            initialColliderStates[i] = tileColliders[i].enabled;
        ApplyVisualState();
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            occupants.Add(other);
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        occupants.Remove(other);
        if (occupants.Count == 0 && meltStartedAt < 0) occupiedTime = 0;
    }

    private void Update()
    {
        if (isServer && !chaoTirado)
        {
            occupants.RemoveWhere(MissingOccupant);
            if (meltStartedAt < 0)
            {
                if (occupants.Count == 0) occupiedTime = 0;
                else occupiedTime += Time.deltaTime;
                if (occupants.Count > 0 && occupiedTime >= tempoAteComecarDerreter)
                    meltStartedAt = NetworkTime.time;
            }
            if (meltStartedAt >= 0 && NetworkTime.time - meltStartedAt >= tempoParaSumirTotalmente)
                tiraChao();
        }
        ApplyVisualState();
    }

    private static bool IsMissingOccupant(Collider occupant) =>
        occupant == null || !occupant.enabled || !occupant.gameObject.activeInHierarchy;

    private void ApplyVisualState()
    {
        if (instancedMaterial == null) return;
        float progress = meltStartedAt < 0 ? 0f : Mathf.Clamp01(
            (float)(NetworkTime.time - meltStartedAt) / Mathf.Max(0.001f, tempoParaSumirTotalmente));
        if (progress != lastProgress)
        {
            instancedMaterial.SetFloat(cutoffProperty, progress);
            lastProgress = progress;
        }
        if (lastRemoved == chaoTirado) return;
        lastRemoved = chaoTirado;
        chaoRenderer.enabled = initiallyVisible && !chaoTirado;
        for (int i = 0; i < tileColliders.Length; i++)
            if (tileColliders[i] != null)
                tileColliders[i].enabled = initialColliderStates[i] && !chaoTirado;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisualState();
    }

    [Server]
    public override void tiraChao()
    {
        chaoTirado = true;
        ApplyVisualState();
    }

    [Server]
    public override void poeChao()
    {
        transform.position = posIncial;
        occupiedTime = 0;
        occupants.Clear();
        meltStartedAt = -1;
        chaoTirado = false;
        ApplyVisualState();
    }

    private void OnDestroy()
    {
        if (instancedMaterial != null) Destroy(instancedMaterial);
    }
}
