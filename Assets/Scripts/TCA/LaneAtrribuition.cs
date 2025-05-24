using UnityEngine;

public class LaneAtrribuition : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private int vehicleCount = 5;
    [SerializeField] private Transform startPoint;
    private VehicleLane vehicleLane;

    private void Awake()
    {
        vehicleLane = GetComponent<VehicleLane>();
        SpawnVehicles();
    }

    private void SpawnVehicles()
    {
        if (vehiclePrefab == null || vehicleLane == null) return;

        vehicleLane.vehicles.Clear();

        for (int i = 0; i < vehicleCount; i++)
        {
            Debug.Log(vehiclePrefab.name + " " + vehiclePrefab.transform.position);
            GameObject clone = Instantiate(vehiclePrefab, startPoint.position, Quaternion.identity, transform);
            Debug.Log(clone.name + " " + clone.transform.position);
            vehicleLane.vehicles.Add(clone.transform);
        }
    }
}
