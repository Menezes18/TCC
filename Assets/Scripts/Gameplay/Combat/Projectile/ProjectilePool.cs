using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Fase 5 - Item 15: Pool de projéteis simples (server-side ownership of lifecycle)
public interface IProjectile
{
    void OnSpawnFromPool(Vector3 origin, Vector3 dir, NetworkIdentity owner, Database db);
    void OnRecycleToPool();
}

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Database db;
    [SerializeField] private int initialSize = 16;
    private readonly Queue<GameObject> _free = new();
    private readonly HashSet<GameObject> _inUse = new();
    private GameObject _prefabGO;

    public void Warm(Database database)
    {
        if (db == null) db = database;
        if (db == null || db.projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectilePool] Database ou prefab não configurados.");
            return;
        }
        _prefabGO = db.projectilePrefab.gameObject;
    }

    public GameObject Rent(Vector3 origin, Vector3 dir, NetworkIdentity owner)
    {
        if (db == null) Debug.LogWarning("[ProjectilePool] db null em Rent()");
        GameObject go = _free.Count > 0 ? _free.Dequeue() : Instantiate(_prefabGO);
        _inUse.Add(go);
        go.transform.position = origin;
        go.transform.rotation = Quaternion.LookRotation(dir);
        go.SetActive(true);
        var ni = go.GetComponent<NetworkIdentity>();
        if (ni != null && !ni.isServer)
        {
            // should only run on server; guard
        }
        if (ni != null && ni.netId == 0)
        {
            NetworkServer.Spawn(go);
        }
        var proj = go.GetComponent<ProjectileScript>();
        if (proj != null)
        {
            proj.Owner = owner.transform;
            proj.Initialize(origin, dir); // server init
        }
        return go;
    }

    public void Recycle(GameObject go)
    {
        if (go == null) return;
        if (!_inUse.Contains(go)) return;
        _inUse.Remove(go);
        var proj = go.GetComponent<ProjectileScript>();
        proj?.ResetForPool();
        // Des-spawn para poder reutilizar sem conflitos de observers
        var ni = go.GetComponent<NetworkIdentity>();
        if (ni != null && ni.netId != 0)
        {
            NetworkServer.UnSpawn(go);
        }
        go.SetActive(false);
        _free.Enqueue(go);
    }
}
