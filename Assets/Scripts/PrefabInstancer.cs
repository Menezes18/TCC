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
   
   [Command(requiresAuthority = false)]
   public void CmdSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
      InternalSpawnProjectile(origin, dir, ownerNetId);
      
   }

   [Server]
   private void InternalSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
      Transform instance = Instantiate(
         db.projectilePrefab,
         origin,
         Quaternion.LookRotation(dir)
      );

      var ps = instance.GetComponent<ProjectileScript>();
      ps.Owner = ownerNetId.transform;

      ps.Initialize(origin, dir);

      NetworkServer.Spawn(instance.gameObject);
   }

}
