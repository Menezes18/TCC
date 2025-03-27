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

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = true;
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorNoTile = false;
        }
    }

    [ServerCallback]
    private void Update()
    {
        if (jogadorNoTile && !chaoTirado)
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
    }
}
