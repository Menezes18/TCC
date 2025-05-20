using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PrefabConstructorEditor : EditorWindow
{
    public List<GameObject> models = new List<GameObject>();
    private SerializedObject serializedObject;
    private SerializedProperty modelsProperty;

    [MenuItem("Tools/Prefab Constructor")]
    public static void ShowWindow()
    {
        GetWindow<PrefabConstructorEditor>("Prefab Constructor");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        modelsProperty = serializedObject.FindProperty("models");
    }

    private void OnGUI()
    {
        GUILayout.Label("Model List", EditorStyles.boldLabel);
        serializedObject.Update();
        EditorGUILayout.PropertyField(modelsProperty, true);
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Prefabs"))
        {
            CreatePrefabs();
        }
    }

    private void CreatePrefabs()
    {
        string prefabFolder = "Assets/Prefab/PrefabConstructer";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        foreach (var model in models)
        {
            if (model == null) continue;

            // Create parent object
            GameObject parent = new GameObject(model.name + "_Parent");
            GameObject instance = Instantiate(model, parent.transform);

            // Optional: reset model transform
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            instance.transform.localScale = Vector3.one;

            // Create prefab
            string prefabPath = $"{prefabFolder}/{model.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(parent, prefabPath);

            // Clean up
            DestroyImmediate(parent);
        }

        Debug.Log("Prefabs criado bobão.");
    }
}
