using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoQuebrando : ChaoMae
{
    [SerializeField]
    private GameObject[] estadosChao;
    private float tempoAcumulado = 0f;
    private bool jogadorNoTile = false;
    private int indiceEstadoAtual = 0;

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
        if (jogadorNoTile && !chaoTirado)
        {
            tempoAcumulado += Time.deltaTime;
            if (tempoAcumulado >= dataChao.tempo)
            {
                tempoAcumulado = 0;
                CmdEstado();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdEstado()
    {
        AtualizaEstado();
        RpcAtualizaEstado(indiceEstadoAtual);
    }

    [Server]
    private void AtualizaEstado()
    {
        indiceEstadoAtual++;

        if (indiceEstadoAtual < estadosChao.Length)
        {
            estadosChao[indiceEstadoAtual].SetActive(true);
            estadosChao[indiceEstadoAtual - 1].SetActive(false);
        }
        else
        {
            tiraChao();
        }
    }

    [ClientRpc]
    private void RpcAtualizaEstado(int novoIndice)
    {
        if (isServer) return; // Evita que o servidor execute isso duas vezes

        if (novoIndice < estadosChao.Length)
        {
            estadosChao[novoIndice].SetActive(true);
            estadosChao[novoIndice - 1].SetActive(false);
        }
        else
        {
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
        transform.position = posIncial;
        tempoAcumulado = 0f;
        chaoTirado = false;
        indiceEstadoAtual = 0;
        estadosChao[indiceEstadoAtual].SetActive(true);
        RpcResetarChao();
    }

    [ClientRpc]
    private void RpcResetarChao()
    {
        if (isServer) return;

        transform.position = posIncial;
        tempoAcumulado = 0f;
        chaoTirado = false;
        indiceEstadoAtual = 0;
        estadosChao[indiceEstadoAtual].SetActive(true);
    }
}
