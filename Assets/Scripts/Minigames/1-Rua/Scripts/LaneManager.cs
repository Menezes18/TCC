using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class LaneManager : NetworkBehaviour
{
    [System.Serializable]
    public class LaneConfig
    {
        public GameObject[] vehiclePrefabs;
        public float minSpawnInterval = 1f;
        public float maxSpawnInterval = 3f;
        public float minVehicleSpeed = 3f;
        public float maxVehicleSpeed = 8f;
        public float minVehicleSpacing = 4f;
        public Lane laneReference;
    }

    [SerializeField] private LaneConfig[] laneConfigs;

    private void Start()
    {
        if (!isServer) return;
        InitializeLanes();
    }

    private void InitializeLanes()
    {
        for (int i = 0; i < laneConfigs.Length; i++)
        {
            if (laneConfigs[i].laneReference != null)
            {
                laneConfigs[i].laneReference.Initialize(laneConfigs[i]);
            }
            else
            {
                Debug.LogWarning($"Lane {i} não está configurada");
            }
        }
    }

    public bool CheckCollision(Bounds playerBounds)
    {
        foreach (var config in laneConfigs)
        {
            if (config.laneReference != null && config.laneReference.CheckCollision(playerBounds))
                return true;
        }
        return false;
    }

    public void SetAllLanesActive(bool active)
    {
        foreach (var config in laneConfigs)
        {
            if (config.laneReference != null)
            {
                config.laneReference.gameObject.SetActive(active);
            }
        }
    }

    public void ClearAllLanes()
    {
        foreach (var config in laneConfigs)
        {
            if (config.laneReference != null)
            {
                config.laneReference.ClearVehicles();
            }
        }
    }
}