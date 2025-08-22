// Coloque em Assets/Editor/SetModelMaterials.cs
using UnityEditor;
using UnityEngine;

public class SetModelMaterials : EditorWindow
{
    [MenuItem("Tools/Materials/Set to Standard (Legacy)")]
    private static void SetMaterialsWindow()
    {
        var folderAbs = EditorUtility.OpenFolderPanel("Selecione a pasta com os modelos", "Assets", "");
        if (string.IsNullOrEmpty(folderAbs)) return;

        // Converte caminho absoluto para relativo (Assets/...)
        var folderRel = "Assets" + folderAbs.Replace(Application.dataPath, "");

        // Procura por todos os assets do tipo Model dentro da pasta
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderRel });

        int count = guids.Length;
        if (count == 0)
        {
            EditorUtility.DisplayDialog("Nada encontrado", "Nenhum Model encontrado nessa pasta.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                // Muda para Standard (Legacy)
#if UNITY_2020_2_OR_NEWER
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
#else
                importer.globalScale = importer.globalScale; // só pra evitar warning em versões antigas
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
#endif
                EditorUtility.DisplayProgressBar("Atualizando materiais...", path, (float)i / count);

                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Atualizados {count} modelos para 'Standard (Legacy)'.");
        EditorUtility.DisplayDialog("Concluído", $"Atualizados {count} modelos.", "OK");
    }
}
