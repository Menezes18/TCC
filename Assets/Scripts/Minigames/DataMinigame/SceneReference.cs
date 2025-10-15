using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SceneReference
{
    [SerializeField]
    private string sceneName;

    [SerializeField]
    private string scenePath;

#if UNITY_EDITOR
    [SerializeField]
    private SceneAsset sceneAsset;
#endif

    /// <summary>
    /// Gets the scene name as registered in Build Settings.
    /// </summary>
    public string SceneName => sceneName;

    /// <summary>
    /// Gets the stored asset path. Falls back to the scene name when empty.
    /// </summary>
    public string ScenePathOrName => string.IsNullOrWhiteSpace(scenePath) ? sceneName : scenePath;

    /// <summary>
    /// Indicates whether this reference points to a valid scene.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(ScenePathOrName);

#if UNITY_EDITOR
    public SceneAsset SceneAsset
    {
        get => sceneAsset;
        set
        {
            if (sceneAsset == value)
                return;

            sceneAsset = value;
            SyncFromAsset();
        }
    }
#endif

    /// <summary>
    /// Ensures cached data is up to date (invoked automatically in the editor).
    /// </summary>
#if UNITY_EDITOR
    public void Validate()
    {
        if (sceneAsset != null)
        {
            SyncFromAsset();
        }
        else if (!string.IsNullOrEmpty(scenePath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (asset != null)
            {
                sceneAsset = asset;
                sceneName = asset.name;
            }
        }
#endif
#if UNITY_EDITOR
        if (sceneAsset == null && string.IsNullOrEmpty(sceneName))
#else
        if (string.IsNullOrEmpty(sceneName))
#endif
        {
            scenePath = string.Empty;
        }
    }

    private void SyncFromAsset()
    {
#if UNITY_EDITOR
        if (sceneAsset == null)
        {
            sceneName = string.Empty;
            scenePath = string.Empty;
            return;
        }

        scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        sceneName = sceneAsset.name;
#endif
    }
}
