#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight scene validation framework tailored to the current multiplayer flow.
/// Add new ISceneRule implementations or extend <see cref="SceneValidationProfile"/> as scenes evolve.
/// </summary>
static class SceneValidator
{
    public static bool Validate(Scene scene, out List<string> errors)
    {
        errors = new List<string>();

        if (!scene.IsValid())
        {
            errors.Add("Scene handle is invalid.");
            return false;
        }

        if (!scene.isLoaded)
        {
            errors.Add("Scene must be loaded before validation can run.");
            return false;
        }

        foreach (var rule in SceneValidationProfile.GetRules(scene))
        {
            rule?.Evaluate(scene, errors);
        }

        return errors.Count == 0;
    }

    public static bool ValidateSceneAsset(string scenePath, out List<string> errors)
    {
        var loadedScene = SceneManager.GetSceneByPath(scenePath);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            return Validate(loadedScene, out errors);
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            return Validate(scene, out errors);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}

interface ISceneRule
{
    string Description { get; }
    bool Evaluate(Scene scene, ICollection<string> errors);
}

static class SceneValidationProfile
{
    public static IEnumerable<ISceneRule> GetRules(Scene scene)
    {
        var config = SceneValidationSettings.Config;
        if (config == null || config.ruleSets == null)
        {
            yield break;
        }

        foreach (var ruleSet in config.ruleSets)
        {
            if (ruleSet == null || string.IsNullOrWhiteSpace(ruleSet.pattern))
            {
                continue;
            }

            if (!SceneNameMatches(scene.name, ruleSet))
            {
                continue;
            }

            if (ruleSet.requiredComponents != null)
            {
                foreach (var componentEntry in ruleSet.requiredComponents)
                {
                    if (componentEntry == null || string.IsNullOrWhiteSpace(componentEntry.componentTypeName))
                    {
                        continue;
                    }

                    var type = SceneValidationReflection.ResolveComponentType(componentEntry.componentTypeName);
                    if (type == null || !typeof(Component).IsAssignableFrom(type))
                    {
                        Debug.LogWarning($"[SceneValidation] Could not resolve component type '{componentEntry.componentTypeName}' for rule set '{ruleSet.displayName}'.");
                        continue;
                    }

                    var label = string.IsNullOrWhiteSpace(componentEntry.label) ? type.Name : componentEntry.label;
                    yield return new RequiredComponentTypeRule(type, label, componentEntry.restrictToObjectName);
                }
            }

            if (ruleSet.requiredObjects != null)
            {
                foreach (var objectEntry in ruleSet.requiredObjects)
                {
                    if (objectEntry == null || string.IsNullOrWhiteSpace(objectEntry.objectName))
                    {
                        continue;
                    }

                    yield return new RequiredNamedObjectRule(objectEntry.objectName, objectEntry.matchMode, objectEntry.mustBeActive);
                }
            }
        }
    }

    static bool SceneNameMatches(string sceneName, SceneRuleSet ruleSet)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        switch (ruleSet.matchMode)
        {
            case SceneMatchMode.Exact:
                return string.Equals(sceneName, ruleSet.pattern, comparison);
            case SceneMatchMode.Prefix:
                return sceneName.StartsWith(ruleSet.pattern, comparison);
            case SceneMatchMode.Suffix:
                return sceneName.EndsWith(ruleSet.pattern, comparison);
            case SceneMatchMode.Contains:
                return sceneName.IndexOf(ruleSet.pattern, comparison) >= 0;
            default:
                return false;
        }
    }
}

abstract class SceneRule : ISceneRule
{
    public abstract string Description { get; }

    public abstract bool Evaluate(Scene scene, ICollection<string> errors);

    protected static IEnumerable<GameObject> EnumerateRootObjects(Scene scene)
    {
        return scene.GetRootGameObjects() ?? Array.Empty<GameObject>();
    }

    protected static IEnumerable<GameObject> EnumerateAllGameObjects(Scene scene)
    {
        foreach (var root in EnumerateRootObjects(scene))
        {
            if (root == null)
            {
                continue;
            }

            yield return root;

            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null)
                {
                    yield return transform.gameObject;
                }
            }
        }
    }

    protected static void AppendError(Scene scene, ICollection<string> errors, string message)
    {
        errors.Add($"[{scene.name}] {message}");
    }
}

sealed class RequiredComponentTypeRule : SceneRule
{
    readonly Type _componentType;
    readonly string _label;
    readonly string _restrictToObjectName;

    public RequiredComponentTypeRule(Type componentType, string label, string restrictToObjectName)
    {
        _componentType = componentType;
        _label = label;
        _restrictToObjectName = restrictToObjectName;
    }

    public override string Description => $"Must include {_label}";

    public override bool Evaluate(Scene scene, ICollection<string> errors)
    {
        if (_componentType == null)
        {
            return true;
        }

        bool MatchName(GameObject go)
        {
            if (string.IsNullOrWhiteSpace(_restrictToObjectName))
            {
                return true;
            }

            return string.Equals(go.name, _restrictToObjectName, StringComparison.OrdinalIgnoreCase);
        }

        Component resolvedComponent = null;

        foreach (var component in EnumerateRootObjects(scene)
                     .SelectMany(root => root.GetComponentsInChildren(_componentType, true)))
        {
            if (component == null)
            {
                continue;
            }

            if (!MatchName(component.gameObject))
            {
                continue;
            }

            resolvedComponent = component as Component;
            if (resolvedComponent != null)
            {
                break;
            }
        }

        if (resolvedComponent == null)
        {
            AppendError(scene, errors, $"Missing {_label}.");
            return false;
        }

        return true;
    }
}

sealed class RequiredNamedObjectRule : SceneRule
{
    readonly string _objectName;
    readonly SceneMatchMode _matchMode;
    readonly bool _mustBeActive;

    public RequiredNamedObjectRule(string objectName, SceneMatchMode matchMode, bool mustBeActive)
    {
        _objectName = objectName;
        _matchMode = matchMode;
        _mustBeActive = mustBeActive;
    }

    public override string Description => $"Must include object '{_objectName}'";

    public override bool Evaluate(Scene scene, ICollection<string> errors)
    {
        bool Matches(string candidate)
        {
            var comparison = StringComparison.OrdinalIgnoreCase;
            return _matchMode switch
            {
                SceneMatchMode.Exact => string.Equals(candidate, _objectName, comparison),
                SceneMatchMode.Prefix => candidate.StartsWith(_objectName, comparison),
                SceneMatchMode.Suffix => candidate.EndsWith(_objectName, comparison),
                SceneMatchMode.Contains => candidate.IndexOf(_objectName, comparison) >= 0,
                _ => false
            };
        }

        foreach (var go in EnumerateAllGameObjects(scene))
        {
            if (go == null)
            {
                continue;
            }

            if (!Matches(go.name))
            {
                continue;
            }

            if (_mustBeActive && !go.activeInHierarchy)
            {
                AppendError(scene, errors, $"GameObject '{go.name}' is present but disabled.");
                return false;
            }

            return true;
        }

        AppendError(scene, errors, $"Missing GameObject '{_objectName}' as per rule.");
        return false;
    }
}

static class SceneValidationMenu
{
    const string MenuRoot = "Tools/Scene Validation";

    [MenuItem(MenuRoot + "/Validate Current Scene", priority = 0)]
    public static void ValidateCurrentScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Scene Validation", "No active scene loaded.", "OK");
            return;
        }

        if (SceneValidator.Validate(activeScene, out var errors))
        {
            EditorUtility.DisplayDialog("Scene Validation", $"Scene '{activeScene.name}' passed all rules.", "Great!");
        }
        else
        {
            EditorUtility.DisplayDialog("Scene Validation", BuildErrorReport(errors), "Fix issues");
        }
    }

    [MenuItem(MenuRoot + "/Validate All Build Scenes", priority = 1)]
    public static void ValidateAllBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
        var aggregatedErrors = new List<string>();

        foreach (var scene in scenes)
        {
            if (SceneValidator.ValidateSceneAsset(scene.path, out var errors))
            {
                continue;
            }

            aggregatedErrors.AddRange(errors);
        }

        if (aggregatedErrors.Count == 0)
        {
            EditorUtility.DisplayDialog("Scene Validation", "All enabled build scenes passed the checks.", "Nice!");
        }
        else
        {
            EditorUtility.DisplayDialog("Scene Validation", BuildErrorReport(aggregatedErrors), "Review scenes");
        }
    }

    internal static string BuildErrorReport(IEnumerable<string> errors)
    {
        return string.Join("\n", errors);
    }
}

sealed class SceneValidationBuildProcessor : IPreprocessBuildWithReport, IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        var aggregatedErrors = new List<string>();

        foreach (var scene in buildScenes)
        {
            if (SceneValidator.ValidateSceneAsset(scene.path, out var errors))
            {
                continue;
            }

            aggregatedErrors.AddRange(errors);
        }

        if (aggregatedErrors.Count > 0)
        {
            throw new BuildFailedException("Scene validation failed before build:\n" + string.Join("\n", aggregatedErrors));
        }
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (SceneValidator.Validate(scene, out var errors))
        {
            return;
        }

        throw new BuildFailedException($"Scene '{scene.name}' failed validation during build:\n" + string.Join("\n", errors));
    }
}

sealed class SceneSaveValidator : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        var scenePaths = paths.Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)).ToList();
        if (scenePaths.Count == 0)
        {
            return paths;
        }

        var aggregatedErrors = new List<string>();

        foreach (var scenePath in scenePaths)
        {
            if (SceneValidator.ValidateSceneAsset(scenePath, out var errors))
            {
                continue;
            }

            aggregatedErrors.AddRange(errors);
        }

        if (aggregatedErrors.Count > 0)
        {
            Debug.LogError("Scene validation issues detected before save:\n" + string.Join("\n", aggregatedErrors));
            EditorUtility.DisplayDialog("Scene Validation", SceneValidationMenu.BuildErrorReport(aggregatedErrors), "Understood");
        }

        return paths;
    }
}
#endif
