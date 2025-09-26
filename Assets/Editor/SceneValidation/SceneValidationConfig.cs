#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneValidationConfig", menuName = "Scene Validation/Config", order = 0)]
public class SceneValidationConfig : ScriptableObject
{
    public List<SceneRuleSet> ruleSets = new();
}

[Serializable]
public class SceneRuleSet
{
    public string displayName = "New Rule Set";
    public SceneMatchMode matchMode = SceneMatchMode.Exact;
    public string pattern = string.Empty;
    public List<RequiredComponentEntry> requiredComponents = new();
    public List<RequiredObjectEntry> requiredObjects = new();
}

public enum SceneMatchMode
{
    Exact,
    Prefix,
    Suffix,
    Contains
}

[Serializable]
public class RequiredComponentEntry
{
    public string label = string.Empty;
    public string componentTypeName = string.Empty;
    public string restrictToObjectName = string.Empty;
}

[Serializable]
public class RequiredObjectEntry
{
    public string objectName = string.Empty;
    public SceneMatchMode matchMode = SceneMatchMode.Exact;
    public bool mustBeActive = true;
}
#endif
