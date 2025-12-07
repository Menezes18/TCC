using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ProjectFeatures.Editor
{
    /// <summary>
    /// Wizard completo e profissional para criar o painel de Features.
    /// 
    /// Características:
    /// - Validação completa antes de criar
    /// - Suporte robusto para Canvas 2D e 3D
    /// - Configurações customizáveis
    /// - Preview e logs detalhados
    /// - Opção de desfazer
    /// 
    /// Menu: Window → TCC → Features Panel Wizard
    /// </summary>
    public class FeaturesPanelWizard : EditorWindow
    {
        #region Configurações

        private enum PanelSize
        {
            HD_1920x1080,
            FullHD_1920x1080,
            QHD_2560x1440,
            UHD_3840x2160,
            Custom
        }

        // Configurações do usuário
        private Canvas targetCanvas;
        private PanelSize panelSize = PanelSize.HD_1920x1080;
        private Vector2 customSize = new Vector2(1920, 1080);
        private bool createPrefab = true;
        private bool autoConnectDatabase = true;
        private FeaturesDatabase database;
        private bool createWithTestData = true; // NOVO: Criar com dados de teste
        private int testFeaturesCount = 5; // NOVO: Quantidade de features de teste

        // Estado
        private Vector2 scrollPosition;
        private GameObject createdPanel;
        private bool hasCreated = false;

        #endregion

        #region Menu

        [MenuItem("Window/TCC/Features Panel Wizard")]
        public static void ShowWizard()
        {
            var window = GetWindow<FeaturesPanelWizard>("Features Panel Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawCanvasSelection();
            EditorGUILayout.Space(10);

            DrawSizeConfiguration();
            EditorGUILayout.Space(10);

            DrawDatabaseConfiguration();
            EditorGUILayout.Space(10);

            DrawOptions();
            EditorGUILayout.Space(10);

            DrawTestDataOptions();
            EditorGUILayout.Space(10);

            DrawPreview();
            EditorGUILayout.Space(10);

            DrawValidation();
            EditorGUILayout.Space(10);

            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("Box");

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎯 Features Panel Wizard", titleStyle);

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField(
                "Configure e crie o painel de features com validações completas",
                subtitleStyle
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasSelection()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("📺 Canvas Alvo", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Selecione o Canvas onde o painel será criado.\n" +
                "Suporta Canvas 2D (Overlay/Camera) e 3D (World Space).",
                MessageType.Info
            );

            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Canvas:", targetCanvas, typeof(Canvas), true);

            if (targetCanvas != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Informações do Canvas:", EditorStyles.boldLabel);

                string renderMode = targetCanvas.renderMode.ToString();
                EditorGUILayout.LabelField($"• Render Mode: {renderMode}");

                RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    EditorGUILayout.LabelField($"• Tamanho: {canvasRect.rect.width} x {canvasRect.rect.height}");
                }

                if (targetCanvas.renderMode == RenderMode.WorldSpace)
                {
                    EditorGUILayout.HelpBox(
                        "⚠️ Canvas 3D detectado!\n" +
                        "O painel será criado com tamanho fixo e anchors centralizados.",
                        MessageType.Warning
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Selecione um Canvas para continuar.", MessageType.Warning);

                if (GUILayout.Button("🔍 Buscar Canvas na Cena"))
                {
                    Canvas[] canvases = FindObjectsOfType<Canvas>();
                    if (canvases.Length > 0)
                    {
                        targetCanvas = canvases[0];
                        Debug.Log($"[Wizard] Canvas encontrado: {targetCanvas.name}");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Nenhum Canvas", "Nenhum Canvas encontrado na cena.", "OK");
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSizeConfiguration()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("📐 Tamanho do Painel", EditorStyles.boldLabel);

            panelSize = (PanelSize)EditorGUILayout.EnumPopup("Preset:", panelSize);

            Vector2 size = GetSizeFromPreset();

            if (panelSize == PanelSize.Custom)
            {
                customSize = EditorGUILayout.Vector2Field("Tamanho Custom:", customSize);
                size = customSize;
            }

            EditorGUILayout.LabelField($"Tamanho final: {size.x} x {size.y} pixels");

            EditorGUILayout.EndVertical();
        }

        private void DrawDatabaseConfiguration()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("💾 Database", EditorStyles.boldLabel);

            database = (FeaturesDatabase)EditorGUILayout.ObjectField(
                "Features Database:",
                database,
                typeof(FeaturesDatabase),
                false
            );

            autoConnectDatabase = EditorGUILayout.Toggle("Auto-conectar Database", autoConnectDatabase);

            if (database == null && autoConnectDatabase)
            {
                EditorGUILayout.HelpBox(
                    "Nenhum database selecionado. O painel será criado sem database conectado.",
                    MessageType.Warning
                );

                if (GUILayout.Button("🔍 Buscar Database"))
                {
                    string[] guids = AssetDatabase.FindAssets("t:FeaturesDatabase");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        database = AssetDatabase.LoadAssetAtPath<FeaturesDatabase>(path);
                        Debug.Log($"[Wizard] Database encontrado: {path}");
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog(
                            "Database não encontrado",
                            "Nenhum FeaturesDatabase encontrado. Deseja criar um novo?",
                            "Sim", "Não"))
                        {
                            CreateNewDatabase();
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("⚙️ Opções", EditorStyles.boldLabel);

            createPrefab = EditorGUILayout.Toggle("Criar Prefab do Card", createPrefab);

            if (createPrefab)
            {
                EditorGUILayout.HelpBox(
                    "Um prefab do FeatureCard será criado em Assets/Prefabs/",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTestDataOptions()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("🧪 Dados de Teste", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Criar com dados de teste permite visualizar imediatamente como ficará o painel!\n" +
                "Features de exemplo serão criadas e exibidas automaticamente.",
                MessageType.Info
            );

            createWithTestData = EditorGUILayout.Toggle("Criar com Dados de Teste", createWithTestData);

            if (createWithTestData)
            {
                testFeaturesCount = EditorGUILayout.IntSlider(
                    "Quantidade de Features:",
                    testFeaturesCount,
                    3, 10
                );

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Features que serão criadas:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Sistema de Multiplayer Mirror");
                EditorGUILayout.LabelField("• Mecânica de Dash");
                EditorGUILayout.LabelField("• Sistema de Save/Load");
                EditorGUILayout.LabelField("• Menu Principal Customizado");
                EditorGUILayout.LabelField("• Sistema de Partículas VFX");
                if (testFeaturesCount > 5)
                {
                    EditorGUILayout.LabelField($"• + {testFeaturesCount - 5} features adicionais");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("👁️ Preview da Estrutura", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Estrutura que será criada:",
                MessageType.Info
            );

            GUIStyle treeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                richText = true
            };

            Vector2 size = GetSizeFromPreset();
            string canvasType = targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace 
                ? "Canvas 3D (World Space)" 
                : "Canvas 2D";

            EditorGUILayout.LabelField($"<b>{canvasType}</b>", treeStyle);
            EditorGUILayout.LabelField($"└─ <b>FeaturesPanel</b> ({size.x}x{size.y})", treeStyle);
            EditorGUILayout.LabelField("   ├─ Header (150px altura)", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ TituloText (28pt, bold)", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ DescricaoText (16pt)", treeStyle);
            EditorGUILayout.LabelField("   │  └─ StatsText (14pt)", treeStyle);
            EditorGUILayout.LabelField("   ├─ FilterButtons (60px altura)", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ BtnTudo", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ BtnProgramação", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ BtnArte", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ BtnVFX", treeStyle);
            EditorGUILayout.LabelField("   │  ├─ BtnMecânica", treeStyle);
            EditorGUILayout.LabelField("   │  └─ BtnGeral", treeStyle);
            EditorGUILayout.LabelField("   └─ ScrollView", treeStyle);
            EditorGUILayout.LabelField("      └─ Viewport", treeStyle);
            EditorGUILayout.LabelField("         └─ Content (Vertical Layout)", treeStyle);

            if (createWithTestData)
            {
                EditorGUILayout.LabelField($"            ├─ FeatureCard x{testFeaturesCount} (criados)", treeStyle);
            }
            else
            {
                EditorGUILayout.LabelField("            └─ (vazio - adicione features)", treeStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidation()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("✅ Validação", EditorStyles.boldLabel);

            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            // Validações
            if (targetCanvas == null)
                errors.Add("Canvas não selecionado");

            if (database == null && autoConnectDatabase)
                warnings.Add("Database não selecionado - será necessário conectar manualmente");

            if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                warnings.Add("Canvas 3D detectado - certifique-se de ajustar a escala após criar");
            }

            // Mostra erros
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    EditorGUILayout.HelpBox($"❌ {error}", MessageType.Error);
                }
            }

            // Mostra avisos
            if (warnings.Count > 0)
            {
                foreach (string warning in warnings)
                {
                    EditorGUILayout.HelpBox($"⚠️ {warning}", MessageType.Warning);
                }
            }

            // Tudo OK
            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ Tudo pronto para criar!", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical("Box");

            bool canCreate = targetCanvas != null && !hasCreated;

            EditorGUI.BeginDisabledGroup(!canCreate);

            if (GUILayout.Button("🚀 Criar Painel de Features", GUILayout.Height(50)))
            {
                CreatePanel();
            }

            EditorGUI.EndDisabledGroup();

            if (hasCreated && createdPanel != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("✅ Painel criado com sucesso!", MessageType.Info);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("📍 Selecionar Painel"))
                {
                    Selection.activeGameObject = createdPanel;
                    EditorGUIUtility.PingObject(createdPanel);
                }

                if (GUILayout.Button("🗑️ Deletar e Refazer"))
                {
                    if (EditorUtility.DisplayDialog(
                        "Deletar Painel",
                        "Tem certeza que deseja deletar o painel criado?",
                        "Sim", "Cancelar"))
                    {
                        DestroyImmediate(createdPanel);
                        hasCreated = false;
                        createdPanel = null;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Logic

        private Vector2 GetSizeFromPreset()
        {
            return panelSize switch
            {
                PanelSize.HD_1920x1080 => new Vector2(1920, 1080),
                PanelSize.FullHD_1920x1080 => new Vector2(1920, 1080),
                PanelSize.QHD_2560x1440 => new Vector2(2560, 1440),
                PanelSize.UHD_3840x2160 => new Vector2(3840, 2160),
                PanelSize.Custom => customSize,
                _ => new Vector2(1920, 1080)
            };
        }

        private void CreatePanel()
        {
            Debug.Log("[Wizard] ========== Iniciando criação do painel ==========");

            // Cria prefab se necessário
            GameObject cardPrefab = null;
            if (createPrefab)
            {
                Debug.Log("[Wizard] Criando prefab do FeatureCard...");
                cardPrefab = CreateFeatureCardPrefab();
            }

            // Cria o painel
            Debug.Log($"[Wizard] Criando painel no Canvas: {targetCanvas.name}");
            createdPanel = CreateFeaturesPanel(targetCanvas.transform);

            // Conecta TUDO automaticamente usando SerializedObject
            Debug.Log("[Wizard] Conectando todas as referências automaticamente...");
            ConnectAllReferences(createdPanel, cardPrefab);

            // Conecta botões aos métodos
            Debug.Log("[Wizard] Conectando botões de filtro...");
            ConnectFilterButtons(createdPanel);

            // Cria dados de teste se configurado
            if (createWithTestData)
            {
                Debug.Log("[Wizard] Criando dados de teste...");
                CreateTestFeatures(cardPrefab);
            }

            hasCreated = true;

            Debug.Log("[Wizard] ========== Painel criado com sucesso! ==========");
            EditorUtility.DisplayDialog(
                "Sucesso!",
                "Painel de Features criado com sucesso!\n\n" +
                "Verifique a Hierarchy e configure conforme necessário.",
                "OK"
            );
        }

        private GameObject CreateFeaturesPanel(Transform parent)
        {
            Vector2 size = GetSizeFromPreset();
            bool isWorldSpace = targetCanvas.renderMode == RenderMode.WorldSpace;

            // Container principal
            GameObject panel = new GameObject("FeaturesPanel");
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();

            // Configura baseado no tipo de Canvas
            if (isWorldSpace)
            {
                // Canvas 3D: centralizado com tamanho fixo
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = size;
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.localRotation = Quaternion.identity; // Garante rotação zerada
                Debug.Log($"[Wizard] Canvas 3D: tamanho fixo {size}");
            }
            else
            {
                // Canvas 2D: fullscreen
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.localRotation = Quaternion.identity;
                Debug.Log("[Wizard] Canvas 2D: fullscreen");
            }

            // Background
            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Componentes
            CreateSimpleHeader(panel.transform, size);
            CreateSimpleFilterButtons(panel.transform, size);
            CreateSimpleScrollView(panel.transform, size);
            GameObject popup = CreateDetailPopup(panel.transform, size);

            // Script
            FeaturesPanel script = panel.AddComponent<FeaturesPanel>();

            Debug.Log($"[Wizard] Painel criado: {panel.name}");
            return panel;
        }

        private void CreateSimpleHeader(Transform parent, Vector2 panelSize)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            RectTransform rect = header.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-40, 150);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.localRotation = Quaternion.identity;

            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Título
            CreateSimpleText(header.transform, "TituloText", "Features do Projeto - TCC", 
                new Vector2(0, -30), new Vector2(-40, 40), 28, FontStyles.Bold);

            // Descrição  
            CreateSimpleText(header.transform, "DescricaoText", "Confira todas as funcionalidades",
                new Vector2(0, -70), new Vector2(-40, 30), 16, FontStyles.Normal);

            // Stats
            CreateSimpleText(header.transform, "StatsText", "Total: 0",
                new Vector2(0, -110), new Vector2(-40, 30), 14, FontStyles.Normal);
        }

        private void CreateSimpleFilterButtons(Transform parent, Vector2 panelSize)
        {
            GameObject filterPanel = new GameObject("FilterButtons");
            filterPanel.transform.SetParent(parent, false);
            RectTransform rect = filterPanel.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-40, 60);
            rect.anchoredPosition = new Vector2(0, -190);
            rect.localRotation = Quaternion.identity;

            HorizontalLayoutGroup layout = filterPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Botões
            string[] labels = { "Tudo", "Programação", "Arte", "VFX", "Mecânica", "Geral" };
            foreach (string label in labels)
            {
                CreateSimpleButton(filterPanel.transform, label);
            }
        }

        private void CreateSimpleButton(Transform parent, string label)
        {
            GameObject btn = new GameObject($"Btn{label}");
            btn.transform.SetParent(parent, false);
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);
            rect.localRotation = Quaternion.identity;

            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.3f, 0.6f, 1f);

            Button button = btn.AddComponent<Button>();

            CreateSimpleText(btn.transform, "Text", label, Vector2.zero, Vector2.zero, 16, FontStyles.Bold, true);
        }

        private void CreateSimpleScrollView(Transform parent, Vector2 panelSize)
        {
            GameObject scroll = new GameObject("ScrollView");
            scroll.transform.SetParent(parent, false);
            RectTransform rect = scroll.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(40, 40);
            rect.offsetMax = new Vector2(-40, -270);
            rect.localRotation = Quaternion.identity;

            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroll.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpRect.localRotation = Quaternion.identity;

            // RectMask2D é mais performático que Mask
            viewport.AddComponent<UnityEngine.UI.RectMask2D>();

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.localRotation = Quaternion.identity;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        private GameObject CreateDetailPopup(Transform parent, Vector2 panelSize)
        {
            // Popup principal (fica por cima de tudo)
            GameObject popup = new GameObject("DetailPopup");
            popup.transform.SetParent(parent, false);
            RectTransform popupRect = popup.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.sizeDelta = Vector2.zero;
            popupRect.localRotation = Quaternion.identity;

            // Background escurecido (overlay)
            Image bgOverlay = popup.AddComponent<Image>();
            bgOverlay.color = new Color(0, 0, 0, 0.8f);

            // Popup content (centralizado)
            GameObject content = new GameObject("PopupContent");
            content.transform.SetParent(popup.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(800, 600);
            contentRect.localRotation = Quaternion.identity;

            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Header do popup
            GameObject header = new GameObject("Header");
            header.transform.SetParent(content.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 80);
            headerRect.localRotation = Quaternion.identity;

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Ícone (header)
            GameObject icone = new GameObject("IconeImage");
            icone.transform.SetParent(header.transform, false);
            RectTransform iconeRect = icone.AddComponent<RectTransform>();
            iconeRect.anchoredPosition = new Vector2(50, -40);
            iconeRect.sizeDelta = new Vector2(60, 60);
            iconeRect.localRotation = Quaternion.identity;
            Image iconeImg = icone.AddComponent<Image>();
            iconeImg.color = Color.white;

            // Título (header)
            CreateSimpleText(header.transform, "TituloText", "Feature", 
                new Vector2(120, -25), new Vector2(500, 30), 24, FontStyles.Bold);

            // Categoria + Tipo (header)
            CreateSimpleText(header.transform, "CategoriaText", "Categoria", 
                new Vector2(120, -55), new Vector2(200, 20), 14, FontStyles.Normal);
            CreateSimpleText(header.transform, "TipoText", "Tipo", 
                new Vector2(330, -55), new Vector2(200, 20), 14, FontStyles.Normal);

            // Botão fechar (header)
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform, false);
            RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.sizeDelta = new Vector2(60, 60);
            closeBtnRect.anchoredPosition = new Vector2(-10, -10);
            closeBtnRect.localRotation = Quaternion.identity;

            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeBtnComp = closeBtn.AddComponent<Button>();

            CreateSimpleText(closeBtn.transform, "Text", "✕", Vector2.zero, Vector2.zero, 30, FontStyles.Bold, true);

            // Scroll area para descrição e screenshot
            GameObject scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(content.transform, false);
            RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 100); // bottom, left
            scrollRect.offsetMax = new Vector2(-20, -80); // top, right
            scrollRect.localRotation = Quaternion.identity;

            // Descrição longa
            CreateSimpleText(scrollArea.transform, "DescricaoLongaText", "Descrição detalhada da feature...",
                new Vector2(0, -20), new Vector2(-40, 200), 16, FontStyles.Normal);

            // Screenshot container
            GameObject screenshotContainer = new GameObject("ScreenshotContainer");
            screenshotContainer.transform.SetParent(scrollArea.transform, false);
            RectTransform ssRect = screenshotContainer.AddComponent<RectTransform>();
            ssRect.anchoredPosition = new Vector2(0, -240);
            ssRect.sizeDelta = new Vector2(700, 400);
            ssRect.localRotation = Quaternion.identity;

            GameObject screenshot = new GameObject("ScreenshotImage");
            screenshot.transform.SetParent(screenshotContainer.transform, false);
            RectTransform ssImgRect = screenshot.AddComponent<RectTransform>();
            ssImgRect.anchorMin = Vector2.zero;
            ssImgRect.anchorMax = Vector2.one;
            ssImgRect.sizeDelta = Vector2.zero;
            ssImgRect.localRotation = Quaternion.identity;

            Image ssImg = screenshot.AddComponent<Image>();
            ssImg.color = Color.white;
            ssImg.preserveAspect = true;

            // Footer com info
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(content.transform, false);
            RectTransform footerRect = footer.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(0, 80);
            footerRect.localRotation = Quaternion.identity;

            Image footerBg = footer.AddComponent<Image>();
            footerBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            CreateSimpleText(footer.transform, "ResponsavelText", "Responsável: Equipe",
                new Vector2(20, 45), new Vector2(400, 25), 14, FontStyles.Normal);

            CreateSimpleText(footer.transform, "StatusText", "Concluído",
                new Vector2(-20, 45), new Vector2(200, 30), 16, FontStyles.Bold);

            CreateSimpleText(footer.transform, "TagsText", "Tags: Tag1, Tag2",
                new Vector2(20, 20), new Vector2(700, 20), 12, FontStyles.Normal);

            // Script do popup
            FeatureDetailPopup popupScript = popup.AddComponent<FeatureDetailPopup>();

            Debug.Log("[Wizard] Popup de detalhes criado");

            return popup;
        }

        private GameObject CreateSimpleText(Transform parent, string name, string text, 
            Vector2 pos, Vector2 size, float fontSize, FontStyles style, bool fullScreen = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();

            if (fullScreen)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
            }

            // CRÍTICO: Garante rotação e escala corretas
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return obj;
        }

        private void CreateTestFeatures(GameObject cardPrefab)
        {
            if (cardPrefab == null)
            {
                Debug.LogWarning("[Wizard] Não é possível criar features de teste sem o prefab do card.");
                return;
            }

            // Encontra o Content do ScrollView
            Transform content = createdPanel.transform.Find("ScrollView/Viewport/Content");
            if (content == null)
            {
                Debug.LogError("[Wizard] Content não encontrado!");
                return;
            }

            // Features de exemplo
            var testFeatures = new[]
            {
                new { titulo = "Sistema de Multiplayer Mirror", categoria = "Programação", tipo = "Sistema", descricao = "Gerencia conexão P2P, sincronização de players e cenas multiplayer usando Mirror Networking.", responsavel = "Gustavo Menezes", status = "Concluído" },
                new { titulo = "Mecânica de Dash", categoria = "Mecânica", tipo = "Movimento", descricao = "Permite ao jogador dar um dash rápido em qualquer direção com cooldown e partículas.", responsavel = "João Silva", status = "Concluído" },
                new { titulo = "Sistema de Save/Load", categoria = "Programação", tipo = "Sistema", descricao = "Salva e carrega progresso do jogador usando JSON e PlayerPrefs com criptografia.", responsavel = "Maria Santos", status = "Concluído" },
                new { titulo = "Menu Principal Customizado", categoria = "Geral", tipo = "UI", descricao = "Interface inicial com animações, opções de configuração e integração com Steam.", responsavel = "Pedro Oliveira", status = "Concluído" },
                new { titulo = "Sistema de Partículas VFX", categoria = "VFX", tipo = "Efeito Visual", descricao = "Efeitos visuais para ataques, impactos e habilidades especiais dos personagens.", responsavel = "Ana Pereira", status = "Em Andamento" },
                new { titulo = "Modelagem de Personagens", categoria = "Arte", tipo = "Modelagem 3D", descricao = "Criação de 4 personagens jogáveis com rigging e animações básicas.", responsavel = "Carlos Lima", status = "Concluído" },
                new { titulo = "Sistema de Audio Manager", categoria = "Programação", tipo = "Sistema", descricao = "Gerenciamento de música, SFX e mixagem com transições suaves.", responsavel = "Fernanda Costa", status = "Concluído" },
                new { titulo = "Minigame: Simon Says", categoria = "Mecânica", tipo = "Minigame", descricao = "Minigame de sequência de cores com dificuldade progressiva.", responsavel = "Roberto Alves", status = "Concluído" },
                new { titulo = "Shader de Água Cartoon", categoria = "VFX", tipo = "Shader", descricao = "Shader customizado para água com estilo cartoon e ondulações.", responsavel = "Juliana Souza", status = "Em Andamento" },
                new { titulo = "Sistema de Achievements", categoria = "Geral", tipo = "Sistema", descricao = "Conquistas desbloqueáveis com progresso e notificações.", responsavel = "Lucas Martins", status = "Planejado" },
            };

            // Cria os cards
            for (int i = 0; i < Mathf.Min(testFeaturesCount, testFeatures.Length); i++)
            {
                var feature = testFeatures[i];

                GameObject cardInstance = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, content);

                // Preenche o card
                Transform tituloText = cardInstance.transform.Find("TituloText");
                if (tituloText != null)
                {
                    TextMeshProUGUI tmp = tituloText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = feature.titulo;
                }

                Transform categoriaText = cardInstance.transform.Find("CategoriaText");
                if (categoriaText != null)
                {
                    TextMeshProUGUI tmp = categoriaText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = feature.categoria;
                }

                Transform tipoText = cardInstance.transform.Find("TipoText");
                if (tipoText != null)
                {
                    TextMeshProUGUI tmp = tipoText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = feature.tipo;
                }

                Transform descricaoText = cardInstance.transform.Find("DescricaoText");
                if (descricaoText != null)
                {
                    TextMeshProUGUI tmp = descricaoText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = feature.descricao;
                }

                Transform responsavelText = cardInstance.transform.Find("ResponsavelText");
                if (responsavelText != null)
                {
                    TextMeshProUGUI tmp = responsavelText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = $"Por: {feature.responsavel}";
                }

                Transform statusText = cardInstance.transform.Find("StatusText");
                if (statusText != null)
                {
                    TextMeshProUGUI tmp = statusText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = feature.status;
                        tmp.color = feature.status == "Concluído" ? new Color(0.3f, 1f, 0.3f) :
                                    feature.status == "Em Andamento" ? new Color(0.3f, 0.7f, 1f) :
                                    new Color(1f, 0.8f, 0.2f);
                    }
                }

                // Cor da barra de status
                Transform statusBar = cardInstance.transform.Find("StatusBar");
                if (statusBar != null)
                {
                    Image img = statusBar.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = feature.status == "Concluído" ? new Color(0.3f, 1f, 0.3f) :
                                    feature.status == "Em Andamento" ? new Color(0.3f, 0.7f, 1f) :
                                    new Color(1f, 0.8f, 0.2f);
                    }
                }

                Debug.Log($"[Wizard] Card de teste criado: {feature.titulo}");
            }

            Debug.Log($"[Wizard] {testFeaturesCount} cards de teste criados!");
        }

        private GameObject CreateFeatureCardPrefab()
        {
            // Garante pasta
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Card principal
            GameObject card = new GameObject("FeatureCard");
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(750, 160);
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;

            // Background e Button (para poder clicar)
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.18f, 0.18f, 0.19f, 1f);

            Button cardButton = card.AddComponent<Button>();
            cardButton.targetGraphic = cardBg;

            // Hover effect
            ColorBlock colors = cardButton.colors;
            colors.normalColor = new Color(0.18f, 0.18f, 0.19f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.26f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.16f, 1f);
            cardButton.colors = colors;

            // Barra lateral de status (5px à esquerda)
            GameObject statusBar = new GameObject("StatusBar");
            statusBar.transform.SetParent(card.transform, false);
            RectTransform barRect = statusBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 0.5f);
            barRect.sizeDelta = new Vector2(5, 0);
            barRect.anchoredPosition = Vector2.zero;
            barRect.localRotation = Quaternion.identity;
            barRect.localScale = Vector3.one;

            Image barImg = statusBar.AddComponent<Image>();
            barImg.color = new Color(0.3f, 1f, 0.3f, 1f); // Verde por padrão

            // Ícone (opcional, 60x60 à esquerda)
            GameObject icone = new GameObject("IconeImage");
            icone.transform.SetParent(card.transform, false);
            RectTransform iconeRect = icone.AddComponent<RectTransform>();
            iconeRect.anchorMin = new Vector2(0, 0.5f);
            iconeRect.anchorMax = new Vector2(0, 0.5f);
            iconeRect.pivot = new Vector2(0, 0.5f);
            iconeRect.sizeDelta = new Vector2(60, 60);
            iconeRect.anchoredPosition = new Vector2(20, 0);
            iconeRect.localRotation = Quaternion.identity;
            iconeRect.localScale = Vector3.one;

            Image iconeImg = icone.AddComponent<Image>();
            iconeImg.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            icone.SetActive(false); // Esconde por padrão

            // TÍTULO (topo, bold, 20pt)
            GameObject titulo = new GameObject("TituloText");
            titulo.transform.SetParent(card.transform, false);
            RectTransform tituloRect = titulo.AddComponent<RectTransform>();
            tituloRect.anchorMin = new Vector2(0, 1);
            tituloRect.anchorMax = new Vector2(1, 1);
            tituloRect.pivot = new Vector2(0, 1);
            tituloRect.sizeDelta = new Vector2(-200, 30);
            tituloRect.anchoredPosition = new Vector2(100, -15);
            tituloRect.localRotation = Quaternion.identity;
            tituloRect.localScale = Vector3.one;

            TextMeshProUGUI tituloTMP = titulo.AddComponent<TextMeshProUGUI>();
            tituloTMP.text = "Nome da Feature";
            tituloTMP.fontSize = 20;
            tituloTMP.fontStyle = FontStyles.Bold;
            tituloTMP.color = Color.white;
            tituloTMP.alignment = TextAlignmentOptions.Left;
            tituloTMP.enableAutoSizing = false;

            // CATEGORIA (abaixo do título, 14pt)
            GameObject categoria = new GameObject("CategoriaText");
            categoria.transform.SetParent(card.transform, false);
            RectTransform catRect = categoria.AddComponent<RectTransform>();
            catRect.anchorMin = new Vector2(0, 1);
            catRect.anchorMax = new Vector2(0, 1);
            catRect.pivot = new Vector2(0, 1);
            catRect.sizeDelta = new Vector2(150, 20);
            catRect.anchoredPosition = new Vector2(100, -50);
            catRect.localRotation = Quaternion.identity;
            catRect.localScale = Vector3.one;

            TextMeshProUGUI catTMP = categoria.AddComponent<TextMeshProUGUI>();
            catTMP.text = "Categoria";
            catTMP.fontSize = 14;
            catTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            catTMP.alignment = TextAlignmentOptions.Left;
            catTMP.enableAutoSizing = false;

            // TIPO (ao lado da categoria, 14pt)
            GameObject tipo = new GameObject("TipoText");
            tipo.transform.SetParent(card.transform, false);
            RectTransform tipoRect = tipo.AddComponent<RectTransform>();
            tipoRect.anchorMin = new Vector2(0, 1);
            tipoRect.anchorMax = new Vector2(0, 1);
            tipoRect.pivot = new Vector2(0, 1);
            tipoRect.sizeDelta = new Vector2(200, 20);
            tipoRect.anchoredPosition = new Vector2(260, -50);
            tipoRect.localRotation = Quaternion.identity;
            tipoRect.localScale = Vector3.one;

            TextMeshProUGUI tipoTMP = tipo.AddComponent<TextMeshProUGUI>();
            tipoTMP.text = "Tipo";
            tipoTMP.fontSize = 14;
            tipoTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            tipoTMP.alignment = TextAlignmentOptions.Left;
            tipoTMP.enableAutoSizing = false;

            // DESCRIÇÃO (centro, 14pt, 2 linhas)
            GameObject descricao = new GameObject("DescricaoText");
            descricao.transform.SetParent(card.transform, false);
            RectTransform descRect = descricao.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.5f);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.pivot = new Vector2(0, 0.5f);
            descRect.sizeDelta = new Vector2(-200, 40);
            descRect.anchoredPosition = new Vector2(100, -10);
            descRect.localRotation = Quaternion.identity;
            descRect.localScale = Vector3.one;

            TextMeshProUGUI descTMP = descricao.AddComponent<TextMeshProUGUI>();
            descTMP.text = "Descrição da feature";
            descTMP.fontSize = 14;
            descTMP.color = Color.white;
            descTMP.alignment = TextAlignmentOptions.TopLeft;
            descTMP.enableAutoSizing = false;
            descTMP.overflowMode = TextOverflowModes.Ellipsis;

            // RESPONSÁVEL (embaixo esquerda, 12pt)
            GameObject responsavel = new GameObject("ResponsavelText");
            responsavel.transform.SetParent(card.transform, false);
            RectTransform respRect = responsavel.AddComponent<RectTransform>();
            respRect.anchorMin = new Vector2(0, 0);
            respRect.anchorMax = new Vector2(0, 0);
            respRect.pivot = new Vector2(0, 0);
            respRect.sizeDelta = new Vector2(300, 20);
            respRect.anchoredPosition = new Vector2(100, 15);
            respRect.localRotation = Quaternion.identity;
            respRect.localScale = Vector3.one;

            TextMeshProUGUI respTMP = responsavel.AddComponent<TextMeshProUGUI>();
            respTMP.text = "Por: Equipe";
            respTMP.fontSize = 12;
            respTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            respTMP.alignment = TextAlignmentOptions.Left;
            respTMP.enableAutoSizing = false;

            // STATUS (embaixo direita, 14pt, bold, colorido)
            GameObject status = new GameObject("StatusText");
            status.transform.SetParent(card.transform, false);
            RectTransform statusRect = status.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 0);
            statusRect.anchorMax = new Vector2(1, 0);
            statusRect.pivot = new Vector2(1, 0);
            statusRect.sizeDelta = new Vector2(150, 25);
            statusRect.anchoredPosition = new Vector2(-15, 15);
            statusRect.localRotation = Quaternion.identity;
            statusRect.localScale = Vector3.one;

            TextMeshProUGUI statusTMP = status.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Concluído";
            statusTMP.fontSize = 14;
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.color = new Color(0.3f, 1f, 0.3f, 1f);
            statusTMP.alignment = TextAlignmentOptions.Right;
            statusTMP.enableAutoSizing = false;

            // Script do card
            FeatureCard script = card.AddComponent<FeatureCard>();

            // Salva como prefab
            string path = "Assets/Prefabs/FeatureCard.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, path);
            Object.DestroyImmediate(card);

            Debug.Log($"[Wizard] Prefab COMPLETO criado: {path}");
            return prefab;
        }

        private void CreateNewDatabase()
        {
            var db = ScriptableObject.CreateInstance<FeaturesDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/FeaturesDatabase.asset");
            AssetDatabase.SaveAssets();
            database = db;
            Debug.Log("[Wizard] Database criado: Assets/FeaturesDatabase.asset");
        }

        private void ConnectAllReferences(GameObject panel, GameObject cardPrefab)
        {
            FeaturesPanel script = panel.GetComponent<FeaturesPanel>();
            if (script == null)
            {
                Debug.LogError("[Wizard] FeaturesPanel script não encontrado!");
                return;
            }

            // Usa SerializedObject para editar as propriedades private
            SerializedObject serializedPanel = new SerializedObject(script);

            // Database
            if (autoConnectDatabase && database != null)
            {
                SerializedProperty databaseProp = serializedPanel.FindProperty("database");
                if (databaseProp != null)
                {
                    databaseProp.objectReferenceValue = database;
                    Debug.Log("[Wizard] ✅ Database conectado");
                }
            }

            // Cards Container (Content do ScrollView)
            Transform content = panel.transform.Find("ScrollView/Viewport/Content");
            if (content != null)
            {
                SerializedProperty containerProp = serializedPanel.FindProperty("cardsContainer");
                if (containerProp != null)
                {
                    containerProp.objectReferenceValue = content;
                    Debug.Log("[Wizard] ✅ Cards Container conectado");
                }
            }

            // Feature Card Prefab
            if (cardPrefab != null)
            {
                SerializedProperty prefabProp = serializedPanel.FindProperty("featureCardPrefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = cardPrefab;
                    Debug.Log("[Wizard] ✅ Feature Card Prefab conectado");
                }
            }

            // Titulo Text
            Transform tituloText = panel.transform.Find("Header/TituloText");
            if (tituloText != null)
            {
                SerializedProperty tituloProp = serializedPanel.FindProperty("tituloText");
                if (tituloProp != null)
                {
                    tituloProp.objectReferenceValue = tituloText.GetComponent<TextMeshProUGUI>();
                    Debug.Log("[Wizard] ✅ Titulo Text conectado");
                }
            }

            // Descricao Text
            Transform descricaoText = panel.transform.Find("Header/DescricaoText");
            if (descricaoText != null)
            {
                SerializedProperty descricaoProp = serializedPanel.FindProperty("descricaoText");
                if (descricaoProp != null)
                {
                    descricaoProp.objectReferenceValue = descricaoText.GetComponent<TextMeshProUGUI>();
                    Debug.Log("[Wizard] ✅ Descricao Text conectado");
                }
            }

            // Stats Text
            Transform statsText = panel.transform.Find("Header/StatsText");
            if (statsText != null)
            {
                SerializedProperty statsProp = serializedPanel.FindProperty("statsText");
                if (statsProp != null)
                {
                    statsProp.objectReferenceValue = statsText.GetComponent<TextMeshProUGUI>();
                    Debug.Log("[Wizard] ✅ Stats Text conectado");
                }
            }

            // Detail Popup
            Transform detailPopup = panel.transform.Find("DetailPopup");
            if (detailPopup != null)
            {
                FeatureDetailPopup popupComp = detailPopup.GetComponent<FeatureDetailPopup>();
                if (popupComp != null)
                {
                    SerializedProperty popupProp = serializedPanel.FindProperty("detailPopup");
                    if (popupProp != null)
                    {
                        popupProp.objectReferenceValue = popupComp;
                        Debug.Log("[Wizard] ✅ Detail Popup conectado");
                    }

                    // Conecta referências dentro do popup
                    ConnectPopupReferences(popupComp, detailPopup);
                }
            }

            // Aplica as mudanças
            serializedPanel.ApplyModifiedProperties();
            EditorUtility.SetDirty(script);

            Debug.Log("[Wizard] ========== Todas as referências conectadas! ==========");
        }

        private void ConnectPopupReferences(FeatureDetailPopup popup, Transform popupTransform)
        {
            SerializedObject serializedPopup = new SerializedObject(popup);

            // Conecta todas as referências do popup
            Transform content = popupTransform.Find("PopupContent");
            if (content == null) return;

            // Título
            Transform titulo = content.Find("Header/TituloText");
            if (titulo != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("tituloText");
                if (prop != null) prop.objectReferenceValue = titulo.GetComponent<TextMeshProUGUI>();
            }

            // Categoria
            Transform categoria = content.Find("Header/CategoriaText");
            if (categoria != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("categoriaText");
                if (prop != null) prop.objectReferenceValue = categoria.GetComponent<TextMeshProUGUI>();
            }

            // Tipo
            Transform tipo = content.Find("Header/TipoText");
            if (tipo != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("tipoText");
                if (prop != null) prop.objectReferenceValue = tipo.GetComponent<TextMeshProUGUI>();
            }

            // Ícone
            Transform icone = content.Find("Header/IconeImage");
            if (icone != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("iconeImage");
                if (prop != null) prop.objectReferenceValue = icone.GetComponent<Image>();
            }

            // Descrição longa
            Transform descricao = content.Find("ScrollArea/DescricaoLongaText");
            if (descricao != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("descricaoLongaText");
                if (prop != null) prop.objectReferenceValue = descricao.GetComponent<TextMeshProUGUI>();
            }

            // Screenshot
            Transform screenshot = content.Find("ScrollArea/ScreenshotContainer/ScreenshotImage");
            if (screenshot != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("screenshotImage");
                if (prop != null) prop.objectReferenceValue = screenshot.GetComponent<Image>();
            }

            Transform ssContainer = content.Find("ScrollArea/ScreenshotContainer");
            if (ssContainer != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("screenshotContainer");
                if (prop != null) prop.objectReferenceValue = ssContainer.gameObject;
            }

            // Responsável
            Transform resp = content.Find("Footer/ResponsavelText");
            if (resp != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("responsavelText");
                if (prop != null) prop.objectReferenceValue = resp.GetComponent<TextMeshProUGUI>();
            }

            // Status
            Transform status = content.Find("Footer/StatusText");
            if (status != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("statusText");
                if (prop != null) prop.objectReferenceValue = status.GetComponent<TextMeshProUGUI>();
            }

            // Tags
            Transform tags = content.Find("Footer/TagsText");
            if (tags != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("tagsText");
                if (prop != null) prop.objectReferenceValue = tags.GetComponent<TextMeshProUGUI>();
            }

            // Close Button
            Transform closeBtn = content.Find("Header/CloseButton");
            if (closeBtn != null)
            {
                SerializedProperty prop = serializedPopup.FindProperty("closeButton");
                if (prop != null) prop.objectReferenceValue = closeBtn.GetComponent<Button>();
            }

            serializedPopup.ApplyModifiedProperties();
            EditorUtility.SetDirty(popup);

            Debug.Log("[Wizard] ✅ Popup references conectadas");
        }

        private void ConnectFilterButtons(GameObject panel)
        {
            FeaturesPanel script = panel.GetComponent<FeaturesPanel>();
            if (script == null) return;

            Transform filterButtons = panel.transform.Find("FilterButtons");
            if (filterButtons == null)
            {
                Debug.LogWarning("[Wizard] FilterButtons não encontrado!");
                return;
            }

            // Conecta cada botão
            ConnectButton(filterButtons, "BtnTudo", script.ShowAll);
            ConnectButton(filterButtons, "BtnProgramação", script.FilterProgramacao);
            ConnectButton(filterButtons, "BtnArte", script.FilterArte);
            ConnectButton(filterButtons, "BtnVFX", script.FilterVFX);
            ConnectButton(filterButtons, "BtnMecânica", script.FilterMecanica);
            ConnectButton(filterButtons, "BtnGeral", script.FilterGeral);

            Debug.Log("[Wizard] ✅ Todos os botões conectados!");
        }

        private void ConnectButton(Transform parent, string buttonName, UnityEngine.Events.UnityAction method)
        {
            Transform btn = parent.Find(buttonName);
            if (btn != null)
            {
                Button button = btn.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(method);
                    Debug.Log($"[Wizard] ✅ Botão '{buttonName}' conectado");
                }
            }
        }

        #endregion
    }
}

