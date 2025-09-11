using UnityEngine;

public partial class PlayerScript {
	// Central damage registry types (scoped to Player assembly to avoid extra files until stable)
	public interface IDamageEffect
	{
		DamageType DamageType { get; }
		void Apply(PlayerContext ctx, Vector3 direction);
	}
	public class DamageRegistry
	{
		private readonly System.Collections.Generic.Dictionary<DamageType, IDamageEffect> _effects = new();
		public void Register(IDamageEffect effect){ if(effect==null) return; _effects[effect.DamageType] = effect; }
		public bool TryGet(DamageType type, out IDamageEffect effect) => _effects.TryGetValue(type, out effect);
	}
	public class BlindDamageEffect : IDamageEffect
	{
		public DamageType DamageType => DamageType.Poop;
		public void Apply(PlayerContext ctx, Vector3 direction)
		{
			ctx.Player.Status = PlayerStatus.Blinded;
			ctx.Player.ApplyBlind(ctx.Db.playerBlindDuration);
		}
	}
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

	private void Awake()
	{
		// Early registry setup so Network partial can use it.
		if (_damageRegistry == null)
		{
			_damageRegistry = new DamageRegistry();
			_damageRegistry.Register(new BlindDamageEffect());
			_damageRegistry.Register(new PushDamageEffect());
		}
	}
}
