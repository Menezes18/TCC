using System;
using TMPro;
using UnityEngine;
using Mirror;

public class RecordeDistanciaEndpoint : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text distanciaText;
    public Transform endpoint;

    [Tooltip("0 = X, 1 = Y, 2 = Z")]
    public int eixoMedicao = 2; 

    [SerializeField]
    private float posicaoInicial = -205.5f;
    private float maxDistancia = 0;
    private float distanciaAtual = 0f;

    // cache e throttling
    private Transform cachedPlayer;
    private float uiUpdateInterval = 0.1f; // 10x por segundo
    private float nextUiUpdate;

    private void Start()
    {
        if (endpoint == null)
        {
            endpoint = transform;
        }
        CacheLocalPlayer();
    }

    private void Update()
    {
        // revalida cache ocasionalmente, evitando Find por frame
        if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy)
        {
            CacheLocalPlayer();
            if (cachedPlayer == null)
                return;
        }

        // throttling de UI
        if (Time.unscaledTime < nextUiUpdate)
            return;
        nextUiUpdate = Time.unscaledTime + uiUpdateInterval;

        float posicaoAtual;
        switch (eixoMedicao)
        {
            case 0: posicaoAtual = cachedPlayer.position.x; break;
            case 1: posicaoAtual = cachedPlayer.position.y; break;
            default: posicaoAtual = cachedPlayer.position.z; break;
        }

        distanciaAtual = Mathf.Max(0, posicaoAtual - posicaoInicial);

        if (distanciaAtual > maxDistancia)
        {
            maxDistancia = distanciaAtual;
            if (distanciaText != null)
            {
                distanciaText.text = "Recorde: " + Mathf.RoundToInt(maxDistancia).ToString();
            }
        }
    }

    private void CacheLocalPlayer()
    {
        // tenta via Mirror primeiro
        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            cachedPlayer = NetworkClient.localPlayer.transform;
            switch (eixoMedicao)
            {
                case 0: posicaoInicial = (endpoint != null ? endpoint.position.x : transform.position.x); break;
                case 1: posicaoInicial = (endpoint != null ? endpoint.position.y : transform.position.y); break;
                default: posicaoInicial = (endpoint != null ? endpoint.position.z : transform.position.z); break;
            }
            maxDistancia = 0f;
            return;
        }

        // fallback único quando ainda não há localPlayer
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;

        var netId = go.GetComponent<NetworkIdentity>();
        if (netId != null && !netId.isLocalPlayer) return;

        cachedPlayer = go.transform;
        switch (eixoMedicao)
        {
            case 0: posicaoInicial = (endpoint != null ? endpoint.position.x : transform.position.x); break;
            case 1: posicaoInicial = (endpoint != null ? endpoint.position.y : transform.position.y); break;
            default: posicaoInicial = (endpoint != null ? endpoint.position.z : transform.position.z); break;
        }
        maxDistancia = 0f;
    }
}