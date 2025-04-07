using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Instrutor : NetworkBehaviour, ISubject
{
    public TMP_Text nome;
    public static Instrutor instrutor;
    public Image imagem;
    public List<IObserver> _observers = new List<IObserver>();
    public float tempoEntreAcoes = 4f;
    public Color[] colors;

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
        while(true)
        {
            Color corEscolhida = EscolherCor();
            currentColor = corEscolhida;

            imagem.color = corEscolhida;

            yield return new WaitForSeconds(tempoEntreAcoes);

            Notifica();

            yield return new WaitForSeconds(tempoEntreAcoes);

            currentColor = Color.white;
            imagem.color = Color.white;
            Notifica();

            yield return new WaitForSeconds(tempoEntreAcoes);
        }
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        if(imagem != null)
        {
            imagem.color = newColor;
        }
    }

    Color EscolherCor()
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
