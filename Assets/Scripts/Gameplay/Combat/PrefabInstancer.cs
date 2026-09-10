using System;
using System.Collections.Generic;
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
   private readonly Dictionary<NetworkIdentity, double> nextThrowTimes = new();
   
   [Command(requiresAuthority = false)]
   public void CmdSpawnProjectile(Vector3 origin, Vector3 dir, NetworkIdentity ownerNetId, NetworkConnectionToClient sender = null)
   {
      if (sender == null || sender.identity == null || ownerNetId != sender.identity || db == null)
         return;
      var player = sender.identity.GetComponent<PlayerScript>();
      if (player == null || player.IsDead || player.isFrozen || player.origin == null) return;
      if (float.IsNaN(dir.sqrMagnitude) || float.IsInfinity(dir.sqrMagnitude) || dir.sqrMagnitude < 0.0001f)
         return;
      if (nextThrowTimes.TryGetValue(ownerNetId, out double nextThrow) && NetworkTime.time < nextThrow)
         return;
      nextThrowTimes[ownerNetId] = NetworkTime.time + Mathf.Max(0.1f, db.playerThrowCooldown);
      InternalSpawnProjectile(player.origin.transform.position, dir.normalized, sender.identity);
      
   }

   public override void OnStopServer()
   {
      nextThrowTimes.Clear();
      base.OnStopServer();
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
      Debug.Log($"💥 [PROJECTILE] Spawned at {origin} dir={dir} owner={ownerNetId.netId}");
   }

}
