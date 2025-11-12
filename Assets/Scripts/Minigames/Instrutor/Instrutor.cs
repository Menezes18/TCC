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
    public TMP_Text textoCor, textoCorUI;
    public TMP_Text textoTimer;
    public Image   imagem;
    public float   tempoEntreAcoes = 4f; // legado
    public float   tempoMemorizar = 5f;
    public float   tempoEspera = 2f;
    public float   tempoResolver = 3f;
    public ColorInfo[] colors;
    public List<IObserver> _observers = new List<IObserver>();

    public static Instrutor instrutor;

    public GameObject canvasUIInstrutor;

    public enum MemoryPhase { Idle = 0, Reveal = 1, Hide = 2, Resolve = 3 }

    [SyncVar]
    public MemoryPhase currentPhase = MemoryPhase.Idle;

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
    // Não iniciar automaticamente; o MinigameController controla o início
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        instrutor = this;

        imagem.color = currentColor;
        textoCor.text = currentColorName;
        if(textoCorUI != null) textoCorUI.text = currentColorName;
        textoTimer.text = currentTimerText;
    }

    private bool _isRunning = false;

    [Server]
    public void StartMemoryCycle()
    {
        if (_isRunning) return;
        _isRunning = true;
        StartCoroutine(CicloMemoria());
    }

    IEnumerator CicloMemoria()
    {
        while (true)
        {
            if (canvasUIInstrutor != null)
                canvasUIInstrutor.SetActive(true);
                
            // 1) Revela: cada chão mostra sua cor para memorizar
            currentPhase = MemoryPhase.Reveal;
            currentColor = Color.white;
            currentColorName = "Memorize as cores";
            Notifica(); // tiles randomizam a cor e mostram
            yield return Countdown(tempoMemorizar, 0);

            // 2) Esconde: todos os chãos ficam neutros
            currentPhase = MemoryPhase.Hide;
            currentColor = Color.white;
            currentColorName = "Suba na cor...";
            Notifica(); // tiles escondem a cor
            yield return Countdown(tempoEspera, 1);

            // 3) Escolhe alvo e resolve: apenas a cor correta permanece
            ColorInfo corEscolhida = EscolherCor();
            currentColor     = corEscolhida.color;
            currentColorName = corEscolhida.colorName; // mostra o nome da cor-alvo
            currentPhase = MemoryPhase.Resolve;
            yield return Countdown(tempoResolver, 2);

            // Notifica para eliminar os errados / manter corretos
            Notifica();

            // 4) Pequena pausa antes de um novo ciclo
            currentPhase = MemoryPhase.Idle;
            currentColor = Color.white;
            currentColorName = "Novo ciclo";
            yield return Countdown(tempoEspera, 1);
        }
    }
    IEnumerator Countdown(float duration, int tipo)
    {
        float timer = duration;
        while (timer > 0f)
        {
            // tipo: 0=memorize, 1=prepare, 2=sumindo
            if (tipo == 0)
                currentTimerText = $"Memorize: {Mathf.Ceil(timer)} s";
            else if (tipo == 1)
                currentTimerText = $"Pronto em {Mathf.Ceil(timer)} s";
            else if (tipo == 2)
                currentTimerText = $"Sumindo em {Mathf.Ceil(timer)} s";
            else
                currentTimerText = $"{Mathf.Ceil(timer)} s";
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