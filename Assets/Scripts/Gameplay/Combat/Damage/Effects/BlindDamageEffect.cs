using UnityEngine;

public class BlindDamageEffect : IDamageEffect
{
	public DamageType DamageType => DamageType.Poop;
	public void Apply(PlayerContext ctx, Vector3 direction)
	{
		ctx.Player.Status = PlayerStatus.Blinded;
		ctx.Player.ApplyBlind(ctx.Db.playerBlindDuration);
	}
}
