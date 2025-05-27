using UnityEngine;

public class LaneAtrribuition : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private int vehicleCount = 5;
    [SerializeField] private Transform startPoint;
    [SerializeField] private bool useFirstChildAsStart = true;
    [SerializeField] private float spacing = 2f;
    
    [Header("Train Settings")]
    [SerializeField] private bool isTrainLane = false; // Marcar se é trilho de trem
    [SerializeField] private TrainSignalController signalController; // Referência ao controlador do sinal
    
    private VehicleLane vehicleLane;

    private void Awake()
    {
        vehicleLane = GetComponent<VehicleLane>();
        
        // Configurações de velocidade baseadas no tipo de veículo
        SetVehicleSpeed();
        
        // Se for trilho de trem, configura velocidade inicial como 0 (parado)
        if (isTrainLane)
        {
            vehicleLane.speed = 0f; // Trens começam parados
        }

        //SetupStartPoint();
        //SpawnVehicles();
        
        // Conecta ao controlador de sinal se for trilho de trem
        if (isTrainLane && signalController != null)
        {
            ConnectToSignalController();
        }
    }
    
    private void SetVehicleSpeed()
    {
        if (vehiclePrefab == null) return;
        
        string prefabName = vehiclePrefab.name;
        
        switch (prefabName)
        {
            case "Elefante":
            case "Skatista":
            case "CarrinhoDeMao" :
                vehicleLane.speed = 4f;
                break;
            case "Golirao":
            case "Bicicleta":
            case "CavaloMontado" :
                vehicleLane.speed = 9f;
                break;
            case "Micos":
            case "BananaCar":
                vehicleLane.speed = 25f;
                break;
            case "Train": // Adicione o nome do seu prefab de trem
                vehicleLane.speed = 20f; // Velocidade máxima do trem
                isTrainLane = true; // Marca automaticamente como trilho
                break;
        }
    }
    
    private void SetupStartPoint()
    {
        if (useFirstChildAsStart && transform.childCount > 0)
        {
            startPoint = transform.GetChild(0);
            Debug.Log($"StartPoint definido automaticamente: {startPoint.name}");
        }
        else if (transform.childCount == 0)
        {
            Debug.LogError("Nenhum filho encontrado para usar como startPoint!");
        }
    }

    private void SpawnVehicles()
    {
        if (vehiclePrefab == null || vehicleLane == null) return;

        vehicleLane.vehicles.Clear();

        for (int i = 0; i < vehicleCount; i++)
        {
            Vector3 spawnPosition = startPoint.position + new Vector3(0, 0, spacing * i);
            GameObject clone = Instantiate(vehiclePrefab, spawnPosition, transform.rotation, transform);
            clone.name = $"{vehiclePrefab.name}_Clone_{i}";
            vehicleLane.vehicles.Add(clone.transform);
        }
    }
    
    private void ConnectToSignalController()
    {
        if (signalController != null)
        {
            signalController.OnSignalGreen.AddListener(vehicleLane.SetTrainSpeedGreen);
            signalController.OnSignalRed.AddListener(vehicleLane.SetTrainSpeedRed);
        }
    }
}