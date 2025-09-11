using System;
using Mirror;
using UnityEngine;

public class PrefabInstancer : NetworkBehaviour
{
   #region Singleton Setup
   public static PrefabInstancer singleton;

   private void Awake()
   {
      singleton = this;
   }

   #endregion

   [SerializeField] Database db;
   [SerializeField] private ProjectilePool projectilePool; // Fase 5 pooling
   [SerializeField] private bool enableProjectilePooling = false;
   
   [Command(requiresAuthority = false)]
   public void CmdSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
      if (db == null) { Debug.LogWarning("[PrefabInstancer] Database null no spawn"); return; }
      if (enableProjectilePooling)
      {
         if (projectilePool == null)
         {
            projectilePool = FindFirstObjectByType<ProjectilePool>();
            if (projectilePool == null)
            {
               var go = new GameObject("__ProjectilePoolRuntime");
               projectilePool = go.AddComponent<ProjectilePool>();
               projectilePool.Warm(db);
               Debug.Log("[PrefabInstancer] Criado ProjectilePool runtime.");
            }
         }
         InternalSpawnProjectile(origin, dir, ownerNetId);
      }
      else
      {
         // caminho original sem pool
         Transform instance = Instantiate(db.projectilePrefab, origin, Quaternion.LookRotation(dir));
         var ps = instance.GetComponent<ProjectileScript>();
         ps.Owner = ownerNetId.transform;
         ps.Initialize(origin, dir);
         NetworkServer.Spawn(instance.gameObject);
         Debug.Log($"💥 [PROJECTILE][LEGACY] Spawned @ {origin} owner={ownerNetId.netId}");
      }
   }

   [Server]
   private void InternalSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
   if (!enableProjectilePooling || projectilePool == null) return;
   var go = projectilePool.Rent(origin, dir, ownerNetId);
   Debug.Log($"💥 [PROJECTILE][POOL] Rent @ {origin} owner={ownerNetId.netId}");
   }

   private void OnDestroy()
   {
      if (singleton == this) singleton = null;
   }

}
