using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerHandler : MonoBehaviour
{
    public static MarkerHandler instance;

    [SerializeField] private MarkerDefinition[] allMarkerDefinitions;
    [SerializeField] private Transform markerContainer;
    
    List<Marker> markerInstances = new List<Marker>();
    public Camera cam;

    private void Awake()
    {
        instance = this;
    }
    
    

    public Marker SpawnMarker(byte markerID, Vector3 targetPos, Transform objToFollow)
    {
        MarkerDefinition markerDef = allMarkerDefinitions[markerID];


        Transform parent = markerContainer;

        GameObject localObj = Instantiate(markerDef.markerLocalObj, parent);
        GameObject worldObj = null;
        Marker marker = localObj.GetComponent<Marker>();

        if (markerDef.markerWorldObj)
        {
            worldObj = Instantiate(markerDef.markerWorldObj, targetPos, Quaternion.identity);
        }

        marker.InitializeMarker(this, worldObj, targetPos, objToFollow);

        AddMarker(marker);

        return marker;
    }


    public void AddMarker(Marker marker)
    {
        markerInstances.Add(marker);
    }

    public void RemoveMarker(Marker marker)
    {
        markerInstances.Remove(marker);

        if (marker.worldObject)
            Destroy(marker.worldObject);

        Destroy(marker.gameObject);
    }

}
