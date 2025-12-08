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

    // Removido estado complexo, usando abordagem simples igual ResultsOverlay
    private bool _isLoading = false;

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
        StartCoroutine(LoadOverlayRoutine());
    }

    public void OnLocalSpectatorExit(PlayerScript local)
    {
        if (LocalSpectator == local)
        {
            LocalSpectator = null;
            CurrentTarget = null;
            OnLocalSpectatorStateChanged?.Invoke(false);
            // Não descarregamos mais manualmente, confiamos na troca de cena
            // EnsureOverlayUnloaded();
        }
    }

    public void OnLocalSpectatorTargetChangedInternal(PlayerScript newTarget)
    {
        CurrentTarget = newTarget;
        OnLocalSpectatorTargetChanged?.Invoke(newTarget);
    }

    private System.Collections.IEnumerator LoadOverlayRoutine()
    {
        if (_isLoading) yield break;

        var scene = SceneManager.GetSceneByName(overlaySceneName);
        if (!scene.isLoaded)
        {
            _isLoading = true;
            Debug.Log($"👁️ [SpectatorManager] Carregando overlay '{overlaySceneName}'...");
            
            var op = SceneManager.LoadSceneAsync(overlaySceneName, LoadSceneMode.Additive);
            if (op != null)
            {
                while (!op.isDone) yield return null;
            }
            
            _isLoading = false;
            Debug.Log($"✅ [SpectatorManager] Overlay carregado.");
        }
        else
        {
            Debug.Log($"👁️ [SpectatorManager] Overlay já estava carregado.");
        }
    }

    // Métodos antigos removidos para simplificação
    /*
    private void EnsureOverlayLoaded() { ... }
    private void EnsureOverlayUnloaded() { ... }
    */


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


