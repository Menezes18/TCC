using Mirror;
using UnityEngine;
using System.Collections;

public partial class PlayerScript : NetworkBehaviour
{
    // Damage handling now delegates to shared DamageRegistry (Fase 3 Item 9)
    [Server]
    public void ReceiveDamage(DamageType dmgType, Vector3 dir)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        TargetRpcReceiveDamage(coon, dmgType, dir);
    }

    [TargetRpc]
    public void TargetRpcReceiveDamage(NetworkConnection coon, DamageType dmgType, Vector3 dir)
    {
        if (_damageRegistry == null)
            return; // should be initialized in Awake of damage partial
        if (_context == null)
            _context = new PlayerContext(this, _cooldowns, db, _animator, _networkAnimator, _cam);
        if (_damageRegistry.TryGet(dmgType, out var effect))
        {
            effect.Apply(_context, dir);
        }
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
