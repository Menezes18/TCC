using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryPlatformGame : MonoBehaviour
{
    [System.Serializable]
    public class Platform
    {
        public Transform platformTransform; 
        public MeshRenderer symbolDisplay;
        public int symbolIndex;
        public bool isCorrectPlatform = false;
    }

    [Header("Platform Settings")]
    public List<Platform> platforms = new List<Platform>();
    public string platformTag = "Platform"; 

    [Header("Game Materials")]
    public Material[] symbolMaterials;
    public Material platformDefaultMaterial;
    
    [Header("References")]
    public MeshRenderer bigScreen;
    
    [Header("Timing Settings")]
    public float showSymbolsDuration = 5f;
    public float timeToChoose = 10f;
    public float platformDisappearDelay = 1f;

    private int currentTargetSymbol;
    private bool gameActive = false;

    void Start()
    {
        FindAndRegisterAllPlatforms();
        
        StartCoroutine(GameRoutine());
    }

    void FindAndRegisterAllPlatforms()
    {
        GameObject[] platformObjects = GameObject.FindGameObjectsWithTag(platformTag);
        
        foreach (GameObject platformObj in platformObjects)
        {
            Platform newPlatform = new Platform();
            newPlatform.platformTransform = platformObj.transform;
            
            if (platformObj.transform.childCount > 0)
            {
                newPlatform.symbolDisplay = platformObj.transform.GetChild(0).GetComponent<MeshRenderer>();
            }
            else
            {
                newPlatform.symbolDisplay = platformObj.GetComponent<MeshRenderer>();
            }
            
            platforms.Add(newPlatform);
        }
    }

    IEnumerator GameRoutine()
    {
        AssignRandomSymbols();
        ShowAllSymbols();
        yield return new WaitForSeconds(showSymbolsDuration);
        HideAllSymbols();
        ChooseTargetSymbol();
        gameActive = true;
        yield return new WaitForSeconds(timeToChoose);
        gameActive = false;
        yield return new WaitForSeconds(platformDisappearDelay);
        DisableIncorrectPlatforms();
    }

    void AssignRandomSymbols()
    {
        int symbolsCount = symbolMaterials.Length;
        
        for (int i = 0; i < platforms.Count; i++)
        {
            platforms[i].symbolIndex = i % symbolsCount;
        }
        
        // Embaralha as plataformas
        ShufflePlatforms();
    }

    void ShufflePlatforms()
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            Platform temp = platforms[i];
            int randomIndex = Random.Range(i, platforms.Count);
            platforms[i] = platforms[randomIndex];
            platforms[randomIndex] = temp;
        }
    }

    void ShowAllSymbols()
    {
        foreach (Platform platform in platforms)
        {
            if (platform.symbolDisplay != null)
            {
                platform.symbolDisplay.material = symbolMaterials[platform.symbolIndex];
            }
        }
    }

    void HideAllSymbols()
    {
        foreach (Platform platform in platforms)
        {
            if (platform.symbolDisplay != null)
            {
                platform.symbolDisplay.material = platformDefaultMaterial;
            }
        }
    }

    void ChooseTargetSymbol()
    {
        currentTargetSymbol = Random.Range(0, symbolMaterials.Length);
        bigScreen.material = symbolMaterials[currentTargetSymbol];
        
        foreach (Platform platform in platforms)
        {
            platform.isCorrectPlatform = (platform.symbolIndex == currentTargetSymbol);
        }
    }

    void DisableIncorrectPlatforms()
    {
        foreach (Platform platform in platforms)
        {
            if (!platform.isCorrectPlatform && platform.platformTransform != null)
            {
                platform.platformTransform.gameObject.SetActive(false);
            }
        }
    }

    public bool IsPlayerOnCorrectPlatform(Transform platformTransform)
    {
        if (!gameActive) return false;
        
        foreach (Platform platform in platforms)
        {
            if (platform.platformTransform == platformTransform)
            {
                return platform.isCorrectPlatform;
            }
        }
        return false;
    }
}