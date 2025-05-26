using UnityEngine;
using UnityEngine.Events;

public class TrainSignalController : MonoBehaviour
{
    [Header("Signal Settings")]
    [SerializeField] private bool isGreen = true;
    [SerializeField] private float changeIntervalInit = 5f; // Tempo para alternar automaticamente
    [SerializeField] private float changeIntervalEnd = 8f;
    [SerializeField] private bool autoChange = false; // Se deve alternar automaticamente
    
    [Header("Visual Components")]
    [SerializeField] private Light signalLight; // Luz do sinal (opcional)
    [SerializeField] private Renderer signalRenderer; // Renderer para mudar cor (opcional)
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    
    [Header("Train Lane References")]
    [SerializeField] private VehicleLane[] trainLanes; // Array de lanes de trem para controlar
    
    [Header("Events")]
    public UnityEvent OnSignalGreen; // Evento quando sinal fica verde
    public UnityEvent OnSignalRed;   // Evento quando sinal fica vermelho
    
    private float timer;

    private void Start()
    {
        // Conecta automaticamente as lanes de trem aos eventos
        ConnectTrainLanes();

        // Define estado inicial
        SetSignalState(isGreen);
        
    }
    
    private void Update()
    {
        if (autoChange)
        {
            timer += Time.deltaTime;
            if (timer >= changeIntervalInit)
            {
                ToggleSignal();
                timer = 0f;
                changeIntervalInit = Random.Range(changeIntervalInit, changeIntervalEnd);
            }
        }
    }
    
    private void ConnectTrainLanes()
    {
        // Se não foram definidas lanes manualmente, encontra automaticamente
        if (trainLanes == null || trainLanes.Length == 0)
        {
            trainLanes = FindObjectsOfType<VehicleLane>();
        }
        
        // Conecta os eventos às lanes
        foreach (var lane in trainLanes)
        {
            if (lane != null)
            {
                OnSignalGreen.AddListener(lane.SetTrainSpeedGreen);
                OnSignalRed.AddListener(lane.SetTrainSpeedRed);
            }
        }
    }
    
    public void ToggleSignal()
    {
        SetSignalState(!isGreen);
    }
    
    public void SetSignalGreen()
    {
        SetSignalState(true);
    }
    
    public void SetSignalRed()
    {
        SetSignalState(false);
    }
    
    private void SetSignalState(bool green)
    {
        isGreen = green;
        
        // Atualiza componentes visuais
        UpdateVisuals();
        
        // Dispara eventos
        if (isGreen)
        {
            OnSignalGreen?.Invoke();
        }
        else
        {
            OnSignalRed?.Invoke();
        }
    }
    
    private void UpdateVisuals()
    {
        // Atualiza luz se existir
        if (signalLight != null)
        {
            signalLight.color = isGreen ? Color.green : Color.red;
        }
        
        // Atualiza material se existir
        if (signalRenderer != null)
        {
            if (isGreen && greenMaterial != null)
                signalRenderer.material = greenMaterial;
            else if (!isGreen && redMaterial != null)
                signalRenderer.material = redMaterial;
        }
    }
    
    // Métodos para serem chamados por outros scripts ou animações
    public void OnAnimationGreen()
    {
        SetSignalGreen();
    }
    
    public void OnAnimationRed()
    {
        SetSignalRed();
    }
}