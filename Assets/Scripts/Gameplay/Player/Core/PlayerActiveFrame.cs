using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum DamageType
{
    Push,
    Poop
}


public class PlayerActiveFrame : NetworkBehaviour
{

    [SerializeField] Database db;

    [SerializeField] List<Collider> _affectedPlayer;

    private readonly HashSet<uint> _serverVictimsThisAttack = new HashSet<uint>();
    private double _serverAttackWindowUntil;
    private double _serverNextAttackTime;


    public void SphereFront()
    {

        if (!isOwned)
            return;

        Collider[] orb = Physics.OverlapSphere(transform.position + transform.forward,
            db.playerPushRadius, db.PlayerMask);
        if (orb.Length == 0) return;

        ApplyDamage(orb, DamageType.Push);
    }

    public void ClearActiveFrame() { _affectedPlayer.Clear(); }

    public void ApplyDamage(Collider[] target, DamageType dmgType)
    {
        Vector3 origin = transform.position;
        origin.y = 0;
        
        foreach (Collider t in target){
            
            if (t.transform.root == transform) 
                continue;
            
            if(_affectedPlayer.Contains(t) == true) continue;
            var identity = t.transform.root.GetComponent<NetworkIdentity>();
            if (identity == null) continue;
            IDamageable dmg = t.transform.GetComponent<IDamageable>();
            
            if (dmg == null) continue;

            Vector3 destination = t.transform.position;
            destination.y = 0;

            Vector3 final = (destination - origin).normalized;

            CmdRequestPush(identity);

        }
    }
    

    [Command]
    private void CmdRequestPush(NetworkIdentity identity)
    {
        if (identity == null || db == null || !NetworkServer.spawned.TryGetValue(identity.netId, out var serverIdentity) || serverIdentity != identity)
            return;

        var attackerScript = transform.root.GetComponent<PlayerScript>();
        if (attackerScript == null || !attackerScript.ServerCanPush)
            return;

        double now = NetworkTime.time;
        if (now >= _serverNextAttackTime)
        {
            _serverVictimsThisAttack.Clear();
            _serverAttackWindowUntil = now + 0.25d;
            _serverNextAttackTime = now + Mathf.Max(0.05f, db.playerPushCooldownTimer);
        }
        else if (now > _serverAttackWindowUntil)
        {
            return;
        }

        if (!ServerIsTargetInPushRange(identity))
            return;

        if (!_serverVictimsThisAttack.Add(identity.netId))
            return;

        Vector3 delta = identity.transform.position - transform.root.position;
        delta.y = 0f;
        if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
            return;

        Vector3 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : transform.root.forward;
        dir.y = 0f;

        Debug.Log($"[Server] Validated push from {connectionToClient?.identity?.netId} -> target {identity.netId}");

        IDamageable damage = identity.GetComponent<IDamageable>();
        if (damage == null)
            return;

        var ball = identity.GetComponent<BallPhysics>();
        if (ball != null)
        {
            var attacker = transform.root.GetComponent<PlayerData>();
            if (attacker != null)
                ball.ServerRegisterTouch(attacker.playerInfo.steamId);
        }
        if (identity.GetComponent<PlayerScript>() != null)
        {
            var controller = FindObjectOfType<BatataQuenteMinigameController>();
            if (controller != null)
            {
                var attacker = transform.root.GetComponent<PlayerData>();
                var target = identity.GetComponent<PlayerData>();
                if (attacker != null && target != null)
                    controller.OnPlayerPush(attacker, target);
            }
        }
        damage.ReceiveDamage(DamageType.Push, dir);
    }

    private bool ServerIsTargetInPushRange(NetworkIdentity identity)
    {
        Vector3 attackerPosition = transform.root.position;
        Vector3 attackerForward = transform.root.forward;
        attackerForward.y = 0f;
        if (attackerForward.sqrMagnitude < 0.0001f)
            attackerForward = Vector3.forward;
        else
            attackerForward.Normalize();

        Vector3 attackCenter = attackerPosition + attackerForward;
        float maxDistance = Mathf.Max(0.1f, db.playerPushRadius) + 0.75f;
        float maxDistanceSqr = maxDistance * maxDistance;
        Collider[] targetColliders = identity.GetComponentsInChildren<Collider>();

        for (int i = 0; i < targetColliders.Length; i++)
        {
            Collider targetCollider = targetColliders[i];
            if (targetCollider == null || !targetCollider.enabled)
                continue;

            Vector3 closestPoint = targetCollider.ClosestPoint(attackCenter);
            Vector3 offset = closestPoint - attackCenter;
            offset.y = 0f;
            if (float.IsFinite(offset.x) && float.IsFinite(offset.z) && offset.sqrMagnitude <= maxDistanceSqr)
                return true;
        }

        return false;
    }
}
