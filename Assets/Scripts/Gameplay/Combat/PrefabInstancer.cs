using Mirror;
using UnityEngine;

public class PrefabInstancer : NetworkBehaviour
{
   public static PrefabInstancer singleton;

   private void Awake()
   {
      singleton = this;
   }

   [SerializeField] private Database db;

   [Command(requiresAuthority = false)]
   public void CmdSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
      if (db == null)
      {
         Debug.LogWarning("[PrefabInstancer] Database null no spawn");
         return;
      }
      InternalSpawnProjectile(origin, dir, ownerNetId);
   }

   [Server]
   private void InternalSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId)
   {
      Transform instance = Instantiate(db.projectilePrefab, origin, Quaternion.LookRotation(dir));
      var ps = instance.GetComponent<ProjectileScript>();
      ps.Owner = ownerNetId.transform;
      ps.Initialize(origin, dir);
      NetworkServer.Spawn(instance.gameObject);
      Debug.Log($"💥 [PROJECTILE] Spawned at {origin} dir={dir} owner={ownerNetId.netId}");
   }

   private void OnDestroy()
   {
      if (singleton == this) singleton = null;
   }
}
