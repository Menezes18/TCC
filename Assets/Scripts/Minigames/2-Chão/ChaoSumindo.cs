using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder;

public class ChaoSumindo : ChaoMae, IObserver
{
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color currentColor;

    public Color[] colors;
    private Material cor;

    void Start()
    {
        cor = GetComponent<Renderer>().material;
        
        if(isServer)
        {
            currentColor = colors[Random.Range(0, colors.Length)];
            cor.color = currentColor;
        }

        Instrutor.instrutor.Adicionar(this);
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        if(cor == null)
        {
            cor = GetComponent<Renderer>().material;
        }
        cor.color = newColor;
    }

    public void Atualizacao(ISubject subject)
    {
        Instrutor instrutor = subject as Instrutor;
        if(instrutor != null)
        {
            if(currentColor == instrutor.imagem.color)
            {
                tiraChao();
            }
            else
            {
                poeChao();
            }
        }
    }

    [Server]
    public override void poeChao()
    {
        gameObject.SetActive(true);
    }

    [Server]
    public override void tiraChao()
    {
        gameObject.SetActive(false);
    }
}
