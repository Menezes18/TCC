using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Smooth
{
    [InitializeOnLoad]
    public static class SmoothControllerMirrorEditor
    {
        static SmoothControllerMirrorEditor()
        {
            EditorSceneManager.sceneOpened += SceneOpenedCallback;
        }

        static void SceneOpenedCallback(Scene scene, OpenSceneMode mode)
        {
            var netMan = Object.FindAnyObjectByType<NetworkManager>();
            if (netMan)
            {
                if (!netMan.GetComponent<SmoothControllerMirror>())
                {
                    netMan.gameObject.AddComponent<SmoothControllerMirror>();
                }
            }
        }
    }
}