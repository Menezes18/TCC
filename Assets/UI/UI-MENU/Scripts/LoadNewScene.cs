using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

public class LoadNewScene : MonoBehaviour
{

    [HideInInspector] public GameObject LoadingPanel;
    [HideInInspector] public Image LoadingBar;
    
    public void LoadANewScene(string scene)
    {
        // If we are the server (host or dedicated), change scene for everyone via Mirror
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(scene);
            return;
        }
        
        // Otherwise, local load with the centralized LoadingScreenUI
        LoadingScreenUI.Instance?.Show(scene);
    }
}
