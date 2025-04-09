using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class ColorInfo {
    public string colorName;
    public Color color;
}

public class Instrutor : NetworkBehaviour, ISubject
{
    public TMP_Text textoCor;
    public TMP_Text textoTimer;
    public static Instrutor instrutor;
    public Image imagem;
    public List<IObserver> _observers = new List<IObserver>();
    public float tempoEntreAcoes = 4f;
    public ColorInfo[] colors;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color currentColor;

    public override void OnStartServer()
    {
        base.OnStartServer();
        instrutor = this;
        StartCoroutine(CicloDeCores());
    }

    IEnumerator CicloDeCores()
    {
        while (true)
        {
            currentColor = Color.white;
            imagem.color = Color.white;
            textoCor.text = "Irá começar";
            
            yield return StartCoroutine(Countdown(tempoEntreAcoes, 1));

            ColorInfo corEscolhida = EscolherCor();
            currentColor = corEscolhida.color;
            imagem.color = corEscolhida.color;
            textoCor.text = corEscolhida.colorName;
            
            yield return StartCoroutine(Countdown(tempoEntreAcoes, 2));
            Notifica();
            
            yield return StartCoroutine(Countdown(tempoEntreAcoes, 1));
            currentColor = Color.white;
            imagem.color = Color.white;
            textoCor.text = "Irá começar";

            Notifica();
        }
    }
    IEnumerator Countdown(float duration, int tipo)
    {
        float timer = duration;
        while (timer > 0)
        {
            textoTimer.text = tipo == 1 ? $"Trocando em {Mathf.Ceil(timer)} s" : $"Sumindo em {Mathf.Ceil(timer)} s";
            timer -= Time.deltaTime;
            yield return null;
        }
        textoTimer.text = "";
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        if (imagem != null)
        {
            imagem.color = newColor;
        }
    }

    ColorInfo EscolherCor()
    {
        return colors[Random.Range(0, colors.Length)];
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
        foreach (var observer in _observers)
        {
            observer.Atualizacao(this);
        }
    }
}