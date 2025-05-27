using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class TrainSignalController : NetworkBehaviour
{
    [Header("Signal Settings")]
    [SyncVar(hook = nameof(OnSignalStateChanged))]
    [SerializeField] private bool isGreen = true;
    
    [SerializeField] private float changeIntervalInit = 5f;
    [SerializeField] private float changeIntervalEnd = 8f;
    [SerializeField] private bool autoChange = false;
    
    [Header("Visual Components")]
    [SerializeField] private Light signalLight;
    [SerializeField] private Renderer signalRenderer;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    
    [Header("Train Lane References")]
    [SerializeField] private VehicleLane[] trainLanes;
    
    [Header("Events")]
    public UnityEvent OnSignalGreen;
    public UnityEvent OnSignalRed;
    
    private float timer;

    private void Start()
    {
        ConnectTrainLanes();
        
        // Atualiza visuais baseado no estado inicial
        UpdateVisuals();
    }
    
    private void Update()
    {
        // Apenas o servidor controla a lógica de mudança automática
        if (isServer && autoChange)
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
        if (trainLanes == null || trainLanes.Length == 0)
        {
            trainLanes = FindObjectsOfType<VehicleLane>();
        }
        
        foreach (var lane in trainLanes)
        {
            if (lane != null)
            {
                OnSignalGreen.AddListener(lane.SetTrainSpeedGreen);
                OnSignalRed.AddListener(lane.SetTrainSpeedRed);
            }
        }
    }
    
    // Hook chamado quando o estado do sinal muda via SyncVar
    private void OnSignalStateChanged(bool oldState, bool newState)
    {
        UpdateVisuals();
        
        // Dispara eventos
        if (newState)
        {
            OnSignalGreen?.Invoke();
        }
        else
        {
            OnSignalRed?.Invoke();
        }
    }
    
    [Server]
    public void ToggleSignal()
    {
        isGreen = !isGreen;
    }
    
    [Server]
    public void SetSignalGreen()
    {
        isGreen = true;
    }
    
    [Server]
    public void SetSignalRed()
    {
        isGreen = false;
    }
    
    private void UpdateVisuals()
    {
        if (signalLight != null)
        {
            signalLight.color = isGreen ? Color.green : Color.red;
        }
        
        if (signalRenderer != null)
        {
            if (isGreen && greenMaterial != null)
                signalRenderer.material = greenMaterial;
            else if (!isGreen && redMaterial != null)
                signalRenderer.material = redMaterial;
        }
    }
    
    // Métodos para serem chamados por outros scripts (apenas server)
    [Server]
    public void OnAnimationGreen()
    {
        SetSignalGreen();
    }
    
    [Server]
    public void OnAnimationRed()
    {
        SetSignalRed();
    }
}