using System.Collections;
using UnityEngine;
using Mirror;

public class CarSpawner : NetworkBehaviour
{
    [SerializeField] private CarData[] carTypes;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 3f;

    public Transform carSpawnPoint;
    public override void OnStartServer()
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

            // Instancia o carro apenas no host
            GameObject newCar = Instantiate(selectedCar.carPrefab, spawnPoint.position, spawnPoint.rotation);

            // Sincroniza o carro com todos os jogadores
            NetworkServer.Spawn(newCar);

            CarController carController = newCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.SetCarData(selectedCar, carSpawnPoint.transform.position);
            }
        }
    }
}