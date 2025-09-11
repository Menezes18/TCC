using UnityEngine;

public class PushDamageEffect : IDamageEffect
{
	public DamageType DamageType => DamageType.Push;
	public void Apply(PlayerContext ctx, Vector3 direction)
	{
		ctx.Player.State = PlayerState.Stagger;
		Vector3 final = direction.normalized * ctx.Db.playerPushStrength;
		ctx.Player.ApplyPush(final, ctx.Db.playerStaggerHeight, ctx.Db.playerStaggerStunDuration);
	}
}
