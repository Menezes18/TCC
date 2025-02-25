using System.Collections;
using UnityEngine;

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
            if(tempoAcumulado >= dataChao.tempo){
                tempoAcumulado = 0;
                AtualizaEstado();
            }
        }
    }

    private void AtualizaEstado()
    {
        indiceEstadoAtual++;

        if(indiceEstadoAtual < estadosChao.Length){
            estadosChao[indiceEstadoAtual].SetActive(true);
            estadosChao[indiceEstadoAtual - 1].SetActive(false);
        }
        else{
            tiraChao();
        }
    }

    public override void tiraChao()
    {
        chaoTirado = true;
        StartCoroutine(DesativaTile());
    }

    private IEnumerator DesativaTile()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    public override void poeChao()
    {
        transform.position = posIncial;
        tempoAcumulado = 0f;
        chaoTirado = false;
        indiceEstadoAtual = 0;
        estadosChao[indiceEstadoAtual].SetActive(true);
    }
}
