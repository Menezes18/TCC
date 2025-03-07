using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContadorTempo : MonoBehaviour
{
    [SerializeField]
    private TempoSo data;

    [SerializeField]
    private TMP_Text uiTempoInicial;

    [SerializeField]
    private TMP_Text uiTempoPrincipal;

    private float tempoInicialAtual;
    private float tempoPrincipalAtual;

    private void Start()
    {
        tempoInicialAtual = data.tempoInicial;
        tempoPrincipalAtual = data.tempoPrincipal;

        StartCoroutine(ContagemInicial());
    }

    private IEnumerator ContagemInicial()
    {
        while (tempoInicialAtual > 0)
        {
            uiTempoInicial.text = tempoInicialAtual.ToString("F0");
            yield return new WaitForSeconds(1);
            tempoInicialAtual -= 1;
        }

        uiTempoInicial.text = "Começou";
        yield return new WaitForSeconds(1);

        uiTempoInicial.gameObject.SetActive(false);

        StartCoroutine(ContagemPrincipal());
    }

    private IEnumerator ContagemPrincipal()
    {
        while (tempoPrincipalAtual > 0)
        {
            int minutos = Mathf.FloorToInt(tempoPrincipalAtual / 60f);
            int segundos = Mathf.FloorToInt(tempoPrincipalAtual % 60);
            uiTempoPrincipal.text = string.Format("{0:0}:{1:00}", minutos, segundos);
            yield return new WaitForSeconds(1);
            tempoPrincipalAtual -= 1;
        }

        uiTempoPrincipal.text = "Tempo Esgotado!";
    }
}
