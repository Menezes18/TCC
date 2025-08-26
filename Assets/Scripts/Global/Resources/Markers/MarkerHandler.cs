using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

public class MarkerHandler : NetworkBehaviour
{
    public static MarkerHandler instance;

    [SerializeField] private MarkerDefinition[] allMarkerDefinitions;
    [SerializeField] private Transform markerContainer;

    private readonly Dictionary<uint, Marker> networkMarkers = new Dictionary<uint, Marker>();
    private readonly List<Marker> localMarkers = new List<Marker>();
    private readonly Dictionary<GameObject, Stack<GameObject>> worldPools = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly Dictionary<GameObject, Stack<GameObject>> localPools = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly List<Marker> _tmpMarkers = new List<Marker>(16);

    private void Awake()
    {
        instance = this;
    }

    public Marker SpawnMarker(byte markerID, Vector3 targetPos, Transform objToFollow)
    {
        if (objToFollow != null)
        {
            for (int i = 0; i < localMarkers.Count; i++)
            {
                var m = localMarkers[i];
                if (m != null && m.FollowTransform == objToFollow)
                    return m;
            }
        }

        var def = allMarkerDefinitions[markerID];
        var localObj = GetFromPool(localPools, def.markerLocalObj, markerContainer);
        GameObject worldObj = def.markerWorldObj != null
            ? GetFromPool(worldPools, def.markerWorldObj, null, targetPos, Quaternion.identity)
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
            if (marker.worldObject) ReturnToPool(worldPools, marker.worldObject);
            ReturnToPool(localPools, marker.gameObject);
            return;
        }

        // Senão tenta remover de network
        uint foundKey = 0;
        bool found = false;
        foreach (var kvp in networkMarkers)
        {
            if (kvp.Value == marker)
            {
                foundKey = kvp.Key;
                found = true;
                break;
            }
        }
        if (found)
        {
            networkMarkers.Remove(foundKey);
            if (marker.worldObject) ReturnToPool(worldPools, marker.worldObject);
            ReturnToPool(localPools, marker.gameObject);
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
        var localObj = GetFromPool(localPools, def.markerLocalObj, markerContainer);
        GameObject worldObj = def.markerWorldObj != null
            ? GetFromPool(worldPools, def.markerWorldObj, null, targetPos, Quaternion.identity)
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

        if (marker.worldObject) ReturnToPool(worldPools, marker.worldObject);
        ReturnToPool(localPools, marker.gameObject);
        networkMarkers.Remove(followNetId);
    }

    public List<Marker> GetAllNetworkMarkers(List<Marker> buffer = null)
    {
        var list = buffer ?? _tmpMarkers;
        list.Clear();
        foreach (var kv in networkMarkers)
        {
            if (kv.Value != null)
                list.Add(kv.Value);
        }
        return list;
    }

    private static GameObject GetFromPool(Dictionary<GameObject, Stack<GameObject>> pools, GameObject prefab, Transform parent, Vector3 position = default, Quaternion rotation = default)
    {
        if (!pools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<GameObject>();
            pools[prefab] = stack;
        }
        GameObject obj;
        if (stack.Count > 0)
        {
            obj = stack.Pop();
            obj.transform.SetParent(parent, false);
            if (parent == null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            obj.SetActive(true);
        }
        else
        {
            obj = Object.Instantiate(prefab, parent);
            if (parent == null)
                obj.transform.SetPositionAndRotation(position, rotation);
            var tag = obj.GetComponent<PooledRef>();
            if (tag == null) tag = obj.AddComponent<PooledRef>();
            tag.prefabKey = prefab;
        }
        return obj;
    }

    private static void ReturnToPool(Dictionary<GameObject, Stack<GameObject>> pools, GameObject obj)
    {
        if (obj == null) return;
        var tag = obj.GetComponent<PooledRef>();
        var key = tag != null ? tag.prefabKey : null;
        if (key == null)
        {
            // If we don't know the prefab, don't pool to avoid corrupting pools
            Object.Destroy(obj);
            return;
        }
        if (!pools.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pools[key] = stack;
        }
        obj.SetActive(false);
        obj.transform.SetParent(null, false);
        stack.Push(obj);
    }
}

// Helper component to keep prefab association for pooling
internal sealed class PooledRef : MonoBehaviour
{
    public GameObject prefabKey;
}
