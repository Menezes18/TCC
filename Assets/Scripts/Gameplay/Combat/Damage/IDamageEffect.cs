using UnityEngine;

public interface IDamageEffect
{
	DamageType DamageType { get; }
	void Apply(PlayerContext ctx, Vector3 direction);
}
