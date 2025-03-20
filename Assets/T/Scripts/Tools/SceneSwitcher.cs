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
    #region Fields & Constants

    private Vector2 scrollPosition;
    private string searchFilter = "";
    private bool showAllScenes = true;
    private bool showBuildScenes = false;
    private bool showFavorites = false;
    private Color defaultColor;

    private List<string> favoriteScenes = new List<string>();
    private List<string> recentScenes = new List<string>();
    private const int MAX_RECENT = 5;
    private Dictionary<string, bool> sceneVisibility = new Dictionary<string, bool>();

    private const string VISIBILITY_PREFS_KEY = "SceneManager_Visibility";
    private const string FAVORITES_PREFS_KEY = "SceneManager_Favorites";
    private const string RECENT_PREFS_KEY = "SceneManager_Recent";

    private GUIStyle headerStyle;
    private GUIStyle sceneButtonStyle;

    #endregion

    #region Menu & Window Setup

    [MenuItem("Tools/Scene Manager %#S")] // Ctrl/Cmd + Shift + S
    public static void ShowWindow()
    {
        var window = GetWindow<SceneManagerWindow>("Scene Manager");
        window.minSize = new Vector2(300, 400);
    }

    private void OnEnable()
    {
        defaultColor = GUI.backgroundColor;
        LoadPreferences();
        LoadVisibilityState();
        InitializeStyles();
    }

    #endregion

    #region State Persistence

    private void SaveVisibilityState()
    {
        string visibilityData = string.Join("|", sceneVisibility.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        EditorPrefs.SetString(VISIBILITY_PREFS_KEY, visibilityData);
    }

    private void LoadVisibilityState()
    {
        string visibilityData = EditorPrefs.GetString(VISIBILITY_PREFS_KEY, "");
        sceneVisibility.Clear();

        if (!string.IsNullOrEmpty(visibilityData))
        {
            foreach (string entry in visibilityData.Split('|'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && bool.TryParse(parts[1], out bool visible))
                {
                    sceneVisibility[parts[0]] = visible;
                }
            }
        }
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(FAVORITES_PREFS_KEY, string.Join("|", favoriteScenes));
        EditorPrefs.SetString(RECENT_PREFS_KEY, string.Join("|", recentScenes));
    }

    private void LoadPreferences()
    {
        string favoritesString = EditorPrefs.GetString(FAVORITES_PREFS_KEY, "");
        string recentString = EditorPrefs.GetString(RECENT_PREFS_KEY, "");

        favoriteScenes = string.IsNullOrEmpty(favoritesString) ? new List<string>() : favoritesString.Split('|').ToList();
        recentScenes = string.IsNullOrEmpty(recentString) ? new List<string>() : recentString.Split('|').ToList();
    }

    private void ResetAllPreferences()
    {
        EditorPrefs.DeleteKey(VISIBILITY_PREFS_KEY);
        EditorPrefs.DeleteKey(FAVORITES_PREFS_KEY);
        EditorPrefs.DeleteKey(RECENT_PREFS_KEY);
        sceneVisibility.Clear();
        favoriteScenes.Clear();
        recentScenes.Clear();
    }

    #endregion

    #region GUI Styles

    private void InitializeStyles()
    {
        // Estilo para os cabeçalhos
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 10, 10)
        };

        // Estilo para os botões de cena – com fonte maior, padding aumentado e altura fixa
        sceneButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,  // Centraliza o texto dentro do botão
            padding = new RectOffset(20, 20, 12, 12),
            fixedHeight = 40                      // Define uma altura fixa maior para o botão
        };
    }

    #endregion

    #region OnGUI

    private void OnGUI()
    {
        // Evita erros de layout enquanto compila
        if (EditorApplication.isCompiling)
            return;

        try
        {
            DrawToolbar();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (showFavorites && favoriteScenes.Count > 0)
                DrawFavoriteScenes();

            if (recentScenes.Count > 0)
                DrawRecentScenes();

            if (showBuildScenes)
                DrawBuildScenes();

            if (showAllScenes)
                DrawAllScenes();

            EditorGUILayout.EndScrollView();

            DrawStatusBar();
        }
        catch (Exception e)
        {
            Debug.LogError($"Scene Manager Window GUI Error: {e}");
        }
    }

    #endregion

    #region Toolbar & Status Bar

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            Repaint();

        showAllScenes = GUILayout.Toggle(showAllScenes, "All Scenes", EditorStyles.toolbarButton);
        showBuildScenes = GUILayout.Toggle(showBuildScenes, "Build Scenes", EditorStyles.toolbarButton);
        showFavorites = GUILayout.Toggle(showFavorites, "Favorites", EditorStyles.toolbarButton);

        if (GUILayout.Button("Show All Hidden", EditorStyles.toolbarButton))
        {
            foreach (var key in sceneVisibility.Keys.ToList())
                sceneVisibility[key] = true;
            SaveVisibilityState();
        }

        if (GUILayout.Button("Reset All", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("Reset All Preferences",
                "Are you sure you want to reset all scene manager preferences?",
                "Yes", "No"))
            {
                ResetAllPreferences();
            }
        }

        GUILayout.FlexibleSpace();
        // Aumenta a largura do campo de pesquisa para facilitar a visualização
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField($"Current Scene: {EditorSceneManager.GetActiveScene().name}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Scene Draw Methods

    private void DrawFavoriteScenes()
    {
        EditorGUILayout.LabelField("Favorite Scenes", headerStyle);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        foreach (string scenePath in favoriteScenes.ToList())
        {
            if (MatchesFilter(scenePath))
                DrawSceneButton(scenePath, true);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawRecentScenes()
    {
        EditorGUILayout.LabelField("Recent Scenes", headerStyle);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        foreach (string scenePath in recentScenes.ToList())
        {
            if (MatchesFilter(scenePath))
                DrawSceneButton(scenePath, false);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawBuildScenes()
    {
        EditorGUILayout.LabelField("Build Scenes", headerStyle);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            var sceneSetting = buildScenes[i];
            if (!MatchesFilter(sceneSetting.path))
                continue;

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = sceneSetting.enabled ? Color.green : Color.gray;
            DrawSceneButton(sceneSetting.path, false);
            GUI.backgroundColor = defaultColor;

            sceneSetting.enabled = EditorGUILayout.Toggle(sceneSetting.enabled, GUILayout.Width(20));

            if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
                SwapBuildScenes(i, i - 1);

            if (GUILayout.Button("↓", GUILayout.Width(25)) && i < buildScenes.Length - 1)
                SwapBuildScenes(i, i + 1);

            if (GUILayout.Button("X", GUILayout.Width(25)))
                RemoveSceneFromBuild(i);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (GUILayout.Button("Add Current Scene to Build"))
            AddCurrentSceneToBuild();
    }

    private void DrawAllScenes()
    {
        EditorGUILayout.LabelField("All Scenes", headerStyle);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (MatchesFilter(scenePath))
                DrawSceneButton(scenePath, false);
        }

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
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            bool isCurrentScene = scenePath == EditorSceneManager.GetActiveScene().path;

            bool isVisible = sceneVisibility[scenePath];
            bool newVisible = EditorGUILayout.Toggle(isVisible, GUILayout.Width(20));
            if (newVisible != isVisible)
            {
                sceneVisibility[scenePath] = newVisible;
                SaveVisibilityState();
            }

            Color originalColor = GUI.backgroundColor;
            if (isCurrentScene)
                GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button(new GUIContent(sceneName, scenePath), sceneButtonStyle))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                    AddToRecentScenes(scenePath);
                }
            }
            GUI.backgroundColor = originalColor;

            if (GUILayout.Button(isFavorite ? "★" : "☆", GUILayout.Width(25)))
                ToggleFavorite(scenePath);

            if (GUILayout.Button("⋮", GUILayout.Width(25)))
                ShowSceneContextMenu(scenePath);
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion

    #region Helper Methods

    private bool MatchesFilter(string scenePath)
    {
        return string.IsNullOrEmpty(searchFilter) ||
               scenePath.ToLower().Contains(searchFilter.ToLower());
    }

    private void AddToRecentScenes(string scenePath)
    {
        recentScenes.Remove(scenePath);
        recentScenes.Insert(0, scenePath);

        if (recentScenes.Count > MAX_RECENT)
            recentScenes.RemoveAt(recentScenes.Count - 1);

        SavePreferences();
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
        menu.AddItem(new GUIContent("Add to Build Settings"), false, () => AddSceneToBuild(scenePath));
        menu.AddItem(new GUIContent("Show in Project"), false, () => ShowSceneInProject(scenePath));
        menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = scenePath);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Toggle Favorite"), false, () => ToggleFavorite(scenePath));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Show/Hide Scene"), sceneVisibility[scenePath], () =>
        {
            sceneVisibility[scenePath] = !sceneVisibility[scenePath];
            SaveVisibilityState();
        });
        menu.ShowAsContext();
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
        var scenesList = EditorBuildSettings.scenes.ToList();
        scenesList.RemoveAt(index);
        EditorBuildSettings.scenes = scenesList.ToArray();
    }

    private void AddCurrentSceneToBuild()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        AddSceneToBuild(currentScenePath);
    }

    private void AddSceneToBuild(string scenePath)
    {
        var scenesList = EditorBuildSettings.scenes.ToList();
        if (!scenesList.Any(s => s.path == scenePath))
        {
            scenesList.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenesList.ToArray();
        }
    }

    private void ShowSceneInProject(string scenePath)
    {
        var sceneObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath);
        if (sceneObject != null)
        {
            EditorGUIUtility.PingObject(sceneObject);
            Selection.activeObject = sceneObject;
        }
    }

    #endregion
}
#endif