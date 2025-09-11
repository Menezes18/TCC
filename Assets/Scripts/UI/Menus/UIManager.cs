using UnityEngine;
using Mirror;

using System;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [Obsolete("Use UIManager.Instance em vez de UIManager.stance (mantido temporariamente para compatibilidade).", false)]
    public static UIManager stance => Instance; // alias legado
    [SerializeField] private GameObject uiPrefab;
    public GameObject LocalUI { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // (opcional) DontDestroyOnLoad(gameObject); // comentar até decidir ciclo de vida
    }
    
    public void SpwnLocalUI()
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