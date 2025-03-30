using UnityEngine;
using System.Collections.Generic;

public class Parenting : MonoBehaviour
{
     public List<GameObject> objectsToReparent;
    
    void Start()
    {
        CreateParents();
    }
    
    public void CreateParents()
    {
        if (objectsToReparent == null || objectsToReparent.Count == 0)
        {
            Debug.LogWarning("No objects assigned!");
            return;
        }
        
        foreach (GameObject obj in objectsToReparent)
        {
            if (obj != null)
            {
                GameObject parent = new GameObject(obj.name);
                parent.transform.position = obj.transform.position;
                obj.transform.SetParent(parent.transform);
            }
        }
        
        Debug.Log("Parents created successfully!");
    }
}
