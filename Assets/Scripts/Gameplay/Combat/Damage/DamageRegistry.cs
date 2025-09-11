using System.Collections.Generic;
using UnityEngine;

public class DamageRegistry
{
	private readonly Dictionary<DamageType, IDamageEffect> _effects = new();
	public void Register(IDamageEffect effect){ if(effect==null) return; _effects[effect.DamageType] = effect; }
	public bool TryGet(DamageType type, out IDamageEffect effect) => _effects.TryGetValue(type, out effect);
}
