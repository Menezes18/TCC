using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : NetworkBehaviour
{
    private CarData carData;
    private CarSpawner spawner;

    public void Initialize(CarData data, CarSpawner spawnerReference)
    {
        carData = data;
        spawner = spawnerReference;
    }

    private void Update()
    {
        if (isServer && carData != null)
        {
            transform.Translate(Vector3.forward * carData.speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.RespawnAt(CarSpawner.GetSpawnPosition());
            }

            spawner.ReturnCarToPool(gameObject);
        }
    }
    public void ReturnToPool()
    {
        if (spawner != null)
        {
            spawner.ReturnCarToPool(gameObject);
        }
        else
        {
            Debug.LogError("Spawner não está definido no CarController!");
        }
    }

}