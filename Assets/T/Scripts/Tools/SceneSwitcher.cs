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

    private const int MAX_RECENT = 5;
    private Dictionary<string, bool> sceneVisibility = new Dictionary<string, bool>();

    private const string VISIBILITY_PREFS_KEY = "SceneManager_Visibility";
    private const string FAVORITES_PREFS_KEY = "SceneManager_Favorites";
    private const string RECENT_PREFS_KEY = "SceneManager_Recent";

    // UI Styles
    private GUIStyle headerStyle;
    private GUIStyle sceneButtonStyle;
    private GUIStyle categoryBoxStyle;
    private GUIStyle favoriteButtonStyle;
    private GUIStyle actionButtonStyle;
    private GUIContent favoriteOnContent;
    private GUIContent favoriteOffContent;
    private GUIContent menuContent;

    #endregion

    #region Menu & Window Setup

    [MenuItem("Tools/Scene Manager %#S")] // Ctrl/Cmd + Shift + S
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
    }

    private void LoadPreferences()
    {
        string favoritesString = EditorPrefs.GetString(FAVORITES_PREFS_KEY, "");

        favoriteScenes = string.IsNullOrEmpty(favoritesString) ? new List<string>() : favoritesString.Split('|').ToList();
    }

    private void ResetAllPreferences()
    {
        EditorPrefs.DeleteKey(VISIBILITY_PREFS_KEY);
        EditorPrefs.DeleteKey(FAVORITES_PREFS_KEY);
        EditorPrefs.DeleteKey(RECENT_PREFS_KEY);
        sceneVisibility.Clear();
        favoriteScenes.Clear();
    }

    #endregion

    #region GUI Styles


    private void InitializeStyles()
    {
        // Header style
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 0,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 5, 8, 8),
            margin = new RectOffset(5, 5, 5, 0)
        };
        // Scene button style - more compact and visually appealing
        sceneButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 5, 5, 5),
            margin = new RectOffset(0, 0, 1, 1),
            fixedHeight = 28
        };

        // Category box style
        categoryBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(5, 5, 5, 10)
        };

        // Button styles for actions
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

        // Create icons for buttons
        favoriteOnContent = new GUIContent("★", "Remove from Favorites");
        favoriteOffContent = new GUIContent("☆", "Add to Favorites");
        menuContent = new GUIContent("⋮", "Scene Options");
    }

    #endregion

    #region OnGUI

    private void OnGUI()
    {
        // Avoid layout errors while compiling
        if (EditorApplication.isCompiling)
            return;

        try
        {
            DrawToolbar();
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (showFavorites && favoriteScenes.Count > 0)
                DrawFavoriteScenes();

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

        // Search field
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
        
        string currentSceneName = EditorSceneManager.GetActiveScene().name;
        string displayName = string.IsNullOrEmpty(currentSceneName) ? "<No Scene>" : currentSceneName;
        
        EditorGUILayout.LabelField($"Current: {displayName}", EditorStyles.miniLabel);
        
        GUILayout.FlexibleSpace();
            
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Scene Draw Methods

    private void DrawFavoriteScenes()
    {
        EditorGUILayout.LabelField("★ Favorite Scenes", headerStyle);
        EditorGUILayout.BeginVertical(categoryBoxStyle);

        bool anyShown = false;
        foreach (string scenePath in favoriteScenes.ToList())
        {
            if (MatchesFilter(scenePath))
            {
                DrawSceneButton(scenePath, true);
                anyShown = true;
            }
        }

        if (!anyShown)
            EditorGUILayout.LabelField("No favorite scenes match the filter", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawBuildScenes()
    {
        EditorGUILayout.LabelField("🔨 Build Scenes", headerStyle);
        EditorGUILayout.BeginVertical(categoryBoxStyle);

        var buildScenes = EditorBuildSettings.scenes;
        bool anyShown = false;
        
        for (int i = 0; i < buildScenes.Length; i++)
        {
            var sceneSetting = buildScenes[i];
            if (!MatchesFilter(sceneSetting.path))
                continue;

            anyShown = true;
            EditorGUILayout.BeginHorizontal();

            // Visibility toggle
            if (!sceneVisibility.ContainsKey(sceneSetting.path))
                sceneVisibility[sceneSetting.path] = true;
                
            bool isVisible = sceneVisibility[sceneSetting.path];
            bool newVisible = EditorGUILayout.Toggle(isVisible, GUILayout.Width(16));
            if (newVisible != isVisible)
            {
                sceneVisibility[sceneSetting.path] = newVisible;
                SaveVisibilityState();
            }

            // Scene button with color indication for enabled state
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = sceneSetting.enabled ? new Color(0.7f, 1.0f, 0.7f) : new Color(0.8f, 0.8f, 0.8f);
            
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneSetting.path);
            bool isCurrentScene = sceneSetting.path == EditorSceneManager.GetActiveScene().path;
            
            if (isCurrentScene)
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
                
            if (GUILayout.Button(new GUIContent(sceneName, sceneSetting.path), sceneButtonStyle, GUILayout.ExpandWidth(true)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(sceneSetting.path);
                }
            }
            GUI.backgroundColor = originalColor;

            // Scene is enabled in build
            sceneSetting.enabled = EditorGUILayout.Toggle(sceneSetting.enabled, GUILayout.Width(16));
            EditorBuildSettings.scenes = buildScenes;

            // Move up
            if (GUILayout.Button("↑", GUILayout.Width(22)) && i > 0)
                SwapBuildScenes(i, i - 1);

            // Move down
            if (GUILayout.Button("↓", GUILayout.Width(22)) && i < buildScenes.Length - 1)
                SwapBuildScenes(i, i + 1);

            // Remove from build
            if (GUILayout.Button("✕", GUILayout.Width(22)))
                RemoveSceneFromBuild(i);

            // Favorite toggle
            bool isFavorite = favoriteScenes.Contains(sceneSetting.path);
            if (GUILayout.Button(isFavorite ? favoriteOnContent : favoriteOffContent, favoriteButtonStyle))
                ToggleFavorite(sceneSetting.path);

            EditorGUILayout.EndHorizontal();
        }

        if (!anyShown)
            EditorGUILayout.LabelField("No build scenes match the filter", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawAllScenes()
    {
        EditorGUILayout.BeginVertical(categoryBoxStyle);

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        bool anyShown = false;
        
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (MatchesFilter(scenePath))
            {
                DrawSceneButton(scenePath, false);
                anyShown = true;
            }
        }

        if (!anyShown)
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
            // Visibility toggle
            bool isVisible = sceneVisibility[scenePath];
            bool newVisible = EditorGUILayout.Toggle(isVisible, GUILayout.Width(16));
            if (newVisible != isVisible)
            {
                sceneVisibility[scenePath] = newVisible;
                SaveVisibilityState();
            }

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            bool isCurrentScene = scenePath == EditorSceneManager.GetActiveScene().path;
            bool isInBuild = EditorBuildSettings.scenes.Any(s => s.path == scenePath);

            // Set button color based on scene status
            Color originalColor = GUI.backgroundColor;
            if (isCurrentScene)
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f); // Light blue for current scene
            else if (isInBuild)
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f); // Light gray for build scenes

            // Get display name - add in build indicator
            string displayName = isInBuild ? $"{sceneName} [Build]" : sceneName;

            // Scene button
            if (GUILayout.Button(new GUIContent(displayName, scenePath), sceneButtonStyle, GUILayout.ExpandWidth(true)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
            GUI.backgroundColor = originalColor;

            // Favorite toggle
            if (GUILayout.Button(isFavorite ? favoriteOnContent : favoriteOffContent, favoriteButtonStyle))
                ToggleFavorite(scenePath);

            // Options menu
            if (GUILayout.Button(menuContent, actionButtonStyle))
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
        var scenes = EditorBuildSettings.scenes.ToList();
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
        var scenesList = EditorBuildSettings.scenes.ToList();
        scenesList.RemoveAt(index);
        EditorBuildSettings.scenes = scenesList.ToArray();
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