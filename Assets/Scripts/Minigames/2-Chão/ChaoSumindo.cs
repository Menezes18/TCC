using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder;

public class ChaoSumindo : NetworkBehaviour, IObserver
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
        var instr = subject as Instrutor;
        if (instr == null) return;

        if (isServer)  // só o servidor decide
        {
            if (currentColor != instr.currentColor && instr.currentColor != Color.white)
                RpcTiraChao();
            else
                RpcPoeChao();
        }
    }

    [ClientRpc]
    public void RpcPoeChao()
    {
        gameObject.SetActive(true);
    }

    [ClientRpc]
    public void RpcTiraChao()
    {
        gameObject.SetActive(false);
    }
}
