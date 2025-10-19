using UnityEngine;
using UnityEditor;
using LevelEditor;
using System.Collections.Generic;

namespace EditorTools
{
    /// <summary>
    /// Editor Window para criar níveis com hexágonos em formato circular
    /// </summary>
    public class HexagonLevelEditor : EditorWindow
    {
        private HexagonLevelData levelData;
        private GameObject previewParent;
        private List<GameObject> previewHexagons = new List<GameObject>();
        
        private Vector2 scrollPosition;
        private bool showHelp = true;
        
        // Configurações
        private float hexSize = 1f;
        private int circleRadius = 5;
        private GameObject hexagonPrefab;
        private float spacingMultiplier = 0.85f; // Mais próximas por padrão
        
        // Cache para detectar mudanças
        private float lastHexSize = 1f;
        private int lastCircleRadius = 5;
        private float lastSpacingMultiplier = 0.85f;
        
        // Ferramentas
        private enum Tool { Place, Remove, Fill }
        private Tool currentTool = Tool.Place;
        
        // Preview
        private bool autoPreview = true;
        private Color previewColor = new Color(1f, 1f, 1f, 0.5f);

        [MenuItem("Tools/Level Editor/Hexagon Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<HexagonLevelEditor>("Hexagon Level Editor");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            ClearPreview();
        }

        private void OnEditorUpdate()
        {
            // Força repaint da janela quando há mudanças
            if (levelData != null)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawLevelDataSection();
            DrawHexagonSettingsSection();
            DrawToolsSection();
            DrawActionsSection();
            DrawHelpSection();
            DrawStatsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Hexagon Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor para criar níveis com hexágonos em formato circular.\n" +
                "Use os botões abaixo para colocar, remover ou preencher hexágonos.",
                MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawLevelDataSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Level Data", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            levelData = (HexagonLevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(HexagonLevelData), false);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (levelData != null)
                {
                    hexSize = levelData.hexSize;
                    circleRadius = levelData.circleRadius;
                    hexagonPrefab = levelData.hexagonPrefab;
                    UpdatePreview();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Level Data", GUILayout.Height(30)))
            {
                CreateNewLevelData();
            }
            if (GUILayout.Button("Save Level Data", GUILayout.Height(30)))
            {
                SaveLevelData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawHexagonSettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Hexagon Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            hexagonPrefab = (GameObject)EditorGUILayout.ObjectField("Hexagon Prefab", hexagonPrefab, typeof(GameObject), false);
            hexSize = EditorGUILayout.Slider("Hex Size", hexSize, 0.1f, 5f);
            circleRadius = EditorGUILayout.IntSlider("Circle Radius", circleRadius, 1, 30);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Spacing Settings", EditorStyles.miniBoldLabel);
            spacingMultiplier = EditorGUILayout.Slider("Spacing Multiplier", spacingMultiplier, 0.5f, 2f);
            
            // Info mais detalhada
            if (spacingMultiplier < 0.8f)
                EditorGUILayout.HelpBox("⚠️ Muito próximas - pode sobrepor!", MessageType.Warning);
            else if (spacingMultiplier < 1.0f)
                EditorGUILayout.HelpBox("✓ Grudadas - CÍRCULO SUAVE! ⭕", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Espaçadas - bordas menos suaves", MessageType.None);

            bool settingsChanged = EditorGUI.EndChangeCheck();
            
            // Atualização em tempo real do preview
            if (settingsChanged)
            {
                if (levelData != null)
                {
                    levelData.hexagonPrefab = hexagonPrefab;
                    levelData.hexSize = hexSize;
                    levelData.circleRadius = circleRadius;
                    EditorUtility.SetDirty(levelData);
                }
                
                // Detecta mudanças que requerem recálculo
                bool needsRecalculation = (lastHexSize != hexSize || 
                                          lastCircleRadius != circleRadius || 
                                          lastSpacingMultiplier != spacingMultiplier);
                
                if (needsRecalculation && levelData != null && levelData.hexagons.Count > 0)
                {
                    RegenerateWithNewSettings();
                }
                
                // Atualiza cache
                lastHexSize = hexSize;
                lastCircleRadius = circleRadius;
                lastSpacingMultiplier = spacingMultiplier;
                
                // Força repaint da scene
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentTool == Tool.Place, "Place (P)", "Button", GUILayout.Height(40)))
                currentTool = Tool.Place;
            if (GUILayout.Toggle(currentTool == Tool.Remove, "Remove (R)", "Button", GUILayout.Height(40)))
                currentTool = Tool.Remove;
            if (GUILayout.Toggle(currentTool == Tool.Fill, "Fill Circle (F)", "Button", GUILayout.Height(40)))
                currentTool = Tool.Fill;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            autoPreview = EditorGUILayout.Toggle("Auto Preview", autoPreview);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Circle", GUILayout.Height(40)))
            {
                FillCircle();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Clear All", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Are you sure you want to clear all hexagons?", "Yes", "No"))
                {
                    ClearAllHexagons();
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            
            // Botão para instanciar DIRETO sem preview
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
            if (GUILayout.Button("⚡ Instanciar Direto (Sem Preview)", GUILayout.Height(45)))
            {
                FillCircle();
                GenerateLevelInScene();
                ClearAllHexagons();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Cria o círculo e instancia imediatamente na cena!", MessageType.Info);

            EditorGUILayout.Space(5);
            
            // Toggle para atualização automática
            EditorGUI.BeginChangeCheck();
            autoPreview = EditorGUILayout.Toggle("Auto Update Preview", autoPreview);
            if (EditorGUI.EndChangeCheck())
            {
                if (autoPreview && levelData != null && levelData.hexagons.Count > 0)
                {
                    SceneView.RepaintAll();
                }
            }
            
            if (!autoPreview)
            {
                EditorGUILayout.HelpBox("Preview automático desligado. Use 'Update Preview' manualmente.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Preview atualiza em TEMPO REAL ao mover sliders!", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
            if (GUILayout.Button("✓ Generate Level from Preview", GUILayout.Height(40)))
            {
                GenerateLevelInScene();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Gera objetos na cena baseado no preview atual", MessageType.None);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.BeginVertical("box");
            showHelp = EditorGUILayout.Foldout(showHelp, "Help & Controls", true);
            
            if (showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Keyboard Shortcuts:\n" +
                    "P - Place Tool | R - Remove Tool | F - Fill Circle\n\n" +
                    "✨ Novo Algoritmo CIRCULAR:\n" +
                    "• Usa distância EUCLIDIANA (√(x²+z²))\n" +
                    "• Cria círculo PERFEITO, não hexagonal\n" +
                    "• Bordas SUAVES e arredondadas\n\n" +
                    "Workflow Rápido:\n" +
                    "1. Configure: Radius, Size, Spacing\n" +
                    "2. ⚡ Instanciar Direto (sem preview)\n" +
                    "   OU\n" +
                    "2. Fill Circle → ajuste → Generate\n\n" +
                    "💡 Dica: Spacing 0.85 = círculo cheio e suave!",
                    MessageType.None);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawStatsSection()
        {
            if (levelData != null)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Total Hexagons:", levelData.hexagons.Count.ToString());
                EditorGUILayout.LabelField("Hex Size:", hexSize.ToString("F2") + " units");
                EditorGUILayout.LabelField("Object Scale:", (hexSize).ToString("F2") + "x");
                EditorGUILayout.LabelField("Spacing:", spacingMultiplier.ToString("F2"));
                EditorGUILayout.LabelField("Circle Radius:", circleRadius.ToString());
                
                // Calcula diâmetro exato do círculo
                float diameter = circleRadius * hexSize * spacingMultiplier * 2f;
                EditorGUILayout.LabelField("Circle Diameter:", diameter.ToString("F2") + " units");
                
                // Área do círculo
                float area = Mathf.PI * Mathf.Pow(circleRadius * hexSize * spacingMultiplier, 2f);
                EditorGUILayout.LabelField("Circle Area:", area.ToString("F1") + " units²");
                
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("⭕ Círculo EUCLIDIANO perfeito!\n✓ Bordas suaves e arredondadas!", MessageType.Info);
                
                EditorGUILayout.EndVertical();
            }
        }

        // Regenera o círculo com as novas configurações
        private void RegenerateWithNewSettings()
        {
            if (levelData == null || levelData.hexagons.Count == 0) return;

            // Salva as posições de grid atuais
            List<Vector2Int> gridPositions = new List<Vector2Int>();
            foreach (var hex in levelData.hexagons)
            {
                gridPositions.Add(hex.gridPosition);
            }

            // Limpa e recria com novos settings
            levelData.Clear();
            foreach (var gridPos in gridPositions)
            {
                Vector3 worldPos = HexagonTile.HexToWorldPosition(gridPos, hexSize * spacingMultiplier);
                levelData.AddHexagon(gridPos, worldPos);
            }

            EditorUtility.SetDirty(levelData);
            
            if (autoPreview)
            {
                UpdatePreview();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (levelData == null || hexagonPrefab == null)
                return;

            HandleKeyboardInput();
            HandleMouseInput();
            DrawSceneGUI();
        }

        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.P:
                        currentTool = Tool.Place;
                        e.Use();
                        Repaint();
                        break;
                    case KeyCode.R:
                        currentTool = Tool.Remove;
                        e.Use();
                        Repaint();
                        break;
                    case KeyCode.F:
                        currentTool = Tool.Fill;
                        e.Use();
                        Repaint();
                        break;
                }
            }
        }

        private void HandleMouseInput()
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    Vector2Int hexCoord = HexagonTile.WorldToHexPosition(hit.point, hexSize * spacingMultiplier);
                    
                    switch (currentTool)
                    {
                        case Tool.Place:
                            PlaceHexagon(hexCoord);
                            break;
                        case Tool.Remove:
                            RemoveHexagon(hexCoord);
                            break;
                        case Tool.Fill:
                            FillCircle();
                            break;
                    }
                    
                    e.Use();
                    if (autoPreview)
                        UpdatePreview();
                }
            }
        }

        private void DrawSceneGUI()
        {
            // Desenha preview dos hexágonos com gizmos
            if (levelData != null && levelData.hexagons.Count > 0)
            {
                DrawHexagonPreviews();
            }

            Handles.BeginGUI();
            
            GUILayout.BeginArea(new Rect(10, 10, 250, 120));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Hexagon Level Editor", EditorStyles.boldLabel);
            GUILayout.Label($"Tool: {currentTool}");
            GUILayout.Label($"Hexagons: {(levelData != null ? levelData.hexagons.Count : 0)}");
            GUILayout.Label($"Rotation: X=-90° (Deitado)");
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }

        private void DrawHexagonPreviews()
        {
            if (levelData == null) return;

            foreach (var hexData in levelData.hexagons)
            {
                // Desenha um hexágono wireframe na posição correta
                DrawHexagonWireframe(hexData.worldPosition, hexSize, Quaternion.Euler(-90f, 0f, 0f));
            }
        }

        private void DrawHexagonWireframe(Vector3 center, float size, Quaternion rotation)
        {
            // Define os 6 vértices de um hexágono regular (pointy-topped)
            Vector3[] vertices = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                // Pointy-topped: vértices começam a 30° e incrementam 60°
                float angle = (30f + 60f * i) * Mathf.Deg2Rad;
                float x = size * Mathf.Cos(angle);
                float z = size * Mathf.Sin(angle);
                // Cria o vértice no plano XZ (horizontal, deitado)
                Vector3 vertex = new Vector3(x, 0, z);
                // Move para a posição central
                vertices[i] = center + vertex;
            }

            // Desenha as linhas do hexágono
            Handles.color = new Color(1f, 0f, 0f, 0.9f); // Vermelho (como na sua imagem)
            for (int i = 0; i < 6; i++)
            {
                Handles.DrawLine(vertices[i], vertices[(i + 1) % 6], 3f);
            }

            // Desenha um ponto no centro
            Handles.color = new Color(1f, 0.5f, 0f, 0.9f); // Laranja
            Handles.SphereHandleCap(0, center, Quaternion.identity, size * 0.1f, EventType.Repaint);
        }

        private void PlaceHexagon(Vector2Int hexCoord)
        {
            if (levelData == null) return;

            // Aplica o multiplicador de espaçamento para evitar sobreposição
            Vector3 worldPos = HexagonTile.HexToWorldPosition(hexCoord, hexSize * spacingMultiplier);
            levelData.AddHexagon(hexCoord, worldPos);
            EditorUtility.SetDirty(levelData);
            SceneView.RepaintAll(); // Atualiza preview imediatamente
        }

        private void RemoveHexagon(Vector2Int hexCoord)
        {
            if (levelData == null) return;

            levelData.RemoveHexagon(hexCoord);
            EditorUtility.SetDirty(levelData);
            SceneView.RepaintAll(); // Atualiza preview imediatamente
        }

        private void FillCircle()
        {
            if (levelData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or select a Level Data first!", "OK");
                return;
            }

            levelData.Clear();

            // Preenche um CÍRCULO REAL usando distância euclidiana no espaço do mundo
            // Isso cria um círculo muito mais arredondado
            int searchRange = Mathf.CeilToInt(circleRadius * 1.5f); // Range ampliado
            float worldRadius = circleRadius * hexSize * spacingMultiplier; // Raio em unidades do mundo
            
            for (int q = -searchRange; q <= searchRange; q++)
            {
                for (int r = -searchRange; r <= searchRange; r++)
                {
                    Vector2Int hexCoord = new Vector2Int(q, r);
                    Vector3 worldPos = HexagonTile.HexToWorldPosition(hexCoord, hexSize * spacingMultiplier);
                    
                    // Calcula distância EUCLIDIANA do centro no plano XZ
                    // Isso cria um círculo perfeito, não um hexágono
                    float distanceFromCenter = Mathf.Sqrt(worldPos.x * worldPos.x + worldPos.z * worldPos.z);
                    
                    // Adiciona apenas hexágonos dentro do raio circular
                    if (distanceFromCenter <= worldRadius)
                    {
                        levelData.AddHexagon(hexCoord, worldPos);
                    }
                }
            }

            EditorUtility.SetDirty(levelData);
            if (autoPreview)
                UpdatePreview();
            
            // Força repaint da SceneView para mostrar os wireframes
            SceneView.RepaintAll();
            
            float radiusUnits = circleRadius * hexSize * spacingMultiplier;
            Debug.Log($"✓ Created CIRCULAR pattern with {levelData.hexagons.Count} hexagons (Radius: {radiusUnits:F2} units, Euclidean distance)");
        }

        private void ClearAllHexagons()
        {
            if (levelData == null) return;

            levelData.Clear();
            EditorUtility.SetDirty(levelData);
            ClearPreview();
        }

        private void UpdatePreview()
        {
            ClearPreview();

            if (levelData == null || hexagonPrefab == null)
                return;

            previewParent = new GameObject("__HexagonPreview__");
            previewParent.hideFlags = HideFlags.DontSave;

            foreach (var hexData in levelData.hexagons)
            {
                GameObject preview = PrefabUtility.InstantiatePrefab(hexagonPrefab) as GameObject;
                preview.transform.position = hexData.worldPosition;
                
                // USA A MESMA ROTAÇÃO que será usada na geração final: -90° no X
                preview.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                
                // Aplica a escala do hexSize (proporcional ao tamanho configurado)
                float scaleFactor = hexSize;
                preview.transform.localScale = Vector3.one * scaleFactor;
                
                preview.transform.SetParent(previewParent.transform);
                preview.hideFlags = HideFlags.DontSave;

                // Torna o preview semi-transparente
                MeshRenderer[] renderers = preview.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    Material[] mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = new Material(mats[i]);
                        mats[i].color = previewColor;
                    }
                    renderer.sharedMaterials = mats;
                }

                previewHexagons.Add(preview);
            }

            SceneView.RepaintAll();
        }

        private void ClearPreview()
        {
            if (previewParent != null)
            {
                DestroyImmediate(previewParent);
            }
            previewHexagons.Clear();
        }

        private void GenerateLevelInScene()
        {
            if (levelData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or select a Level Data first!", "OK");
                return;
            }

            if (hexagonPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Hexagon Prefab!", "OK");
                return;
            }

            // Cria um parent para organizar
            GameObject levelParent = new GameObject($"Level_{levelData.levelName}");
            Undo.RegisterCreatedObjectUndo(levelParent, "Generate Hexagon Level");

            foreach (var hexData in levelData.hexagons)
            {
                GameObject hex = PrefabUtility.InstantiatePrefab(hexagonPrefab) as GameObject;
                hex.transform.position = hexData.worldPosition;
                
                // EXATAMENTE A MESMA ROTAÇÃO DO PREVIEW: -90° no X
                hex.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                
                // EXATAMENTE A MESMA ESCALA DO PREVIEW
                float scaleFactor = hexSize;
                hex.transform.localScale = Vector3.one * scaleFactor;
                
                hex.transform.SetParent(levelParent.transform);

                HexagonTile tile = hex.GetComponent<HexagonTile>();
                if (tile == null)
                    tile = hex.AddComponent<HexagonTile>();
                
                tile.GridPosition = hexData.gridPosition;

                Undo.RegisterCreatedObjectUndo(hex, "Generate Hexagon Level");
            }

            Selection.activeGameObject = levelParent;
            EditorGUIUtility.PingObject(levelParent);
            Debug.Log($"✓ Generated level with {levelData.hexagons.Count} hexagons at {levelParent.name}!");
        }

        private void CreateNewLevelData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Hexagon Level Data",
                "NewHexagonLevel",
                "asset",
                "Choose a location to save the level data");

            if (!string.IsNullOrEmpty(path))
            {
                HexagonLevelData newData = CreateInstance<HexagonLevelData>();
                newData.hexSize = hexSize;
                newData.circleRadius = circleRadius;
                newData.hexagonPrefab = hexagonPrefab;

                AssetDatabase.CreateAsset(newData, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                levelData = newData;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newData;
            }
        }

        private void SaveLevelData()
        {
            if (levelData != null)
            {
                EditorUtility.SetDirty(levelData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Level Data saved!");
            }
        }
    }
}
