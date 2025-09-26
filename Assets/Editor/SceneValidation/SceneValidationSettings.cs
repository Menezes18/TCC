#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static class SceneValidationSettings
{
    const string ConfigAssetPath = "Assets/Editor/SceneValidation/SceneValidationConfig.asset";
    static SceneValidationConfig _cachedConfig;

    internal static SceneValidationConfig Config
    {
        get
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = AssetDatabase.LoadAssetAtPath<SceneValidationConfig>(ConfigAssetPath);
                if (_cachedConfig == null)
                {
                    _cachedConfig = ScriptableObject.CreateInstance<SceneValidationConfig>();
                    SceneValidationDefaults.PopulateDefaults(_cachedConfig);
                    AssetDatabase.CreateAsset(_cachedConfig, ConfigAssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log("[SceneValidation] Created default config at " + ConfigAssetPath);
                }
            }

            return _cachedConfig;
        }
    }

    internal static void ResetToDefaults()
    {
        var config = Config;
        if (config == null)
        {
            return;
        }

        SceneValidationDefaults.PopulateDefaults(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }
}

static class SceneValidationDefaults
{
    public static void PopulateDefaults(SceneValidationConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        config.ruleSets = BuildDefaultRuleSets().Select(CloneRuleSet).ToList();
    }

    static RequiredComponentEntry CreateComponentEntry(Type type, string label, string restrictToObjectName = "")
    {
        return new RequiredComponentEntry
        {
            label = label,
            componentTypeName = type.AssemblyQualifiedName,
            restrictToObjectName = restrictToObjectName
        };
    }

    static RequiredObjectEntry CreateObjectEntry(string name, SceneMatchMode mode = SceneMatchMode.Exact, bool mustBeActive = true)
    {
        return new RequiredObjectEntry
        {
            objectName = name,
            matchMode = mode,
            mustBeActive = mustBeActive
        };
    }

    static IEnumerable<SceneRuleSet> BuildDefaultRuleSets()
    {
        yield return new SceneRuleSet
        {
            displayName = "Offline",
            matchMode = SceneMatchMode.Exact,
            pattern = "Offline",
            requiredComponents = new List<RequiredComponentEntry>
            {
                CreateComponentEntry(typeof(MyNetworkManager), "MyNetworkManager (network bootstrap)"),
                CreateComponentEntry(typeof(SteamLobby), "SteamLobby (matchmaking wrapper)"),
                CreateComponentEntry(typeof(ActionFrameCamera), "ActionFrameCamera")
            },
        };

        yield return new SceneRuleSet
        {
            displayName = "RASCUNHO hub",
            matchMode = SceneMatchMode.Exact,
            pattern = "RASCUNHO",
            requiredComponents = new List<RequiredComponentEntry>
            {
                CreateComponentEntry(typeof(MatchManager), "MatchManager (round coordinator)"),
                CreateComponentEntry(typeof(BriefingManager), "BriefingManager (ready gate)"),
                CreateComponentEntry(typeof(ScoreboardUI), "ScoreboardUI (hub results)"),
                CreateComponentEntry(typeof(ActionFrameCamera), "ActionFrameCamera")
            },
        };

        yield return new SceneRuleSet
        {
            displayName = "Victory",
            matchMode = SceneMatchMode.Exact,
            pattern = "Vitoria",
            requiredComponents = new List<RequiredComponentEntry>
            {
                CreateComponentEntry(typeof(ScoreboardUI), "ScoreboardUI (victory results)"),
                CreateComponentEntry(typeof(ActionFrameCamera), "ActionFrameCamera")
            },
        };

        yield return new SceneRuleSet
        {
            displayName = "Generic Minigame",
            matchMode = SceneMatchMode.Prefix,
            pattern = "MN_",
            requiredComponents = new List<RequiredComponentEntry>
            {
                CreateComponentEntry(typeof(MatchManager), "MatchManager (minigame flow)"),
                CreateComponentEntry(typeof(BriefingManager), "BriefingManager (minigame ready gate)"),
                CreateComponentEntry(typeof(MinigameController), "MinigameController (game logic)"),
                CreateComponentEntry(typeof(ActionFrameCamera), "ActionFrameCamera")
            },
        };

        yield return CreateExactMinigameRule("MN_Fut", typeof(SoccerMinigameController));
        yield return CreateExactMinigameRule("MN_BatataQ", typeof(BatataQuenteMinigameController));
        yield return CreateExactMinigameRule("MN_Corrida", typeof(RaceMinigameController));
        yield return CreateExactMinigameRule("MN_Run", typeof(RaceMinigameController));
        yield return CreateExactMinigameRule("MN_Barco", typeof(RaceMinigameController));
        yield return CreateExactMinigameRule("MN_Memoria", typeof(MemoriaMinigameController));
        yield return CreateExactMinigameRule("MN_new_Rua", typeof(StreetMinigameController));
        yield return CreateExactMinigameRule("MN_Rua", typeof(StreetMinigameController));
        yield return CreateExactMinigameRule("MN_Queda", typeof(QuedaMinigameController));
        yield return CreateExactMinigameRule("MN_Round6", typeof(GlassMinigameController));
        yield return CreateExactMinigameRule("MN_Sumo", typeof(SumoMinigameController));
    }

    static SceneRuleSet CreateExactMinigameRule(string sceneName, Type controllerType)
    {
        return new SceneRuleSet
        {
            displayName = sceneName,
            matchMode = SceneMatchMode.Exact,
            pattern = sceneName,
            requiredComponents = new List<RequiredComponentEntry>
            {
                CreateComponentEntry(controllerType, controllerType.Name)
            }
        };
    }
    static SceneRuleSet CloneRuleSet(SceneRuleSet source)
    {
        return new SceneRuleSet
        {
            displayName = source.displayName,
            matchMode = source.matchMode,
            pattern = source.pattern,
            requiredComponents = source.requiredComponents?.Select(CloneComponent).ToList() ?? new List<RequiredComponentEntry>(),
            requiredObjects = source.requiredObjects?.Select(CloneObject).ToList() ?? new List<RequiredObjectEntry>()
        };
    }

    static RequiredComponentEntry CloneComponent(RequiredComponentEntry source)
    {
        return new RequiredComponentEntry
        {
            label = source.label,
            componentTypeName = source.componentTypeName,
            restrictToObjectName = source.restrictToObjectName
        };
    }

    static RequiredObjectEntry CloneObject(RequiredObjectEntry source)
    {
        return new RequiredObjectEntry
        {
            objectName = source.objectName,
            matchMode = source.matchMode,
            mustBeActive = source.mustBeActive
        };
    }
}

static class SceneValidationReflection
{
    static readonly Dictionary<string, Type> Cache = new();

    public static Type ResolveComponentType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        if (Cache.TryGetValue(typeName, out var cached))
        {
            return cached;
        }

        var type = Type.GetType(typeName);
        if (type == null)
        {
            foreach (var candidate in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (candidate.FullName == typeName || candidate.AssemblyQualifiedName == typeName || candidate.Name == typeName)
                {
                    type = candidate;
                    break;
                }
            }
        }

        if (type != null)
        {
            Cache[typeName] = type;
        }

        return type;
    }
}
#endif
