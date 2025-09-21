using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GlassPathEditorWindow : EditorWindow
{
    private GlassPathData _path;
    private Vector2 _scroll;
    private int _rows = 6;

    [MenuItem("Tools/Glass Path Editor")]
    public static void Open()
    {
        GetWindow<GlassPathEditorWindow>("Glass Path Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            _path = (GlassPathData)EditorGUILayout.ObjectField("Path Asset", _path, typeof(GlassPathData), false);
            if (GUILayout.Button("New", GUILayout.Width(60)))
            {
                var asset = ScriptableObject.CreateInstance<GlassPathData>();
                var save = EditorUtility.SaveFilePanelInProject("Salvar GlassPathData", "GlassPath", "asset", "Local para salvar o caminho");
                if (!string.IsNullOrEmpty(save))
                {
                    AssetDatabase.CreateAsset(asset, save);
                    AssetDatabase.SaveAssets();
                    _path = asset;
                }
            }
        }

        if (_path == null)
        {
            EditorGUILayout.HelpBox("Selecione ou crie um GlassPathData.", MessageType.Info);
            return;
        }

        // Linha de controle de tamanho
        _rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _path.rows != null ? _path.rows.Count : _rows));
        EnsureRowCount(_path, _rows);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Marque o lado seguro por linha (exclusivo)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _path.rows.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Row {i}", GUILayout.Width(60));
                bool left  = _path.rows[i] == GlassSide.Left;
                bool right = _path.rows[i] == GlassSide.Right;

                bool newLeft  = GUILayout.Toggle(left,  "Left",  "Button");
                bool newRight = GUILayout.Toggle(right, "Right", "Button");

                if (newLeft && !left)
                    _path.rows[i] = GlassSide.Left;
                else if (newRight && !right)
                    _path.rows[i] = GlassSide.Right;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Salvar"))
            {
                EditorUtility.SetDirty(_path);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Aplicar ao Controller Selecionado"))
            {
                ApplyToSelectedController();
            }
        }
    }

    private static void EnsureRowCount(GlassPathData path, int rows)
    {
        if (path.rows == null) path.rows = new List<GlassSide>();
        while (path.rows.Count < rows) path.rows.Add(GlassSide.Left);
        while (path.rows.Count > rows) path.rows.RemoveAt(path.rows.Count - 1);
        EditorUtility.SetDirty(path);
    }

    private void ApplyToSelectedController()
    {
        var ctrl = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<GlassMinigameController>() : null;
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("Glass Path Editor", "Selecione um GameObject com GlassMinigameController para aplicar.", "OK");
            return;
        }

        var so = new SerializedObject(ctrl);
        so.Update();
        so.FindProperty("pathData").objectReferenceValue = _path;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[GlassPathEditor] Caminho aplicado ao controller selecionado.");
    }
}

