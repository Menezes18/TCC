using Mirror;
using UnityEngine;
using System.Collections;

public partial class PlayerScript : NetworkBehaviour
{
    // ==== Fase 3 Item 9: Damage Effect Registry (OCP) ====
    private interface IDamageEffect
    {
        DamageType Type { get; }
        void Apply(PlayerScript p, Vector3 dir);
    }
    private class BlindEffect : IDamageEffect
    {
        public DamageType Type => DamageType.Poop; // reutiliza enum existente
        public void Apply(PlayerScript p, Vector3 dir)
        {
            p.Status = PlayerStatus.Blinded;
            p.ApplyBlind(p.db.playerBlindDuration);
        }
    }
    private class PushEffect : IDamageEffect
    {
        public DamageType Type => DamageType.Push;
        public void Apply(PlayerScript p, Vector3 dir)
        {
            p.State = PlayerState.Stagger;
            Vector3 final = dir.normalized * p.db.playerPushStrength;
            p.ApplyPush(final, p.db.playerStaggerHeight, p.db.playerStaggerStunDuration);
        }
    }
    private System.Collections.Generic.Dictionary<DamageType, IDamageEffect> _damageEffects;
    private void RegisterDamageEffects()
    {
        if (_damageEffects != null) return;
        _damageEffects = new System.Collections.Generic.Dictionary<DamageType, IDamageEffect>
        {
            { DamageType.Poop, new BlindEffect() },
            { DamageType.Push, new PushEffect() }
        };
    }
    // Called via Unity automatically if declared (ensure one Awake across partials)
    private void Awake()
    {
        RegisterDamageEffects();
    }
    // =====================================================
    [Server]
    public void ReceiveDamage(DamageType dmgType, Vector3 dir)
    {
        NetworkConnection coon = transform.GetComponent<NetworkIdentity>().connectionToClient;
        TargetRpcReceiveDamage(coon, dmgType, dir);
    }

    [TargetRpc]
    public void TargetRpcReceiveDamage(NetworkConnection coon, DamageType dmgType, Vector3 dir)
    {
        if (_damageEffects == null) RegisterDamageEffects();
        if (_damageEffects != null && _damageEffects.TryGetValue(dmgType, out var effect))
        {
            effect.Apply(this, dir);
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
