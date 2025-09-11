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
    private readonly Queue<Transform> _free = new();
    private readonly HashSet<Transform> _inUse = new();

    public void Warm(Database database)
    {
        if (db == null) db = database;
        if (db == null || db.projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectilePool] Database ou prefab não configurados.");
            return;
        }
        for (int i = _free.Count + _inUse.Count; i < initialSize; i++)
        {
            var t = Instantiate(db.projectilePrefab);
            t.gameObject.SetActive(false);
            NetworkServer.Spawn(t.gameObject); // manter compat
            _free.Enqueue(t);
        }
    }

    public GameObject Rent(Vector3 origin, Vector3 dir, NetworkIdentity owner)
    {
        if (db == null) Debug.LogWarning("[ProjectilePool] db null em Rent()");
        Transform t = _free.Count > 0 ? _free.Dequeue() : Instantiate(db.projectilePrefab);
        _inUse.Add(t);
        t.position = origin;
        t.rotation = Quaternion.LookRotation(dir);
        t.gameObject.SetActive(true);
        var proj = t.GetComponent<ProjectileScript>();
        if (proj != null)
        {
            proj.Owner = owner.transform;
            proj.Initialize(origin, dir); // server init
        }
        return t.gameObject;
    }

    public void Recycle(GameObject go)
    {
        if (go == null) return;
        var t = go.transform;
        if (!_inUse.Contains(t)) return;
        _inUse.Remove(t);
        var proj = go.GetComponent<ProjectileScript>();
        proj?.ResetForPool();
        go.SetActive(false);
        _free.Enqueue(t);
    }
}
