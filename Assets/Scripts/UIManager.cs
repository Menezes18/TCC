using UnityEngine;
using Mirror;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject uiPrefab;
    public GameObject LocalUI { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

    }
    
    public void SpawnLocalUI()
    {
        if (LocalUI != null) return;

        LocalUI = Instantiate(uiPrefab);
        var canvas = LocalUI.GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera =
                NetworkClient.connection.identity
                    .GetComponentInChildren<Camera>();
        }
    }
}