using UnityEngine;
using Mirror;

/// <summary>
/// Editor helper to ensure SceneTransitionManager is properly registered with Mirror.
/// This script ensures the NetworkIdentity is properly configured.
/// </summary>
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public static class SceneTransitionSetup
{
    static SceneTransitionSetup()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
        {
            // Ensure SceneTransitionManager is created
            if (SceneTransitionManager.singleton == null)
            {
                Debug.Log("[SceneTransitionSetup] Creating SceneTransitionManager for this session");
                GameObject go = new GameObject("SceneTransitionManager");
                go.AddComponent<SceneTransitionManager>();
            }
        }
    }
}
#endif