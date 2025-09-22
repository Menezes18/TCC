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


    public void SphereFront()
    {
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

            CmdRequestPush(identity, dmgType, final);

        }
    }
    
    [Command(requiresAuthority = false)]
    private void CmdRequestPush(NetworkIdentity identity, DamageType dmgType, Vector3 dir)
    {
        Debug.Log($"[Cliente] ApplyDamage chamado em {netId} para {identity} alvos");

        IDamageable damage = identity.GetComponent<IDamageable>();
        // Se acertar a bola de futebol, registrar o altimo toque
        var ball = identity.GetComponent<BallPhysics>();
        if (ball != null)
        {
            var attacker = transform.root.GetComponent<PlayerData>();
            if (attacker != null)
                ball.ServerRegisterTouch(attacker.playerInfo.steamId);
        }
        damage.ReceiveDamage(dmgType, dir);
        if (dmgType == DamageType.Push)
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
    }
}
