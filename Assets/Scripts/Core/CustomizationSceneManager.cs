using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class CustomizationSceneManager : MonoBehaviour
{
    private static CustomizationSceneManager _instance;
    
    public static CustomizationSceneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CustomizationSceneManager");
                _instance = go.AddComponent<CustomizationSceneManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("🎬 [CustomizationSceneManager] Initialized");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎬 [CustomizationSceneManager] Scene loaded: {scene.name}");
        
        StartCoroutine(ApplyCustomizationAfterSceneLoad());
    }


    private IEnumerator ApplyCustomizationAfterSceneLoad()
    {
        yield return new WaitForSeconds(0.5f);
        
        ApplyCustomizationToAllLocalPlayers();
        
        yield return new WaitForSeconds(0.3f);
        ApplyCustomizationToAllLocalPlayers();
    }


    public void ApplyCustomizationToAllLocalPlayers()
    {
        if (CustomizationManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [CustomizationSceneManager] CustomizationManager not available");
            return;
        }

        var customization = CustomizationManager.Instance.GetCurrentCustomization();
        if (customization == null)
        {
            Debug.LogWarning("⚠️ [CustomizationSceneManager] No customization data available");
            return;
        }

        int applied = 0;
        
        var playerScripts = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var playerScript in playerScripts)
        {
            if (playerScript.isLocalPlayer)
            {
                playerScript.ApplyPlayerCustomization();
                applied++;
                Debug.Log($"✅ [CustomizationSceneManager] Applied to local PlayerScript");
            }
            else
            {
                var playerData = playerScript.GetComponent<PlayerData>();
                if (playerData != null)
                {
                    playerScript.ApplyRemoteCustomization(playerData.hatIndex, playerData.glassesIndex, playerData.shirtIndex);
                    applied++;
                    Debug.Log($"✅ [CustomizationSceneManager] Applied to remote PlayerScript from SyncVars");
                }
            }
        }
        
        var playerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        foreach (var playerData in playerDatas)
        {
            if (playerData.characterInstance != null)
            {
                var applier = playerData.characterInstance.GetComponentInChildren<CustomizationApplier>();
                if (applier != null)
                {
                    var customData = new PlayerCustomizationData("");
                    customData.hatIndex = playerData.hatIndex;
                    customData.glassesIndex = playerData.glassesIndex;
                    customData.shirtIndex = playerData.shirtIndex;
                    
                    applier.ApplyCustomization(customData);
                    applied++;
                    Debug.Log($"✅ [CustomizationSceneManager] Applied to PlayerData.characterInstance from SyncVars");
                }
            }
        }
        
        if (applied == 0)
        {
            var appliers = FindObjectsByType<CustomizationApplier>(FindObjectsSortMode.None);
            foreach (var applier in appliers)
            {
                var netBehaviour = applier.GetComponentInParent<Mirror.NetworkBehaviour>();
                if (netBehaviour == null) 
                {
                    applier.ApplyCustomization(customization);
                    applied++;
                    Debug.Log($"✅ [CustomizationSceneManager] Applied to standalone player (no network)");
                }
            }
        }

        if (applied > 0)
        {
            Debug.Log($"✅ [CustomizationSceneManager] Applied customization to {applied} player(s)");
        }
        else
        {
            Debug.Log("ℹ️ [CustomizationSceneManager] No players found to apply customization");
        }
    }


    public void ForceReapplyCustomization()
    {
        Debug.Log("🔄 [CustomizationSceneManager] Force reapplying customization");
        ApplyCustomizationToAllLocalPlayers();
    }
}
