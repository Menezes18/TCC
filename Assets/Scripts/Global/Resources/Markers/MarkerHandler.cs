using Mirror;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarkerHandler : NetworkBehaviour
{
    public static MarkerHandler instance;

    [SerializeField] private MarkerDefinition[] allMarkerDefinitions;
    [SerializeField] private Transform markerContainer;

    private Dictionary<uint, Marker> networkMarkers = new Dictionary<uint, Marker>();
    private List<Marker> localMarkers = new List<Marker>();

    private void Awake()
    {
        instance = this;
    }

    public Marker SpawnMarker(byte markerID, Vector3 targetPos, Transform objToFollow)
    {
        if (objToFollow != null && localMarkers.Any(m => m.FollowTransform == objToFollow))
            return localMarkers.First(m => m.FollowTransform == objToFollow);

        var def = allMarkerDefinitions[markerID];
        var localObj = Instantiate(def.markerLocalObj, markerContainer);
        GameObject worldObj = def.markerWorldObj != null
            ? Instantiate(def.markerWorldObj, targetPos, Quaternion.identity)
            : null;

        var marker = localObj.GetComponent<Marker>();
        marker.InitializeMarker(this, worldObj, targetPos, objToFollow);
        localMarkers.Add(marker);
        return marker;
    }

    public void RemoveMarker(Marker marker)
    {
        if (localMarkers.Remove(marker))
        {
            if (marker.worldObject) Destroy(marker.worldObject);
            Destroy(marker.gameObject);
            return;
        }

        // Senão tenta remover de network
        var kvp = networkMarkers.FirstOrDefault(x => x.Value == marker);
        if (kvp.Value != null)
        {
            networkMarkers.Remove(kvp.Key);
            if (marker.worldObject) Destroy(marker.worldObject);
            Destroy(marker.gameObject);
        }
    }

    [Server]
    public void SpawnMarkerServer(byte markerID, Vector3 targetPos, NetworkIdentity objToFollow)
    {
        RpcSpawnMarker(markerID, targetPos, objToFollow.netId);
    }

    [ClientRpc]
    private void RpcSpawnMarker(byte markerID, Vector3 targetPos, uint followNetId)
    {
        if (networkMarkers.ContainsKey(followNetId))
            return;

        if (!NetworkClient.spawned.TryGetValue(followNetId, out var identity))
            return;

        var followTransform = identity.transform;
        var def = allMarkerDefinitions[markerID];
        var localObj = Instantiate(def.markerLocalObj, markerContainer);
        GameObject worldObj = def.markerWorldObj != null
            ? Instantiate(def.markerWorldObj, targetPos, Quaternion.identity)
            : null;

        var marker = localObj.GetComponent<Marker>();
        marker.InitializeMarker(this, worldObj, targetPos, followTransform);
        networkMarkers[followNetId] = marker;
    }

    [Server]
    public void RemoveMarkerServer(NetworkIdentity objToFollow)
    {
        RpcRemoveMarker(objToFollow.netId);
    }

    [ClientRpc]
    private void RpcRemoveMarker(uint followNetId)
    {
        if (!networkMarkers.TryGetValue(followNetId, out var marker))
            return;

        if (marker.worldObject) Destroy(marker.worldObject);
        Destroy(marker.gameObject);
        networkMarkers.Remove(followNetId);
    }

    public List<Marker> GetAllNetworkMarkers()
    {
        return networkMarkers.Values.ToList();
    }
}
