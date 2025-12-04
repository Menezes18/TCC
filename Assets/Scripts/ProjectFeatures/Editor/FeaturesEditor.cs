using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProjectFeatures.Editor
{
    /// <summary>
    /// Editor customizado para gerenciar features do projeto de forma visual.
    /// Abre em: Window → TCC → Features Manager
    /// </summary>
    public class FeaturesEditor : EditorWindow
    {
        private FeaturesDatabase database;
        private Vector2 scrollPosition;
        private Vector2 statsScrollPosition;
        
        // Aba selecionada
        private int selectedTab = 0;
        private string[] tabs = { "Features", "Estatísticas", "Criar Feature" };
        
        // Filtros
        private FeatureCategory? filterCategory = null;
        private FeatureStatus? filterStatus = null;
        private string searchText = "";
        
        // Nova feature (aba de criação)
        private string newFeatureTitulo = "Nova Feature";
        private FeatureCategory newFeatureCategoria = FeatureCategory.Geral;
        private string newFeatureTipo = "Sistema";
        private string newFeatureDescricaoCurta = "";
        private string newFeatureDescricaoLonga = "";
        private string newFeatureResponsavel = "Equipe TCC";
        private FeatureStatus newFeatureStatus = FeatureStatus.Concluido;
        private Sprite newFeatureIcone;
        private Sprite newFeatureScreenshot;
        private List<string> newFeatureTags = new List<string>();
        private string newTagInput = "";

        [MenuItem("Window/TCC/Features Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<FeaturesEditor>("Features Manager");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            // Tenta encontrar o database existente
            string[] guids = AssetDatabase.FindAssets("t:FeaturesDatabase");
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<FeaturesDatabase>(path);
            }
        }

        private void OnGUI()
        {
            // Header
            DrawHeader();

            // Verifica se tem database
            if (database == null)
            {
                DrawNoDatabaseWarning();
                return;
            }

            // Tabs
            EditorGUILayout.Space(10);
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            EditorGUILayout.Space(10);

            // Conteúdo baseado na tab
            switch (selectedTab)
            {
                case 0:
                    DrawFeaturesTab();
                    break;
                case 1:
                    DrawStatsTab();
                    break;
                case 2:
                    DrawCreateFeatureTab();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("Box");
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎯 Features Manager - TCC", titleStyle);
            
            EditorGUILayout.Space(5);
            
            // Campo do database
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Database:", GUILayout.Width(70));
            database = (FeaturesDatabase)EditorGUILayout.ObjectField(database, typeof(FeaturesDatabase), false);
            
            if (GUILayout.Button("Criar Novo", GUILayout.Width(100)))
            {
                CreateNewDatabase();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawNoDatabaseWarning()
        {
            EditorGUILayout.HelpBox(
                "Nenhum FeaturesDatabase encontrado!\n\n" +
                "Clique em 'Criar Novo' para começar.",
                MessageType.Warning
            );

            if (GUILayout.Button("Criar Database Agora", GUILayout.Height(40)))
            {
                CreateNewDatabase();
            }
        }

        private void DrawFeaturesTab()
        {
            EditorGUILayout.BeginVertical();

            // Filtros
            DrawFilters();

            EditorGUILayout.Space(10);

            // Lista de features
            DrawFeaturesList();

            EditorGUILayout.EndVertical();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("🔍 Filtros", EditorStyles.boldLabel);

            // Busca por texto
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buscar:", GUILayout.Width(60));
            searchText = EditorGUILayout.TextField(searchText);
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Filtro de categoria
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Categoria:", GUILayout.Width(80));
            
            if (GUILayout.Button(filterCategory.HasValue ? filterCategory.ToString() : "Todas"))
            {
                ShowCategoryMenu();
            }
            
            if (filterCategory.HasValue && GUILayout.Button("X", GUILayout.Width(30)))
            {
                filterCategory = null;
            }
            EditorGUILayout.EndHorizontal();

            // Filtro de status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(80));
            
            if (GUILayout.Button(filterStatus.HasValue ? GetStatusName(filterStatus.Value) : "Todos"))
            {
                ShowStatusMenu();
            }
            
            if (filterStatus.HasValue && GUILayout.Button("X", GUILayout.Width(30)))
            {
                filterStatus = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ShowCategoryMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Todas"), false, () => filterCategory = null);
            menu.AddSeparator("");
            
            foreach (FeatureCategory cat in System.Enum.GetValues(typeof(FeatureCategory)))
            {
                FeatureCategory c = cat; // Cópia local para closure
                menu.AddItem(new GUIContent(GetCategoryName(c)), false, () => filterCategory = c);
            }
            
            menu.ShowAsContext();
        }

        private void ShowStatusMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Todos"), false, () => filterStatus = null);
            menu.AddSeparator("");
            
            foreach (FeatureStatus status in System.Enum.GetValues(typeof(FeatureStatus)))
            {
                FeatureStatus s = status; // Cópia local para closure
                menu.AddItem(new GUIContent(GetStatusName(s)), false, () => filterStatus = s);
            }
            
            menu.ShowAsContext();
        }

        private void DrawFeaturesList()
        {
            var features = GetFilteredFeatures();

            EditorGUILayout.LabelField($"📋 Features ({features.Count})", EditorStyles.boldLabel);

            if (features.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhuma feature encontrada com os filtros atuais.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var feature in features)
            {
                DrawFeatureCard(feature);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFeatureCard(FeatureEntry feature)
        {
            EditorGUILayout.BeginVertical("Box");

            // Linha 1: Título + Status
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField($"📌 {feature.titulo}", titleStyle);
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
            statusStyle.normal.textColor = GetStatusColorEditor(feature.status);
            EditorGUILayout.LabelField($"[{feature.GetStatusNome()}]", statusStyle, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();

            // Linha 2: Categoria + Tipo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🏷️ {feature.GetCategoriaNome()} / {feature.tipo}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Linha 3: Descrição curta
            if (!string.IsNullOrEmpty(feature.descricaoCurta))
            {
                EditorGUILayout.LabelField(feature.descricaoCurta, EditorStyles.wordWrappedLabel);
            }

            // Linha 4: Responsável + Tags
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"👤 {feature.responsavel}", EditorStyles.miniLabel, GUILayout.Width(150));
            
            if (feature.tags != null && feature.tags.Length > 0)
            {
                EditorGUILayout.LabelField($"Tags: {string.Join(", ", feature.tags)}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            // Botões
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Editar", GUILayout.Width(60)))
            {
                Selection.activeObject = feature;
                EditorGUIUtility.PingObject(feature);
            }
            
            if (GUILayout.Button("Duplicar", GUILayout.Width(70)))
            {
                DuplicateFeature(feature);
            }
            
            if (GUILayout.Button("Deletar", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Deletar Feature", 
                    $"Tem certeza que deseja deletar '{feature.titulo}'?", "Sim", "Cancelar"))
                {
                    DeleteFeature(feature);
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawStatsTab()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("📊 Estatísticas do Projeto", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            var stats = database.GetStats();

            statsScrollPosition = EditorGUILayout.BeginScrollView(statsScrollPosition);

            // Total
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField($"Total de Features: {stats.totalFeatures}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Por status
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Por Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"✅ Concluídas: {stats.concluidas}");
            EditorGUILayout.LabelField($"🔄 Em Andamento: {stats.emAndamento}");
            EditorGUILayout.LabelField($"📋 Planejadas: {stats.planejadas}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Por categoria
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Por Categoria:", EditorStyles.boldLabel);
            foreach (var kvp in stats.porCategoria)
            {
                EditorGUILayout.LabelField($"{GetCategoryIcon(kvp.Key)} {GetCategoryName(kvp.Key)}: {kvp.Value}");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawCreateFeatureTab()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("➕ Criar Nova Feature", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Título
            EditorGUILayout.LabelField("Título:", EditorStyles.boldLabel);
            newFeatureTitulo = EditorGUILayout.TextField(newFeatureTitulo);

            EditorGUILayout.Space(5);

            // Categoria e Tipo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Categoria:", EditorStyles.boldLabel);
            newFeatureCategoria = (FeatureCategory)EditorGUILayout.EnumPopup(newFeatureCategoria);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Tipo:", EditorStyles.boldLabel);
            newFeatureTipo = EditorGUILayout.TextField(newFeatureTipo);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Descrição Curta
            EditorGUILayout.LabelField("Descrição Curta:", EditorStyles.boldLabel);
            newFeatureDescricaoCurta = EditorGUILayout.TextArea(newFeatureDescricaoCurta, GUILayout.Height(50));

            EditorGUILayout.Space(5);

            // Descrição Longa
            EditorGUILayout.LabelField("Descrição Detalhada (opcional):", EditorStyles.boldLabel);
            newFeatureDescricaoLonga = EditorGUILayout.TextArea(newFeatureDescricaoLonga, GUILayout.Height(80));

            EditorGUILayout.Space(5);

            // Responsável e Status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Responsável:", EditorStyles.boldLabel);
            newFeatureResponsavel = EditorGUILayout.TextField(newFeatureResponsavel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            newFeatureStatus = (FeatureStatus)EditorGUILayout.EnumPopup(newFeatureStatus);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Tags
            EditorGUILayout.LabelField("Tags:", EditorStyles.boldLabel);
            DrawTagsList();

            EditorGUILayout.Space(5);

            // Visuais
            EditorGUILayout.LabelField("Visuais (opcional):", EditorStyles.boldLabel);
            newFeatureIcone = (Sprite)EditorGUILayout.ObjectField("Ícone:", newFeatureIcone, typeof(Sprite), false);
            newFeatureScreenshot = (Sprite)EditorGUILayout.ObjectField("Screenshot:", newFeatureScreenshot, typeof(Sprite), false);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Botão criar
            if (GUILayout.Button("Criar Feature", GUILayout.Height(40)))
            {
                CreateNewFeature();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTagsList()
        {
            EditorGUILayout.BeginVertical("Box");
            
            // Tags existentes
            for (int i = 0; i < newFeatureTags.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {newFeatureTags[i]}");
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    newFeatureTags.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Adicionar nova tag
            EditorGUILayout.BeginHorizontal();
            newTagInput = EditorGUILayout.TextField(newTagInput);
            if (GUILayout.Button("Adicionar", GUILayout.Width(80)))
            {
                if (!string.IsNullOrWhiteSpace(newTagInput) && !newFeatureTags.Contains(newTagInput))
                {
                    newFeatureTags.Add(newTagInput);
                    newTagInput = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateNewFeature()
        {
            var feature = CreateInstance<FeatureEntry>();
            feature.titulo = newFeatureTitulo;
            feature.categoria = newFeatureCategoria;
            feature.tipo = newFeatureTipo;
            feature.descricaoCurta = newFeatureDescricaoCurta;
            feature.descricaoLonga = newFeatureDescricaoLonga;
            feature.responsavel = newFeatureResponsavel;
            feature.status = newFeatureStatus;
            feature.tags = newFeatureTags.ToArray();
            feature.icone = newFeatureIcone;
            feature.screenshot = newFeatureScreenshot;

            // Garante que a pasta existe
            string folderPath = "Assets/Features";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Features");
            }

            // Salva o asset
            string path = $"{folderPath}/{newFeatureTitulo}.asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(feature, uniquePath);

            // Adiciona ao database
            database.features.Add(feature);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Sucesso", $"Feature '{newFeatureTitulo}' criada com sucesso!", "OK");

            // Reset campos
            ResetNewFeatureFields();
            
            // Volta para aba de features
            selectedTab = 0;
        }

        private void ResetNewFeatureFields()
        {
            newFeatureTitulo = "Nova Feature";
            newFeatureCategoria = FeatureCategory.Geral;
            newFeatureTipo = "Sistema";
            newFeatureDescricaoCurta = "";
            newFeatureDescricaoLonga = "";
            newFeatureResponsavel = "Equipe TCC";
            newFeatureStatus = FeatureStatus.Concluido;
            newFeatureTags.Clear();
            newFeatureIcone = null;
            newFeatureScreenshot = null;
        }

        private void CreateNewDatabase()
        {
            var db = CreateInstance<FeaturesDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/FeaturesDatabase.asset");
            AssetDatabase.SaveAssets();
            database = db;
            EditorUtility.DisplayDialog("Sucesso", "Database criado em Assets/FeaturesDatabase.asset", "OK");
        }

        private void DuplicateFeature(FeatureEntry original)
        {
            var duplicate = Instantiate(original);
            duplicate.titulo = $"{original.titulo} (Cópia)";
            
            string path = $"Assets/Features/{duplicate.titulo}.asset";
            AssetDatabase.CreateAsset(duplicate, AssetDatabase.GenerateUniqueAssetPath(path));
            
            database.features.Add(duplicate);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void DeleteFeature(FeatureEntry feature)
        {
            database.features.Remove(feature);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(feature));
        }

        private List<FeatureEntry> GetFilteredFeatures()
        {
            var features = database.GetAllFeatures();

            // Filtro de busca
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                features = database.SearchFeatures(searchText);
            }

            // Filtro de categoria
            if (filterCategory.HasValue)
            {
                features = features.FindAll(f => f.categoria == filterCategory.Value);
            }

            // Filtro de status
            if (filterStatus.HasValue)
            {
                features = features.FindAll(f => f.status == filterStatus.Value);
            }

            return features;
        }

        // Helpers
        private string GetCategoryName(FeatureCategory cat)
        {
            return cat switch
            {
                FeatureCategory.Programacao => "Programação",
                FeatureCategory.Mecanica => "Mecânica",
                _ => cat.ToString()
            };
        }

        private string GetStatusName(FeatureStatus status)
        {
            return status switch
            {
                FeatureStatus.EmAndamento => "Em Andamento",
                FeatureStatus.Concluido => "Concluído",
                _ => status.ToString()
            };
        }

        private string GetCategoryIcon(FeatureCategory cat)
        {
            return cat switch
            {
                FeatureCategory.Programacao => "💻",
                FeatureCategory.Arte => "🎨",
                FeatureCategory.VFX => "✨",
                FeatureCategory.Mecanica => "⚙️",
                FeatureCategory.Geral => "📋",
                _ => "📌"
            };
        }

        private Color GetStatusColorEditor(FeatureStatus status)
        {
            return status switch
            {
                FeatureStatus.Planejado => new Color(1f, 0.8f, 0f),
                FeatureStatus.EmAndamento => new Color(0.3f, 0.7f, 1f),
                FeatureStatus.Concluido => new Color(0.3f, 0.9f, 0.3f),
                _ => Color.white
            };
        }
    }
}

