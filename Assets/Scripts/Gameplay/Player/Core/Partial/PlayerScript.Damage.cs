using UnityEngine;

public partial class PlayerScript {
	private void Awake()
	{
		if (_damageRegistry == null)
		{
			_damageRegistry = new DamageRegistry();
			_damageRegistry.Register(new BlindDamageEffect());
			_damageRegistry.Register(new PushDamageEffect());
		}
	}
}
