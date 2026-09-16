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

[System.Serializable]
public class InstrutorUIDisplay
{
    public GameObject canvas;
    public TMP_Text textoCor;
    public TMP_Text textoTimer;
    public Image imagem;
}

public class Instrutor : NetworkBehaviour, ISubject
{
    public List<InstrutorUIDisplay> displays = new List<InstrutorUIDisplay>();
    public float   tempoEntreAcoes = 4f; // legado
    public float   tempoMemorizar = 5f;
    public float   tempoEspera = 2f;
    public float   tempoResolver = 3f;
    public ColorInfo[] colors;
    public List<IObserver> _observers = new List<IObserver>();

    public static Instrutor instrutor;

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

        ApplyColorToDisplays(currentColor);
        ApplyColorNameToDisplays(currentColorName);
        ApplyTimerToDisplays(currentTimerText);
    }

    private bool _isRunning = false;
    private Coroutine _memoryCycle;

    [Server]
    public void StartMemoryCycle()
    {
        if (_isRunning) return;
        _isRunning = true;
        _memoryCycle = StartCoroutine(CicloMemoria());
    }

    [Server]
    public void StopMemoryCycle()
    {
        if (_memoryCycle != null) StopCoroutine(_memoryCycle);
        _memoryCycle = null;
        _isRunning = false;
        currentPhase = MemoryPhase.Idle;
        currentTimerText = string.Empty;
        SetCanvasesActive(false);
    }

    IEnumerator CicloMemoria()
    {
        while (true)
        {
            SetCanvasesActive(true);
                
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
        int lastDisplayedSecond = int.MinValue;
        while (timer > 0f)
        {
            int displayedSecond = Mathf.CeilToInt(timer);
            if (displayedSecond != lastDisplayedSecond)
            {
                currentTimerText = FormatTimerText(tipo, displayedSecond);
                lastDisplayedSecond = displayedSecond;
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        currentTimerText = "";
    }

    private static string FormatTimerText(int tipo, int seconds)
    {
        // tipo: 0=memorize, 1=prepare, 2=sumindo
        return tipo switch
        {
            0 => $"Memorize: {seconds} s",
            1 => $"Pronto em {seconds} s",
            2 => $"Sumindo em {seconds} s",
            _ => $"{seconds} s"
        };
    }

    void OnColorChanged(Color oldC, Color newC)
    {
        ApplyColorToDisplays(newC);
    }

    void OnColorNameChanged(string oldName, string newName)
    {
        ApplyColorNameToDisplays(newName);
    }

    void OnTimerTextChanged(string oldText, string newText)
    {
        ApplyTimerToDisplays(newText);
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

    void ApplyColorToDisplays(Color color)
    {
        foreach (var display in displays)
        {
            if (display == null) continue;
            if (display.imagem != null)
                display.imagem.color = color;
        }
    }

    void ApplyColorNameToDisplays(string colorName)
    {
        foreach (var display in displays)
        {
            if (display == null) continue;
            if (display.textoCor != null)
                display.textoCor.text = colorName;
        }
    }

    void ApplyTimerToDisplays(string timerText)
    {
        foreach (var display in displays)
        {
            if (display == null) continue;
            if (display.textoTimer != null)
                display.textoTimer.text = timerText;
        }
    }

    void SetCanvasesActive(bool active)
    {
        foreach (var display in displays)
        {
            if (display == null) continue;
            if (display.canvas != null)
                display.canvas.SetActive(active);
        }
    }
}
