using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MinigameSettingsWindow : EditorWindow
{
    private const string DefaultCatalogPath = "Assets/Scripts/Data/ScriptableObjects/Minigames/MinigameCatalog.asset";
    private const string DefaultSettingsFolder = "Assets/Scripts/Minigames/DataMinigame/Data";

    [SerializeField] private MinigameCatalog catalog;
    private Vector2 _scroll;

    [MenuItem("Tools/Minigames/Configurar Pontos e Tempo")] 
    public static void Open()
    {
        var win = GetWindow<MinigameSettingsWindow>(true, "Minigames: Pontos e Tempo");
        win.minSize = new Vector2(680, 340);
        win.Show();
    }

    private void OnEnable()
    {
        if (catalog == null)
        {
            var direct = AssetDatabase.LoadAssetAtPath<MinigameCatalog>(DefaultCatalogPath);
            if (direct != null) catalog = direct;
            else
            {
                var guid = AssetDatabase.FindAssets("t:MinigameCatalog").FirstOrDefault();
                if (!string.IsNullOrEmpty(guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    catalog = AssetDatabase.LoadAssetAtPath<MinigameCatalog>(path);
                }
            }
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("Selecione um MinigameCatalog para editar as configuracoes.", MessageType.Info);
            return;
        }

        using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = sv.scrollPosition;
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || !entry.HasValidScene) continue;
                DrawEntry(entry);
                EditorGUILayout.Space(2);
            }
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            catalog = (MinigameCatalog)EditorGUILayout.ObjectField(catalog, typeof(MinigameCatalog), false, GUILayout.Width(360));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Salvar", EditorStyles.toolbarButton, GUILayout.Width(70)))
                AssetDatabase.SaveAssets();
            if (GUILayout.Button("Recarregar", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (catalog != null)
                {
                    var path = AssetDatabase.GetAssetPath(catalog);
                    catalog = AssetDatabase.LoadAssetAtPath<MinigameCatalog>(path);
                }
            }
        }
    }

    private void DrawEntry(MinigameCatalog.MinigameEntry entry)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(string.IsNullOrEmpty(entry.displayName) ? entry.id : entry.displayName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(entry.SceneIdentifier, EditorStyles.miniLabel);
            }

            var settings = entry.settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Sem Settings", MessageType.Warning);
                if (GUILayout.Button("Criar Settings", GUILayout.Width(130)))
                {
                    settings = CreateSettingsForEntry(entry);
                    entry.settings = settings;
                    EditorUtility.SetDirty(catalog);
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(settings, "Editar SettingsMinigame");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Duracao", GUILayout.Width(70));
                settings.miniGameDuration = Mathf.Max(0f, EditorGUILayout.FloatField(settings.miniGameDuration, GUILayout.Width(90)));

               
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Bonus", GUILayout.Width(70));
                settings.firstPlaceBonus = Mathf.Max(0, EditorGUILayout.IntField(settings.firstPlaceBonus, GUILayout.Width(56)));
                settings.secondPlaceBonus = Mathf.Max(0, EditorGUILayout.IntField(settings.secondPlaceBonus, GUILayout.Width(56)));
                settings.thirdPlaceBonus = Mathf.Max(0, EditorGUILayout.IntField(settings.thirdPlaceBonus, GUILayout.Width(56)));
                settings.fourthPlaceBonus = Mathf.Max(0, EditorGUILayout.IntField(settings.fourthPlaceBonus, GUILayout.Width(56)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Selecionar Catalog", GUILayout.Width(120)))
                {
                    EditorGUIUtility.PingObject(catalog);
                }

                if (GUILayout.Button("Settings", GUILayout.Width(90))) EditorGUIUtility.PingObject(settings);
            }
        }
    }

    private static SettingsMiniGameData CreateSettingsForEntry(MinigameCatalog.MinigameEntry entry)
    {
        if (!AssetDatabase.IsValidFolder(DefaultSettingsFolder))
        {
            var parts = DefaultSettingsFolder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        string baseName = string.IsNullOrEmpty(entry.displayName) ? entry.id : entry.displayName;
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "SettingsMinigame";
        foreach (var c in Path.GetInvalidFileNameChars()) baseName = baseName.Replace(c, '_');

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(DefaultSettingsFolder, baseName + "Minigame.asset"));

        var settings = ScriptableObject.CreateInstance<SettingsMiniGameData>();
        settings.miniGameName = entry.SceneIdentifier;
        settings.miniGameDuration = 120f;
        settings.firstPlaceBonus = 50;
        settings.secondPlaceBonus = 30;
        settings.thirdPlaceBonus = 10;
        settings.fourthPlaceBonus = 5;

        AssetDatabase.CreateAsset(settings, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(settings);
        return settings;
    }
}

