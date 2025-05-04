using System.Collections;
using UnityEngine;
using TMPro;
using Mirror;
using DG.Tweening;
using System.Collections.Generic;

public class ContadorTempo : NetworkBehaviour, ISubject
{
    [SerializeField] private TempoSo data;

    [SerializeField] private TMP_Text uiTempoInicial;
    [SerializeField] private TMP_Text uiTempoPrincipal;
    [SerializeField] private string nomeCena = "MiniGame";
    public List<IObserver> _observers = new List<IObserver>();

    [SyncVar(hook = nameof(OnTempoInicialChanged))]
    private float tempoInicialAtual;

    [SyncVar(hook = nameof(OnTempoPrincipalChanged))]
    private float tempoPrincipalAtual;

    private bool contadorIniciado = false;

    // Efeitos visuais
    private Tween shakeTween;
    private Tween colorTween;
    private bool efeitoFinalAtivado = false;

    [SerializeField] private Color corOriginal = Color.white;
    [SerializeField] private Color corAlerta = Color.red;

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
        if (contadorIniciado) return;

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

            if (newValue <= 10 && !efeitoFinalAtivado)
            {
                efeitoFinalAtivado = true;

                // Shake
                shakeTween = uiTempoPrincipal.rectTransform
                    .DOShakeAnchorPos(0.5f, 10f, 20, 90, false, true)
                    .SetLoops(-1, LoopType.Restart);

                // Piscar cor
                colorTween = uiTempoPrincipal
                    .DOColor(corAlerta, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        else
        {
            uiTempoPrincipal.text = "Tempo Esgotado!";

            shakeTween?.Kill();
            colorTween?.Kill();
            uiTempoPrincipal.color = corOriginal;
            efeitoFinalAtivado = false;
            Notifica();

            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(nomeCena);
            }
        }
    }

    public void Adicionar(IObserver observer)
    {
        _observers.Add(observer);
    }

    public void Retira(IObserver observer)
    {
        _observers.Remove(observer);
    }

    public void Notifica()
    {
        foreach (IObserver observer in _observers)
        {
            observer.Atualizacao(this);
        }
    }
}
