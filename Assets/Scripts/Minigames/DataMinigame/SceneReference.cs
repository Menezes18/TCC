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

    public string SceneName => sceneName;

    public string ScenePathOrName => string.IsNullOrWhiteSpace(scenePath) ? sceneName : scenePath;

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

    public void Validate()
    {
#if UNITY_EDITOR
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

        if (sceneAsset == null && string.IsNullOrEmpty(sceneName))
        {
            scenePath = string.Empty;
        }
    }
#endif
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
