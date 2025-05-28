using UnityEngine;

public class LaneAtrribuition : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private int vehicleCount = 5;
    [SerializeField] private Transform startPoint;
    [SerializeField] private bool useFirstChildAsStart = true;
    [SerializeField] private float spacing = 2f;
    
    [Header("Train Settings")]
    [SerializeField] private bool isTrainLane = false;
    // Removido signalController daqui - será conectado pelo TrainSignalController
    
    private VehicleLane vehicleLane;

    private void Awake()
    {
        vehicleLane = GetComponent<VehicleLane>();
        
        // Configurações de velocidade baseadas no tipo de veículo
        SetVehicleSpeed();
        
        // Trens começam com velocidade normal, mas serão controlados pelo sinal
        // O sinal que determinará quando parar/andar
    }
    
    private void SetVehicleSpeed()
    {
        if (vehiclePrefab == null) return;
        
        string prefabName = vehiclePrefab.name;
        
        switch (prefabName)
        {
            case "Elefante":
            case "Skatista":
            case "CarrinhoDeMao":
                vehicleLane.speed = 4f;
                break;
            case "Golirao":
            case "Bicicleta":
            case "CavaloMontado":
                vehicleLane.speed = 9f;
                break;
            case "Micos":
            case "BananaCar":
                vehicleLane.speed = 25f;
                break;
            case "Train":
                vehicleLane.speed = 120f;
                isTrainLane = true;
                break;
        }
    }
    
    private void SetupStartPoint()
    {
        if (useFirstChildAsStart && transform.childCount > 0)
        {
            startPoint = transform.GetChild(0);
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
    
    // Propriedade pública para verificar se é trilho de trem
    public bool IsTrainLane => isTrainLane;
}