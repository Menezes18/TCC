using System.Collections;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private CarData[] carTypes; 
    [SerializeField] private Transform[] spawnPoints; 
    [SerializeField] private float spawnInterval = 3f; 

    private void Start()
    {
        StartCoroutine(SpawnCars());
    }

    private IEnumerator SpawnCars()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);


            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            CarData selectedCar = carTypes[Random.Range(0, carTypes.Length)];
            
            GameObject newCar = Instantiate(selectedCar.carPrefab, spawnPoint.position, spawnPoint.rotation);
            
            CarController carController = newCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.SetCarData(selectedCar);
            }
        }
    }
}