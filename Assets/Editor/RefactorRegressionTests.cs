using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

public class RefactorRegressionTests
{
    private GameObject root;

    [SetUp]
    public void SetUp() => root = new GameObject("Refactor regression fixture");

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    [Test]
    public void RemoteResetRestoresDestroyedTileAndAllowsAnotherActivation()
    {
        root.transform.position = new Vector3(3, 4, 5);
        var tile = root.AddComponent<ChaoQuebrandoSimples>();
        var initial = Child("Initial");
        var cracked = Child("Cracked");
        Set(tile, "estadosChao", new[] { initial, cracked });
        Invoke(tile, "Awake");
        tile.AtivarTile();
        tile.AtualizarVisualizacaoRemota(1, true);
        Assert.That(root.activeSelf, Is.False);
        root.transform.position = Vector3.zero;

        tile.AtualizarVisualizacaoRemota(0, false);

        Assert.That(root.activeSelf, Is.True);
        Assert.That(root.transform.position, Is.EqualTo(new Vector3(3, 4, 5)));
        Assert.That(initial.activeSelf, Is.True);
        Assert.That(cracked.activeSelf, Is.False);
        Assert.That(tile.FoiPisado(), Is.False);
        tile.AtivarTile();
        Assert.That(tile.FoiPisado(), Is.True);
    }

    [Test]
    public void RemoteProgressSelectsExactlyOneTileState()
    {
        var tile = root.AddComponent<ChaoQuebrandoSimples>();
        var initial = Child("Initial");
        var cracked = Child("Cracked");
        Set(tile, "estadosChao", new[] { initial, cracked });
        Invoke(tile, "Awake");
        tile.AtualizarVisualizacaoRemota(1, false);
        Assert.That(initial.activeSelf, Is.False);
        Assert.That(cracked.activeSelf, Is.True);
        Assert.That(tile.FoiPisado(), Is.True);
    }

    [Test]
    public void FloorStateIsAllocatedBeforeNetworkStartEvenWithMissingTileReferences()
    {
        root.AddComponent<NetworkIdentity>();
        var manager = root.AddComponent<FloorBreakingManager>();
        var tile = Child("Tile").AddComponent<ChaoQuebrandoSimples>();
        Set(manager, "tiles", new ChaoQuebrandoSimples[] { null, tile });
        Invoke(manager, "Awake");
        Assert.That(Get<FloorBreakingManager.TileState[]>(manager, "tileStates").Length, Is.EqualTo(2));
        Assert.That(Get<int>(tile, "tileId"), Is.EqualTo(1));
        Assert.That(Get<FloorBreakingManager>(tile, "manager"), Is.SameAs(manager));
    }

    [Test]
    public void PhysicsDetectionGrowsPastItsInitialCapacity()
    {
        root.transform.position = new Vector3(10000, 10000, 10000);
        var tile = root.AddComponent<ChaoQuebrandoSimples>();
        for (int i = 0; i < 40; i++) Child("Collider " + i).AddComponent<BoxCollider>();
        Physics.SyncTransforms();
        int count = (int)Invoke(tile, "DetectColliders");
        Assert.That(count, Is.GreaterThanOrEqualTo(40));
        Assert.That(Get<Collider[]>(tile, "detectionBuffer").Length, Is.GreaterThan(count));
    }

    [Test]
    public void WaveConfigurationUpdatesValuesAlreadyCachedByAwake()
    {
        var bounce = root.AddComponent<VerticalBounce>();
        Invoke(bounce, "Awake");
        bounce.ConfigureWave(2f, 0f, Mathf.PI / 2f);
        Invoke(bounce, "Update");
        Assert.That(root.transform.localPosition.y, Is.EqualTo(2f).Within(0.0001f));
    }

    [Test]
    public void GlassLateJoinAppliesBrokenStateWithoutWaitingForAnRpc()
    {
        root.AddComponent<NetworkIdentity>();
        root.AddComponent<BoxCollider>();
        var tile = root.AddComponent<GlassTile>();
        var surface = Child("Surface");
        var renderer = surface.AddComponent<SkinnedMeshRenderer>();
        var collider = surface.AddComponent<BoxCollider>();
        Set(tile, "renderers", new[] { renderer });
        Set(tile, "colliders", new Collider[] { collider });
        Set(tile, "_isBroken", true);
        tile.OnStartClient();
        Assert.That(renderer.enabled, Is.False);
        Assert.That(collider.enabled, Is.False);
        Invoke(tile, "OnBrokenChanged", true, false);
        Assert.That(renderer.enabled, Is.True);
        Assert.That(collider.enabled, Is.True);
    }

    [Test]
    public void PushRangeUsesTargetColliderSurfaceInsteadOfItsCenter()
    {
        var database = ScriptableObject.CreateInstance<Database>();
        var target = new GameObject("Large push target");
        try
        {
            database.playerPushRadius = 1.3f;
            root.AddComponent<NetworkIdentity>();
            var activeFrame = root.AddComponent<PlayerActiveFrame>();
            Set(activeFrame, "db", database);

            target.transform.position = new Vector3(0f, 0f, 3f);
            var identity = target.AddComponent<NetworkIdentity>();
            target.AddComponent<SphereCollider>().radius = 1f;
            Physics.SyncTransforms();

            Assert.That((bool)Invoke(activeFrame, "ServerIsTargetInPushRange", identity), Is.True);

            target.transform.position = new Vector3(0f, 0f, 5f);
            Physics.SyncTransforms();
            Assert.That((bool)Invoke(activeFrame, "ServerIsTargetInPushRange", identity), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void LobbyReadyIsNotBlockedWhenNoBriefingIsVisible()
    {
        BriefingManager.singleton = null;
        root.AddComponent<NetworkIdentity>();
        var briefing = root.AddComponent<BriefingManager>();

        Assert.That(briefing.IsReadyInputBlocked, Is.False);

        Set(briefing, "_briefingVisibleClient", true);
        Assert.That(briefing.IsReadyInputBlocked, Is.True);

        Set(briefing, "readyInteractableClient", true);
        Assert.That(briefing.IsReadyInputBlocked, Is.False);
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static object Invoke(object target, string name, params object[] arguments) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
}
