using UnityEngine;
using UnityEngine.Events;

public class TrainSignalController : MonoBehaviour
{
    [Header("Signal Settings")]
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
    private float currentChangeInterval;

    private void Start()
    {
        ConnectTrainLanes();
        currentChangeInterval = changeIntervalInit;
        
        // Atualiza visuais baseado no estado inicial
        UpdateVisuals();
        
        // Dispara evento inicial
        if (isGreen)
        {
            OnSignalGreen?.Invoke();
        }
        else
        {
            OnSignalRed?.Invoke();
        }
    }
    
    private void Update()
    {
        if (autoChange)
        {
            timer += Time.deltaTime;
            if (timer >= currentChangeInterval)
            {
                ToggleSignal();
                timer = 0f;
                currentChangeInterval = Random.Range(changeIntervalInit, changeIntervalEnd);
            }
        }
    }
    
    private void ConnectTrainLanes()
    {
        if (trainLanes == null || trainLanes.Length == 0)
        {
            // Busca apenas lanes que são de trem
            var allLanes = FindObjectsOfType<VehicleLane>();
            var trainLanesList = new System.Collections.Generic.List<VehicleLane>();
            
            foreach (var lane in allLanes)
            {
                var attribution = lane.GetComponent<LaneAtrribuition>();
                if (attribution != null && attribution.IsTrainLane)
                {
                    trainLanesList.Add(lane);
                }
            }
            
            trainLanes = trainLanesList.ToArray();
        }
        
        // Conecta eventos apenas aos trilhos de trem
        foreach (var lane in trainLanes)
        {
            if (lane != null)
            {
                OnSignalGreen.AddListener(lane.SetTrainSpeedGreen);
                OnSignalRed.AddListener(lane.SetTrainSpeedRed);
                
                Debug.Log($"Conectado ao trilho: {lane.name}");
            }
        }
        
        Debug.Log($"Total de trilhos conectados: {trainLanes.Length}");
    }
    
    public void ToggleSignal()
    {
        isGreen = !isGreen;
        OnSignalStateChanged();
    }
    
    public void SetSignalGreen()
    {
        if (!isGreen)
        {
            isGreen = true;
            OnSignalStateChanged();
        }
    }
    
    public void SetSignalRed()
    {
        if (isGreen)
        {
            isGreen = false;
            OnSignalStateChanged();
        }
    }
    
    private void OnSignalStateChanged()
    {
        UpdateVisuals();
        
        Debug.Log($"Sinal mudou para: {(isGreen ? "VERDE" : "VERMELHO")}");
        
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
    
    public void OnAnimationGreen()
    {
        SetSignalGreen();
    }
    
    public void OnAnimationRed()
    {
        SetSignalRed();
    }
    
    public bool IsGreen => isGreen;
}