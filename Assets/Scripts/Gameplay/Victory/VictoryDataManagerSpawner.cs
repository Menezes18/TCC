using Mirror;
using UnityEngine;


public class VictoryDataManagerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject victoryDataManagerPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    
    private void Awake()
    {
        if (VictoryDataManager.Instance != null)
        {
            if (debugLogs)
                Debug.Log("✅ [VictoryDataManagerSpawner] VictoryDataManager já existe - não criando nova instância");
            return;
        }
        
        if (victoryDataManagerPrefab == null)
        {
            Debug.LogError("❌ [VictoryDataManagerSpawner] victoryDataManagerPrefab não configurado! Configure no Inspector.");
            return;
        }
        
        if (debugLogs)
            Debug.Log("🔧 [VictoryDataManagerSpawner] Criando VictoryDataManager...");
        
        GameObject instance = Instantiate(victoryDataManagerPrefab);
        instance.name = "VictoryDataManager"; 
        

        if (NetworkServer.active)
        {
            var netIdentity = instance.GetComponent<NetworkIdentity>();
            if (netIdentity != null)
            {
                NetworkServer.Spawn(instance);
                
                if (debugLogs)
                    Debug.Log("✅ [VictoryDataManagerSpawner] VictoryDataManager criado e spawnado na rede");
            }
            else
            {
                Debug.LogError("❌ [VictoryDataManagerSpawner] VictoryDataManager prefab NÃO tem NetworkIdentity!");
            }
        }
        else
        {
            if (debugLogs)
                Debug.Log("✅ [VictoryDataManagerSpawner] VictoryDataManager criado (cliente)");
        }
    }
    

}

