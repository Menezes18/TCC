using UnityEngine;
using Mirror;
using System.Collections;



public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject uiPrefab;
    public GameObject _localUI;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(this); }
        else Destroy(gameObject);

    }
    
    public void SpawnLocalUI()
    {
        Debug.Log("Spawning local UI");
        SpawnWhenReady();
        
    }

    private void SpawnWhenReady()
    {

        _localUI = Instantiate(uiPrefab);
        var canvas = _localUI.GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            canvas.worldCamera =
                NetworkClient.connection.identity
                    .GetComponentInChildren<Camera>();
    }
}