using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum DamageType
{
    Push,
    Poop
}

public class PlayerActiveFrame : NetworkBehaviour{
    
    [SerializeField] Database db;

    [SerializeField] List<Collider> _affectedPlayer;
    
    private Transform _cam;

    private PlayerScript _playerScript;

    private void Start()
    {
        if (!this.isOwned) return;
        if (!isLocalPlayer) return;
        _cam = Camera.main.transform;
        
    }

    public void SphereFront()
    {
        if (!this.isOwned) return;
        // 1) Pega a direção que a câmera está olhando (incluindo vertical).
        Vector3 camForward = _cam.transform.forward.normalized;

        // 2) Origem da esfera: posição do player + direção da câmera multiplicada pelo raio de detecção.
        Vector3 sphereOrigin = transform.position + camForward * db.playerPushRadius;

        Collider[] orb = Physics.OverlapSphere(
            sphereOrigin,
            db.playerPushRadius,
            db.PlayerMask
        );
        if (orb.Length == 0) return;

        ApplyDamage(orb, DamageType.Push);
    }

    public void ClearActiveFrame() {_affectedPlayer.Clear();}
    
    public void ApplyDamage(Collider[] target, DamageType dmgType)
    {
        if (!this.isOwned) return;
        Vector3 origin = transform.position;
        origin.y = 0;
        
        foreach (Collider t in target){
            
            if (t.transform.root == transform) 
                continue;
            
            if(_affectedPlayer.Contains(t) == true) continue;

            IDamageable dmg = t.transform.GetComponent<IDamageable>();
            
            if (dmg == null) continue;

            Vector3 destination = t.transform.position;
            destination.y = 0;

            Vector3 final = (destination - origin).normalized;
            
            dmg.ReceiveDamage(DamageType.Push, final);

        }
    }


    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 camForward = _cam.transform.forward.normalized;
        Vector3 sphereOrigin = transform.position + camForward * db.playerPushRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(sphereOrigin, db.playerPushRadius);
    }
}
