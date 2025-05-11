using UnityEngine;

public interface IDamageable{
    
    public void ReceiveDamage(DamageType dmgType, Vector3 dir);
}
