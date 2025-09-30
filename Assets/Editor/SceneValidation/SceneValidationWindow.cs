#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SceneValidationWindow : EditorWindow
{
    SerializedObject _serializedConfig;
    SerializedProperty _ruleSetsProperty;
    Vector2 _scroll;

    [MenuItem("Tools/Scene Validation/Rule Manager", priority = 2)]
    public static void Open()
    {
        var window = GetWindow<SceneValidationWindow>("Scene Validation Rules");
        window.minSize = new Vector2(540, 360);
        window.Show();
    }

    void OnEnable()
    {
        var config = SceneValidationSettings.Config;
        if (config == null)
        {
            return;
        }

        _serializedConfig = new SerializedObject(config);
        _ruleSetsProperty = _serializedConfig.FindProperty("ruleSets");
    }

    void OnGUI()
    {
        if (_serializedConfig == null || _ruleSetsProperty == null)
        {
            EditorGUILayout.HelpBox("Scene validation config asset not found.", MessageType.Error);
            if (GUILayout.Button("Create Config"))
            {
                var config = SceneValidationSettings.Config;
                if (config != null)
                {
                    _serializedConfig = new SerializedObject(config);
                    _ruleSetsProperty = _serializedConfig.FindProperty("ruleSets");
                }
            }
            return;
        }

        _serializedConfig.Update();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Configure validation rules per scene pattern. Add, edit or remove requirements and they will be saved into the shared config asset.", MessageType.Info);
        DrawToolbar();
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawRuleSets();
        EditorGUILayout.EndScrollView();

        _serializedConfig.ApplyModifiedProperties();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(new GUIContent("New Rule Set", "Create a new rule set for a scene or pattern."), EditorStyles.toolbarButton))
        {
            _ruleSetsProperty.arraySize++;
        }

        if (GUILayout.Button(new GUIContent("Reset to Defaults", "Replace all rules with the default configuration."), EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("Reset Scene Validation", "This will replace all custom rules with the default configuration. Continue?", "Reset", "Cancel"))
            {
                SceneValidationSettings.ResetToDefaults();
                OnEnable();
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Open Asset", "Select the underlying SceneValidationConfig asset in the Project window."), EditorStyles.toolbarButton))
        {
            Selection.activeObject = _serializedConfig.targetObject;
            EditorGUIUtility.PingObject(_serializedConfig.targetObject);
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawRuleSets()
    {
        if (_ruleSetsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No rule sets defined. Add one to start configuring validation.", MessageType.Info);
            return;
        }

        for (int i = 0; i < _ruleSetsProperty.arraySize; i++)
        {
            var ruleSetProp = _ruleSetsProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ruleSetProp.FindPropertyRelative("displayName"));
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                _ruleSetsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(ruleSetProp.FindPropertyRelative("matchMode"));
            EditorGUILayout.PropertyField(ruleSetProp.FindPropertyRelative("pattern"));

            EditorGUILayout.Space();
            DrawRequiredComponents(ruleSetProp.FindPropertyRelative("requiredComponents"));
            EditorGUILayout.Space();
            DrawRequiredObjects(ruleSetProp.FindPropertyRelative("requiredObjects"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    void DrawRequiredComponents(SerializedProperty componentsProp)
    {
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Required Components", EditorStyles.boldLabel);

        for (int i = 0; i < componentsProp.arraySize; i++)
        {
            var entryProp = componentsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");

            var labelProp = entryProp.FindPropertyRelative("label");
            var typeNameProp = entryProp.FindPropertyRelative("componentTypeName");
            var restrictProp = entryProp.FindPropertyRelative("restrictToObjectName");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(labelProp);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                componentsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Component", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.SelectableLabel(SimplifyTypeName(typeNameProp.stringValue), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("Pick", GUILayout.Width(60)))
            {
                ComponentTypePicker.Show(type =>
                {
                    typeNameProp.stringValue = type.AssemblyQualifiedName;
                    if (string.IsNullOrWhiteSpace(labelProp.stringValue))
                    {
                        labelProp.stringValue = type.Name;
                    }
                    entryProp.serializedObject.ApplyModifiedProperties();
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(restrictProp);
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Component Rule"))
        {
            componentsProp.arraySize++;
        }

        EditorGUILayout.EndVertical();
    }

    void DrawRequiredObjects(SerializedProperty objectsProp)
    {
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Required GameObjects", EditorStyles.boldLabel);

        for (int i = 0; i < objectsProp.arraySize; i++)
        {
            var entryProp = objectsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");

            var nameProp = entryProp.FindPropertyRelative("objectName");
            var modeProp = entryProp.FindPropertyRelative("matchMode");
            var activeProp = entryProp.FindPropertyRelative("mustBeActive");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(nameProp);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                objectsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(modeProp);
            EditorGUILayout.PropertyField(activeProp);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add GameObject Rule"))
        {
            objectsProp.arraySize++;
        }

        EditorGUILayout.EndVertical();
    }

    static string SimplifyTypeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "(none)";
        }

        try
        {
            var type = SceneValidationReflection.ResolveComponentType(fullName);
            return type != null ? type.FullName : fullName;
        }
        catch
        {
            return fullName;
        }
    }
}

static class ComponentTypePicker
{
    static readonly List<Type> ComponentTypes;
    static Action<Type> _onPicked;

    static ComponentTypePicker()
    {
        ComponentTypes = TypeCache.GetTypesDerivedFrom<Component>()
            .Where(t => !t.IsAbstract && t.IsPublic)
            .OrderBy(t => t.FullName)
            .ToList();
    }

    public static void Show(Action<Type> onPicked)
    {
        _onPicked = onPicked;
        var menu = new GenericMenu();

        foreach (var type in ComponentTypes)
        {
            var content = new GUIContent(type.FullName.Replace('.', '/'));
            menu.AddItem(content, false, HandleSelection, type);
        }

        menu.ShowAsContext();
    }

    static void HandleSelection(object userData)
    {
        if (userData is Type type)
        {
            _onPicked?.Invoke(type);
        }
    }
}
#endif
