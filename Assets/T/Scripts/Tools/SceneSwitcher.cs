#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerWindow : EditorWindow
{
    // Constantes e chaves para EditorPrefs
    private const int MaxRecent = 5;
    private const string VisibilityPrefsKey = "SceneManager_Visibility";
    private const string FavoritesPrefsKey = "SceneManager_Favorites";
    private const string RecentPrefsKey = "SceneManager_Recent";

    // Campos
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private bool showAllScenes = true;
    private bool showBuildScenes = false;
    private bool showFavorites = false;
    private Color defaultColor;

    private List<string> favoriteScenes = new List<string>();
    private Dictionary<string, bool> sceneVisibility = new Dictionary<string, bool>();

    // Estilos de GUI e conteúdos
    private GUIStyle headerStyle;
    private GUIStyle sceneButtonStyle;
    private GUIStyle categoryBoxStyle;
    private GUIStyle favoriteButtonStyle;
    private GUIStyle actionButtonStyle;

    private GUIContent favoriteOnContent;
    private GUIContent favoriteOffContent;
    private GUIContent menuContent;

    #region Menu & Janela

    [MenuItem("Tools/Scene Manager %#S")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneManagerWindow>("Scene Manager");
        window.minSize = new Vector2(350, 450);
    }

    private void OnEnable()
    {
        defaultColor = GUI.backgroundColor;
        LoadPreferences();
        LoadVisibilityState();
        EditorApplication.delayCall += InitializeStyles;
    }

    #endregion

    #region Inicialização dos Estilos

    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 5, 8, 8),
            margin = new RectOffset(5, 5, 5, 0)
        };

        sceneButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 5, 5, 5),
            margin = new RectOffset(0, 0, 1, 1),
            fixedHeight = 28
        };

        categoryBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(5, 5, 5, 10)
        };

        favoriteButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedWidth = 24,
            fixedHeight = 22,
            padding = new RectOffset(2, 2, 2, 2),
            margin = new RectOffset(2, 2, 3, 3)
        };

        actionButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedWidth = 24,
            fixedHeight = 22,
            padding = new RectOffset(2, 2, 2, 2),
            margin = new RectOffset(2, 2, 3, 3)
        };

        favoriteOnContent = new GUIContent("★", "Remove from Favorites");
        favoriteOffContent = new GUIContent("☆", "Add to Favorites");
        menuContent = new GUIContent("⋮", "Scene Options");
    }

    #endregion

    #region OnGUI

    private void OnGUI()
    {
        if (EditorApplication.isCompiling)
        {
            EditorGUILayout.LabelField("Compilando scripts, aguarde...");
            return;
        }

        DrawToolbar();
        EditorGUILayout.Space(5);

        // Envolve o Begin/End ScrollView com try/finally para garantir que o layout seja finalizado
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        try
        {
            if (showFavorites && favoriteScenes.Count > 0)
            {
                DrawFavoriteScenes();
                EditorGUILayout.Space(5);
            }
            if (showBuildScenes)
            {
                DrawBuildScenes();
                EditorGUILayout.Space(5);
            }
            if (showAllScenes)
                DrawAllScenes();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }

        DrawStatusBar();
    }

    #endregion

    #region Toolbar & StatusBar

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(new GUIContent("↻", "Refresh List"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            Repaint();

        showAllScenes = GUILayout.Toggle(showAllScenes, "All", EditorStyles.toolbarButton, GUILayout.Width(40));
        showBuildScenes = GUILayout.Toggle(showBuildScenes, "Build", EditorStyles.toolbarButton, GUILayout.Width(40));
        showFavorites = GUILayout.Toggle(showFavorites, "Favs", EditorStyles.toolbarButton, GUILayout.Width(40));

        GUILayout.Space(10);

        if (GUILayout.Button("Show All", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            foreach (var key in sceneVisibility.Keys.ToList())
                sceneVisibility[key] = true;
            SaveVisibilityState();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Label("🔍", GUILayout.Width(18));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        GUILayout.Space(5);

        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(45)))
        {
            if (EditorUtility.DisplayDialog("Reset All Preferences",
                "Are you sure you want to reset all scene manager preferences?",
                "Yes", "No"))
            {
                ResetAllPreferences();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        string currentScene = EditorSceneManager.GetActiveScene().name;
        string displayScene = string.IsNullOrEmpty(currentScene) ? "<No Scene>" : currentScene;
        EditorGUILayout.LabelField($"Current: {displayScene}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Desenho das Cenas

    private void DrawFavoriteScenes()
    {
        EditorGUILayout.LabelField("★ Favorite Scenes", headerStyle);
        EditorGUILayout.BeginVertical(categoryBoxStyle);

        bool anyMatch = false;
        foreach (string scenePath in favoriteScenes.ToList())
        {
            if (MatchesFilter(scenePath))
            {
                DrawSceneButton(scenePath, true);
                anyMatch = true;
            }
        }
        if (!anyMatch)
            EditorGUILayout.LabelField("No favorite scenes match the filter", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawBuildScenes()
    {
        EditorGUILayout.LabelField("🔨 Build Scenes", headerStyle);
        EditorGUILayout.BeginVertical(categoryBoxStyle);

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        bool anyMatch = false;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            var sceneSetting = buildScenes[i];
            if (!MatchesFilter(sceneSetting.path))
                continue;

            anyMatch = true;
            EditorGUILayout.BeginHorizontal();

            if (!sceneVisibility.ContainsKey(sceneSetting.path))
                sceneVisibility[sceneSetting.path] = true;

            bool isVisible = sceneVisibility[sceneSetting.path];
            bool newVisible = EditorGUILayout.Toggle(isVisible, GUILayout.Width(16));
            if (newVisible != isVisible)
            {
                sceneVisibility[sceneSetting.path] = newVisible;
                SaveVisibilityState();
            }

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = sceneSetting.enabled ? new Color(0.7f, 1.0f, 0.7f) : new Color(0.8f, 0.8f, 0.8f);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneSetting.path);
            bool isCurrent = sceneSetting.path == EditorSceneManager.GetActiveScene().path;
            if (isCurrent)
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);

            if (GUILayout.Button(new GUIContent(sceneName, sceneSetting.path), sceneButtonStyle, GUILayout.ExpandWidth(true)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(sceneSetting.path);
            }
            GUI.backgroundColor = originalColor;

            sceneSetting.enabled = EditorGUILayout.Toggle(sceneSetting.enabled, GUILayout.Width(16));
            EditorBuildSettings.scenes = buildScenes;

            if (GUILayout.Button("↑", GUILayout.Width(22)) && i > 0)
                SwapBuildScenes(i, i - 1);
            if (GUILayout.Button("↓", GUILayout.Width(22)) && i < buildScenes.Length - 1)
                SwapBuildScenes(i, i + 1);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
                RemoveSceneFromBuild(i);

            bool isFavorite = favoriteScenes.Contains(sceneSetting.path);
            if (GUILayout.Button(isFavorite ? favoriteOnContent : favoriteOffContent, favoriteButtonStyle))
                ToggleFavorite(sceneSetting.path);

            EditorGUILayout.EndHorizontal();
        }

        if (!anyMatch)
            EditorGUILayout.LabelField("No build scenes match the filter", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawAllScenes()
    {
        // Em vez de retornar caso o estilo esteja nulo, inicializamos e continuamos
        if (categoryBoxStyle == null)
            InitializeStyles();

        EditorGUILayout.BeginVertical(categoryBoxStyle);
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        bool anyMatch = false;
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (MatchesFilter(scenePath))
            {
                DrawSceneButton(scenePath, favoriteScenes.Contains(scenePath));
                anyMatch = true;
            }
        }
        if (!anyMatch)
            EditorGUILayout.LabelField("No scenes match the filter", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawSceneButton(string scenePath, bool isFavorite)
    {
        if (!sceneVisibility.ContainsKey(scenePath))
            sceneVisibility[scenePath] = true;
        if (!sceneVisibility[scenePath] && string.IsNullOrEmpty(searchFilter))
            return;

        EditorGUILayout.BeginHorizontal();
        try
        {
            bool visible = sceneVisibility[scenePath];
            bool newVisible = EditorGUILayout.Toggle(visible, GUILayout.Width(16));
            if (newVisible != visible)
            {
                sceneVisibility[scenePath] = newVisible;
                SaveVisibilityState();
            }

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            bool isCurrent = scenePath == EditorSceneManager.GetActiveScene().path;
            bool isInBuild = EditorBuildSettings.scenes.Any(s => s.path == scenePath);

            Color originalColor = GUI.backgroundColor;
            if (isCurrent)
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
            else if (isInBuild)
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);

            string displayName = isInBuild ? $"{sceneName} [Build]" : sceneName;
            if (GUILayout.Button(new GUIContent(displayName, scenePath), sceneButtonStyle, GUILayout.ExpandWidth(true)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(scenePath);
            }
            GUI.backgroundColor = originalColor;

            if (GUILayout.Button(isFavorite ? favoriteOnContent : favoriteOffContent, favoriteButtonStyle))
                ToggleFavorite(scenePath);

            if (GUILayout.Button(menuContent, actionButtonStyle))
                ShowSceneContextMenu(scenePath);
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion

    #region Helper & Persistência

    private bool MatchesFilter(string scenePath)
    {
        if (string.IsNullOrEmpty(searchFilter))
            return true;
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        return sceneName.ToLower().Contains(searchFilter.ToLower()) ||
               scenePath.ToLower().Contains(searchFilter.ToLower());
    }

    private void ToggleFavorite(string scenePath)
    {
        if (favoriteScenes.Contains(scenePath))
            favoriteScenes.Remove(scenePath);
        else
            favoriteScenes.Add(scenePath);
        SavePreferences();
    }

    private void ShowSceneContextMenu(string scenePath)
    {
        GenericMenu menu = new GenericMenu();
        bool isInBuild = EditorBuildSettings.scenes.Any(s => s.path == scenePath);
        if (isInBuild)
            menu.AddItem(new GUIContent("Remove from Build"), false, () => RemoveFromBuild(scenePath));
        else
            menu.AddItem(new GUIContent("Add to Build Settings"), false, () => AddSceneToBuild(scenePath));

        menu.AddItem(new GUIContent("Show in Project"), false, () => ShowSceneInProject(scenePath));
        menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = scenePath);
        menu.AddSeparator("");

        bool isFavorite = favoriteScenes.Contains(scenePath);
        menu.AddItem(new GUIContent(isFavorite ? "Remove from Favorites" : "Add to Favorites"), false, () => ToggleFavorite(scenePath));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent(sceneVisibility[scenePath] ? "Hide Scene" : "Show Scene"), false, () =>
        {
            sceneVisibility[scenePath] = !sceneVisibility[scenePath];
            SaveVisibilityState();
        });
        menu.ShowAsContext();
    }

    private void RemoveFromBuild(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == scenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private void SwapBuildScenes(int indexA, int indexB)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        var temp = scenes[indexA];
        scenes[indexA] = scenes[indexB];
        scenes[indexB] = temp;
        EditorBuildSettings.scenes = scenes;
    }

    private void RemoveSceneFromBuild(int index)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (index >= 0 && index < scenes.Count)
        {
            scenes.RemoveAt(index);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private void AddSceneToBuild(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private void ShowSceneInProject(string scenePath)
    {
        UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath);
        if (sceneAsset != null)
        {
            EditorGUIUtility.PingObject(sceneAsset);
            Selection.activeObject = sceneAsset;
        }
    }

    private void SaveVisibilityState()
    {
        string data = string.Join("|", sceneVisibility.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        EditorPrefs.SetString(VisibilityPrefsKey, data);
    }

    private void LoadVisibilityState()
    {
        string data = EditorPrefs.GetString(VisibilityPrefsKey, "");
        sceneVisibility.Clear();
        if (!string.IsNullOrEmpty(data))
        {
            foreach (string entry in data.Split('|'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && bool.TryParse(parts[1], out bool visible))
                    sceneVisibility[parts[0]] = visible;
            }
        }
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(FavoritesPrefsKey, string.Join("|", favoriteScenes));
    }

    private void LoadPreferences()
    {
        string favData = EditorPrefs.GetString(FavoritesPrefsKey, "");
        favoriteScenes = string.IsNullOrEmpty(favData)
            ? new List<string>()
            : favData.Split('|').ToList();
    }

    private void ResetAllPreferences()
    {
        EditorPrefs.DeleteKey(VisibilityPrefsKey);
        EditorPrefs.DeleteKey(FavoritesPrefsKey);
        EditorPrefs.DeleteKey(RecentPrefsKey);
        sceneVisibility.Clear();
        favoriteScenes.Clear();
    }

    #endregion
}
#endif