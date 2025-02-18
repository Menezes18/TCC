using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarSpawner : NetworkBehaviour
{
    [SerializeField] private CarData[] carTypes;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int poolSize = 5;

    public Transform carSpawnPoint;
    private Queue<GameObject> carPool = new Queue<GameObject>();

    private static CarSpawner instance;
    
    private void Awake()
    {
        instance = this;
        
    }
    
    public override void OnStartServer()
    {
        InitializePool();
        StartCoroutine(SpawnCars());
    }

    private void InitializePool()
    {
        foreach (var carData in carTypes)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject car = Instantiate(carData.carPrefab);
                car.SetActive(false);
                carPool.Enqueue(car);
            }
        }
    }

    private IEnumerator SpawnCars()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnCar();
        }
    }

    private void SpawnCar()
    {
        if (carPool.Count == 0) return;

        GameObject car = carPool.Dequeue();
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        CarData selectedCar = carTypes[Random.Range(0, carTypes.Length)];

        car.transform.position = spawnPoint.position;
        car.transform.rotation = spawnPoint.rotation;
        car.SetActive(true);

        NetworkServer.Spawn(car);

        CarController carController = car.GetComponent<CarController>();
        if (carController != null)
        {
            carController.Initialize(selectedCar, this);
        }
    }

    public void ReturnCarToPool(GameObject car)
    {
        car.SetActive(false);
        carPool.Enqueue(car);
    }

    public static Vector3 GetSpawnPosition()
    {
        return instance != null && instance.carSpawnPoint != null ? instance.carSpawnPoint.position : Vector3.zero;
    }
}
