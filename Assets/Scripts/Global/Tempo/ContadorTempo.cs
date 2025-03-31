using System.Collections;
using UnityEngine;
using TMPro;
using Mirror;

public class ContadorTempo : NetworkBehaviour
{
    [SerializeField]
    private TempoSo data;

    [SerializeField]
    private TMP_Text uiTempoInicial;

    [SerializeField]
    private TMP_Text uiTempoPrincipal;

    [SyncVar(hook = nameof(OnTempoInicialChanged))]
    private float tempoInicialAtual;
    
    [SyncVar(hook = nameof(OnTempoPrincipalChanged))]
    private float tempoPrincipalAtual;
    private bool contadorIniciado = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (tempoInicialAtual <= 0)
        {
            uiTempoInicial.gameObject.SetActive(false);
        }
    }

    [Server]
    public void IniciarContador()
    {
        if(contadorIniciado) return;

        contadorIniciado = true;
        tempoInicialAtual = data.tempoInicial;
        tempoPrincipalAtual = data.tempoPrincipal;
        StartCoroutine(ContagemInicial());
    }

    [Server]
    private IEnumerator ContagemInicial()
    {
        RpcShowTempoInicial();
        while (tempoInicialAtual > 0)
        {
            yield return new WaitForSeconds(1);
            tempoInicialAtual -= 1;
        }
        
        yield return new WaitForSeconds(1);
        RpcHideTempoInicial();
        StartCoroutine(ContagemPrincipal());
    }

    [ClientRpc]
    private void RpcHideTempoInicial()
    {
        uiTempoInicial.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void RpcShowTempoInicial()
    {
        uiTempoInicial.gameObject.SetActive(true);
    }

    [Server]
    private IEnumerator ContagemPrincipal()
    {
        while (tempoPrincipalAtual > 0)
        {
            yield return new WaitForSeconds(1);
            tempoPrincipalAtual -= 1;
        }
    }

    private void OnTempoInicialChanged(float oldValue, float newValue)
    {
        uiTempoInicial.text = newValue > 0 ? newValue.ToString("F0") : "Começou";
    }

    private void OnTempoPrincipalChanged(float oldValue, float newValue)
    {
        if (newValue > 0)
        {
            int minutos = Mathf.FloorToInt(newValue / 60f);
            int segundos = Mathf.FloorToInt(newValue % 60);
            uiTempoPrincipal.text = string.Format("{0:0}:{1:00}", minutos, segundos);
        }
        else
        {
            uiTempoPrincipal.text = "Tempo Esgotado!";
            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene("MiniGame");
            }
        }
    }
}
