using Mirror;
using UnityEngine;
using System.Collections;

public partial class PlayerScript : NetworkBehaviour
{
    [Server]
    public void ReceiveDamage(DamageType dmgType, Vector3 dir)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        TargetRpcReceiveDamage(coon, dmgType, dir);
    }

    [TargetRpc]
    public void TargetRpcReceiveDamage(NetworkConnection coon, DamageType dmgType, Vector3 dir)
    {
        if (dmgType == DamageType.Poop) {
            Status = PlayerStatus.Blinded;
            _blindTimer = db.playerBlindDuration;
            return;
        }
        State = PlayerState.Stagger;
        Debug.DrawRay(transform.position, dir * 5, Color.cyan, 5);
        Vector3 final = dir.normalized * db.playerPushStrength;
        _inertia = final;
        InertiaCap = final.magnitude;
        _move.y = db.playerStaggerHeight;
        _staggerTimer = db.playerStaggerStunDuration;
    }

    [Server]
    private IEnumerator ClearStagger(float delay)
    {
        yield return new WaitForSeconds(delay);
        isStaggered = false;
    }

    public void OnStaggerChanged(bool oldValue, bool newValue)
    {
        _staggerIndicator.gameObject.SetActive(newValue);
    }
}
