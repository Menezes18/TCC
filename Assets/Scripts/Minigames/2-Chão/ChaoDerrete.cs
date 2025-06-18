using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoDerrete : ChaoMae
{
    [SerializeField]
    private Renderer chaoRenderer; // Referência ao Renderer do objeto que tem o material com Alpha Clip
    [SerializeField]
    private string alphaCutoffPropertyName = "_AlphaClip"; // Nome da propriedade do shader que controla o Alpha Clip
    [SerializeField]
    [Range(0.01f, 1f)]
    private float taxaDerretimento = 0.1f; // Velocidade com que o chão "derrete" (altera o cutoff)
    [SerializeField]
    private float tempoAteComecarDerreter = 1.0f; // Tempo que o jogador precisa ficar no chão para começar a derreter
    [SerializeField]
    private float tempoParaSumirTotalmente = 2.0f; // Tempo que leva para o chão sumir completamente após iniciar o derretimento

    private Material instancedMaterial; // Instância do material para evitar modificar o asset original
    private float tempoAcumuladoNoChao = 0f;
    private float tempoDerretendo = 0f;
    private bool jogadorNoTile = false;
    private bool estaDerretendo = false;

    // Constante para o valor inicial do cutoff (totalmente visível)
    private const float FULLY_VISIBLE_CUTOFF = 0f;
    // Constante para o valor final do cutoff (totalmente invisível)
    private const float FULLY_INVISIBLE_CUTOFF = 1f;

    void Awake()
    {
        if (chaoRenderer == null)
        {
            chaoRenderer = GetComponent<Renderer>();
        }

        if (chaoRenderer != null && chaoRenderer.material != null)
        {
            // Cria uma instância do material para que as mudanças não afetem outros objetos
            instancedMaterial = new Material(chaoRenderer.material);
            chaoRenderer.material = instancedMaterial;
        }
        else
        {
            Debug.LogError("ChaoDerreteAlpha: Renderer ou Material não encontrado! Certifique-se de que o objeto tem um Renderer com um material que suporte Alpha Clip.");
            enabled = false; // Desativa o script se não houver renderer/material
        }
    }

    private void Start()
    {
        // Garante que o chão esteja visível no início
        if (instancedMaterial != null)
        {
            instancedMaterial.SetFloat(alphaCutoffPropertyName, FULLY_VISIBLE_CUTOFF);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = true;
            // Só inicia o derretimento se o chão ainda não estiver tirado
            if (!chaoTirado && !estaDerretendo)
            {
                RpcIniciarContagemDerretimento();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = false;
            // Reseta a contagem se o jogador sair antes de começar a derreter
            if (!estaDerretendo)
            {
                RpcPararContagemDerretimento();
            }
        }
    }

    private void Update()
    {
        if (!isServer) return; // A lógica de derretimento será gerenciada pelo servidor

        if (jogadorNoTile && !chaoTirado && !estaDerretendo)
        {
            tempoAcumuladoNoChao += Time.deltaTime;
            if (tempoAcumuladoNoChao >= tempoAteComecarDerreter)
            {
                estaDerretendo = true;
                RpcIniciarDerretimento();
            }
        }

        if (estaDerretendo && !chaoTirado)
        {
            tempoDerretendo += Time.deltaTime;
            float progressoDerretimento = Mathf.Clamp01(tempoDerretendo / tempoParaSumirTotalmente);
            float novoCutoff = Mathf.Lerp(FULLY_VISIBLE_CUTOFF, FULLY_INVISIBLE_CUTOFF, progressoDerretimento);

            RpcAtualizarAlphaCutoff(novoCutoff);

            if (progressoDerretimento >= 1f)
            {
                tiraChao(); // O chão sumiu completamente
            }
        }
    }

    [ClientRpc]
    private void RpcIniciarContagemDerretimento()
    {
        if (!isServer)
        {
            // O cliente também precisa controlar o tempo acumulado para feedback visual imediato
            // Embora o servidor seja o autoritário, o cliente pode pré-visualizar
            tempoAcumuladoNoChao = 0f;
            Debug.Log("Cliente: Contagem de derretimento iniciada.");
        }
    }

    [ClientRpc]
    private void RpcPararContagemDerretimento()
    {
        if (!isServer)
        {
            // O cliente também precisa parar e resetar a contagem
            tempoAcumuladoNoChao = 0f;
            Debug.Log("Cliente: Contagem de derretimento parada.");
        }
    }

    [ClientRpc]
    private void RpcIniciarDerretimento()
    {
        if (!isServer)
        {
            estaDerretendo = true;
            tempoDerretendo = 0f;
            Debug.Log("Cliente: Derretimento iniciado!");
        }
    }

    [ClientRpc]
    private void RpcAtualizarAlphaCutoff(float novoCutoff)
    {
        if (instancedMaterial != null)
        {
            instancedMaterial.SetFloat(alphaCutoffPropertyName, novoCutoff);
        }
    }

    [Server]
    public override void tiraChao()
    {
        if (chaoTirado) return; // Evita múltiplas chamadas
        chaoTirado = true;
        RpcDesativarOuDestruir();
    }

    [ClientRpc]
    private void RpcDesativarOuDestruir()
    {
        // Decida se quer desativar ou destruir.
        // Se for um objeto que será reaparecido, desativar é melhor.
        // Se for um objeto que não será reaparecido, destruir pode ser melhor para liberar memória.
        // Neste exemplo, vamos desativar para possibilitar o reaparecimento.
        gameObject.SetActive(false);
        Debug.Log("Chão desativado em todos os clientes.");
    }

    [Server]
    public override void poeChao()
    {
        if (!chaoTirado) return; // Evita múltiplas chamadas
        transform.position = posIncial;
        tempoAcumuladoNoChao = 0f;
        tempoDerretendo = 0f;
        jogadorNoTile = false;
        estaDerretendo = false;
        chaoTirado = false;
        RpcResetarChao();
    }

    [ClientRpc]
    private void RpcResetarChao()
    {
        if (isServer) return; // O servidor já executou sua parte

        transform.position = posIncial;
        tempoAcumuladoNoChao = 0f;
        tempoDerretendo = 0f;
        jogadorNoTile = false;
        estaDerretendo = false;
        chaoTirado = false;
        gameObject.SetActive(true);
        if (instancedMaterial != null)
        {
            instancedMaterial.SetFloat(alphaCutoffPropertyName, FULLY_VISIBLE_CUTOFF);
        }
        Debug.Log("Chão resetado em todos os clientes.");
    }
}