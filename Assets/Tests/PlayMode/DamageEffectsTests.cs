#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public class DamageEffectsTests
{
    private PlayerScript _player;
    private Database _db;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("PlayerTest");
        _player = go.AddComponent<PlayerScript>();
        _db = ScriptableObject.CreateInstance<Database>();
        typeof(PlayerScript).GetField("db", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.SetValue(_player, _db);
        _player.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
    }

    [Test]
    public void BlindEffect_SetsStatusBlinded()
    {
        var registryField = typeof(PlayerScript).GetField("_damageRegistry", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var registry = registryField.GetValue(_player) as DamageRegistry;
        Assert.IsNotNull(registry, "Registry null");
        Assert.IsTrue(registry.TryGet(DamageType.Poop, out var effect));
        effect.Apply(new PlayerContext(_player, new PlayerCooldowns(), _db, null, null, null), Vector3.forward);
        Assert.AreEqual(PlayerStatus.Blinded, _player.Status);
    }

    [Test]
    public void PushEffect_ChangesStateToStagger()
    {
        var registryField = typeof(PlayerScript).GetField("_damageRegistry", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var registry = registryField.GetValue(_player) as DamageRegistry;
        Assert.IsTrue(registry.TryGet(DamageType.Push, out var effect));
        effect.Apply(new PlayerContext(_player, new PlayerCooldowns(), _db, null, null, null), Vector3.forward);
        Assert.AreEqual(PlayerState.Stagger, _player.State);
    }
}
#endif
