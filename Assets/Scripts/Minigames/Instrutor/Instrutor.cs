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
    public Image   imagem;
    public float   tempoEntreAcoes = 4f;
    public ColorInfo[] colors;
    public List<IObserver> _observers = new List<IObserver>();

    public static Instrutor instrutor;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color currentColor;

    [SyncVar(hook = nameof(OnColorNameChanged))]
    public string currentColorName;

    [SyncVar(hook = nameof(OnTimerTextChanged))]
    public string currentTimerText;

    public override void OnStartServer()
    {
        base.OnStartServer();
        instrutor = this;
        StartCoroutine(CicloDeCores());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        instrutor = this;

        imagem.color = currentColor;
        textoCor.text = currentColorName;
        textoTimer.text = currentTimerText;
    }

    IEnumerator CicloDeCores()
    {
        while (true)
        {
            currentColor     = Color.white;
            currentColorName = "Irá começar";
            yield return Countdown(tempoEntreAcoes, 1);

            ColorInfo corEscolhida = EscolherCor();
            currentColor     = corEscolhida.color;
            currentColorName = corEscolhida.colorName;
            yield return Countdown(tempoEntreAcoes, 2);

            Notifica();

            yield return Countdown(tempoEntreAcoes, 1);
            
            currentColor     = Color.white;
            currentColorName = "Irá começar";
            Notifica();
        }
    }
    IEnumerator Countdown(float duration, int tipo)
    {
        float timer = duration;
        while (timer > 0f)
        {
            currentTimerText = tipo == 1 
                ? $"Trocando em {Mathf.Ceil(timer)} s" 
                : $"Sumindo em {Mathf.Ceil(timer)} s";
            timer -= Time.deltaTime;
            yield return null;
        }
        currentTimerText = "";
    }

    void OnColorChanged(Color oldC, Color newC)
    {
        if (imagem != null)
            imagem.color = newC;
    }

    void OnColorNameChanged(string oldName, string newName)
    {
        if (textoCor != null)
            textoCor.text = newName;
    }

    void OnTimerTextChanged(string oldText, string newText)
    {
        if (textoTimer != null)
            textoTimer.text = newText;
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
            observer.Atualizacao(this);
    }
}