using Mirror;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    [SerializeField] private CarData carData; 
    private Vector3 spawnPosition; 
    private void Update()
    {
        
            transform.Translate(Vector3.forward * carData.speed * Time.deltaTime);
    }
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
    }

    
    public void SetCarData(CarData data, Vector3 position)
    {
        carData = data;
        spawnPosition = position;
    }

    
}