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
    private bool hasAssignedColor = false;

    void Start()
    {
        cor = GetComponent<Renderer>().material;
        
        if (Instrutor.instrutor != null)
            Instrutor.instrutor.Adicionar(this);
        else
            StartCoroutine(EsperarInstrutor());
    }

    private IEnumerator EsperarInstrutor()
    {
        while (Instrutor.instrutor == null)
            yield return null;
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

        if (!isServer) return; // apenas o servidor decide

        switch (instr.currentPhase)
        {
            case Instrutor.MemoryPhase.Reveal:
                // Escolhe e mostra uma cor para memorizar
                if (!hasAssignedColor)
                {
                    if (Instrutor.instrutor != null && Instrutor.instrutor.colors != null && Instrutor.instrutor.colors.Length > 0)
                    {
                        var ci = Instrutor.instrutor.colors[Random.Range(0, Instrutor.instrutor.colors.Length)];
                        currentColor = ci.color;
                    }
                    else if (colors != null && colors.Length > 0)
                    {
                        currentColor = colors[Random.Range(0, colors.Length)];
                    }
                    hasAssignedColor = true;
                }
                RpcShowColor(currentColor);
                RpcPoeChao();
                break;
            case Instrutor.MemoryPhase.Hide:
                // Esconde a cor (material neutro)
                RpcHideColor();
                break;
            case Instrutor.MemoryPhase.Resolve:
                // Mantém apenas quem corresponde à cor-alvo
                if (hasAssignedColor && ColorsEqual(currentColor, instr.currentColor))
                    RpcPoeChao();
                else
                    RpcTiraChao();
                // prepara para próximo round reatribuir cor
                hasAssignedColor = false;
                break;
            case Instrutor.MemoryPhase.Idle:
            default:
                // Nada específico
                break;
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

    [ClientRpc]
    public void RpcShowColor(Color c)
    {
        if (cor == null)
            cor = GetComponent<Renderer>().material;
        cor.color = c;
    }

    [ClientRpc]
    public void RpcHideColor()
    {
        if (cor == null)
            cor = GetComponent<Renderer>().material;
        // Define uma cor neutra (cinza claro) para esconder
        cor.color = Color.gray;
    }

    private bool ColorsEqual(Color a, Color b)
    {
        // Comparação com tolerância para evitar problemas de precisão
        return Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);
    }
}
