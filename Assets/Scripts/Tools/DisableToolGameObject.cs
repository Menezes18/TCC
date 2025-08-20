using UnityEngine;

public class DisableToolGameObject : MonoBehaviour
{
    public GameObject[] gameObjectsToDisable;

    public void DisableGameObjects()
    {
        foreach (var gd in gameObjectsToDisable)
        {
            gd.SetActive(false);
        }
    }
    
}
