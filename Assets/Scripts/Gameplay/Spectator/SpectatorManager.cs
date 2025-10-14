using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance { get; private set; }

    public event Action<bool> OnLocalSpectatorStateChanged;
    public event Action<PlayerScript> OnLocalSpectatorTargetChanged;

    [SerializeField] private string overlaySceneName = "SpecOverlay";


    public PlayerScript CurrentTarget { get; private set; }

    public PlayerScript LocalSpectator { get; private set; }


    private bool overlayLoadingOrLoaded;

    // Lista de espectadores desativada (sem snapshot)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SpectatorManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SpectatorManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Sem atualização por frame necessária neste manager


    public void OnLocalSpectatorEnter(PlayerScript local)
    {
    
        LocalSpectator = local;
        OnLocalSpectatorStateChanged?.Invoke(true);
        EnsureOverlayLoaded();
    }

    public void OnLocalSpectatorExit(PlayerScript local)
    {
        if (LocalSpectator == local)
        {
            LocalSpectator = null;
            CurrentTarget = null;
            OnLocalSpectatorStateChanged?.Invoke(false);
            EnsureOverlayUnloaded();
        }
    }

    public void OnLocalSpectatorTargetChangedInternal(PlayerScript newTarget)
    {
        CurrentTarget = newTarget;
        OnLocalSpectatorTargetChanged?.Invoke(newTarget);
    }

    private void EnsureOverlayLoaded()
    {
        if (overlayLoadingOrLoaded) return;

        var scene = SceneManager.GetSceneByName(overlaySceneName);
        if (scene.isLoaded)
        {
            overlayLoadingOrLoaded = true;
            return;
        }

        overlayLoadingOrLoaded = true;
        SceneManager.LoadSceneAsync(overlaySceneName, LoadSceneMode.Additive).completed += _ =>
        {
           
        };
    }

    private void EnsureOverlayUnloaded()
    {
        var scene = SceneManager.GetSceneByName(overlaySceneName);
        if (!scene.isLoaded)
        {
            overlayLoadingOrLoaded = false;
            return;
        }

        SceneManager.UnloadSceneAsync(scene).completed += _ =>
        {
            overlayLoadingOrLoaded = false;
        };
    }


    private string SafeResolvePlayerName(PlayerScript ps)
    {
        if (ps == null) return "—";
        var data = ps.GetComponent<PlayerData>();
        if (data != null)
        {
            return string.IsNullOrEmpty(data.alias) ? data.playerInfo.username : data.alias;
        }
        return ps.name;
    }

}


