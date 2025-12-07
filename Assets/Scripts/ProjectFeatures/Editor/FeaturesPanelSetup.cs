using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace ProjectFeatures.Editor
{
    /// <summary>
    /// Editor que cria automaticamente a estrutura da UI de Features.
    /// Cria: Prefab do Card, Painel completo com botões e ScrollView.
    /// 
    /// Menu: GameObject → TCC → Create Features UI
    /// </summary>
    public static class FeaturesPanelSetup
    {
        #region Menu Items

        // Menu no GameObject (Hierarchy)
        [MenuItem("GameObject/TCC/Create Features UI/Complete Panel", false, 10)]
        // Menu alternativo no Tools
        [MenuItem("Tools/TCC/Create Features UI/Complete Panel", false, 10)]
        public static void CreateCompletePanel()
        {
            // Pega o objeto selecionado ou procura/cria Canvas
            Transform parent = GetParentTransform();

            // Cria o painel completo
            GameObject panel = CreateFeaturesPanel(parent);

            // Seleciona no editor
            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);

            Debug.Log($"[FeaturesPanelSetup] ✅ Painel de Features criado como filho de '{parent.name}'!");
        }

        [MenuItem("GameObject/TCC/Create Features UI/Feature Card Prefab Only", false, 11)]
        [MenuItem("Tools/TCC/Create Features UI/Feature Card Prefab Only", false, 11)]
        public static void CreateFeatureCardPrefabOnly()
        {
            GameObject card = CreateFeatureCardPrefab();
            
            // Salva como prefab
            string prefabPath = "Assets/Prefabs/FeatureCard.prefab";
            
            // Garante que a pasta existe
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            PrefabUtility.SaveAsPrefabAsset(card, prefabPath);
            Object.DestroyImmediate(card);

            // Seleciona o prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"[FeaturesPanelSetup] ✅ Prefab criado em: {prefabPath}");
        }

        #endregion

        #region Parent Selection

        /// <summary>
        /// Determina onde criar o painel baseado na seleção.
        /// Prioridade:
        /// 1. Objeto selecionado (se for Canvas ou tiver Canvas)
        /// 2. Canvas existente na cena
        /// 3. Cria novo Canvas
        /// </summary>
        private static Transform GetParentTransform()
        {
            // Se há algo selecionado
            if (Selection.activeGameObject != null)
            {
                GameObject selected = Selection.activeGameObject;

                // Se o selecionado é um Canvas, usa ele
                Canvas canvas = selected.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[FeaturesPanelSetup] Usando Canvas selecionado: {selected.name}");
                    return canvas.transform;
                }

                // Se o selecionado tem Canvas como pai, usa o pai
                Canvas parentCanvas = selected.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"[FeaturesPanelSetup] Usando Canvas pai: {parentCanvas.name}");
                    return parentCanvas.transform;
                }

                // Se não tem Canvas, cria como filho do selecionado
                Debug.Log($"[FeaturesPanelSetup] Criando como filho de: {selected.name}");
                return selected.transform;
            }

            // Se não há nada selecionado, procura ou cria Canvas
            Canvas sceneCanvas = FindOrCreateCanvas();
            return sceneCanvas.transform;
        }

        #endregion

        #region Create Canvas

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // EventSystem
                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                Debug.Log("[FeaturesPanelSetup] Canvas criado.");
            }

            return canvas;
        }

        #endregion

        #region Create Features Panel

        private static GameObject CreateFeaturesPanel(Transform parent)
        {
            // Detecta se é Canvas 3D
            Canvas parentCanvas = parent.GetComponent<Canvas>();
            bool isWorldSpaceCanvas = parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace;

            // Container principal
            GameObject panel = new GameObject("FeaturesPanel");
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            
            // Configura tamanho baseado no tipo de Canvas
            if (isWorldSpaceCanvas)
            {
                // Para Canvas 3D: tamanho fixo
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(1920, 1080); // Tamanho padrão HD
                panelRect.anchoredPosition = Vector2.zero;
                Debug.Log("[FeaturesPanelSetup] Canvas 3D detectado - usando tamanho fixo 1920x1080");
            }
            else
            {
                // Para Canvas 2D: fullscreen com anchors
                SetFullScreen(panelRect);
            }

            // Background
            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Header
            GameObject header = CreateHeader(panel.transform);

            // Filter Buttons
            GameObject filterButtons = CreateFilterButtons(panel.transform);

            // ScrollView
            GameObject scrollView = CreateScrollView(panel.transform);

            // Adiciona o script FeaturesPanel
            FeaturesPanel featuresPanel = panel.AddComponent<FeaturesPanel>();

            // Conecta referências
            Transform content = scrollView.transform.Find("Viewport/Content");
            featuresPanel.GetType().GetField("cardsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featuresPanel, content);

            featuresPanel.GetType().GetField("tituloText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featuresPanel, header.transform.Find("TituloText").GetComponent<TextMeshProUGUI>());

            featuresPanel.GetType().GetField("descricaoText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featuresPanel, header.transform.Find("DescricaoText").GetComponent<TextMeshProUGUI>());

            featuresPanel.GetType().GetField("statsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featuresPanel, header.transform.Find("StatsText").GetComponent<TextMeshProUGUI>());

            // Conecta botões
            ConnectFilterButtons(filterButtons, featuresPanel);

            EditorUtility.SetDirty(panel);

            return panel;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            
            // Para Canvas 3D ou 2D: sempre usa anchors no topo
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(-40, 150); // -40 para margem lateral
            headerRect.anchoredPosition = new Vector2(0, -20); // -20 para margem superior

            // Background
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Título
            GameObject titulo = CreateText(header.transform, "TituloText", "Features do Projeto - TCC");
            RectTransform tituloRect = titulo.GetComponent<RectTransform>();
            tituloRect.anchoredPosition = new Vector2(0, -30);
            tituloRect.sizeDelta = new Vector2(-40, 40);
            TextMeshProUGUI tituloTMP = titulo.GetComponent<TextMeshProUGUI>();
            tituloTMP.fontSize = 28;
            tituloTMP.fontStyle = FontStyles.Bold;
            tituloTMP.alignment = TextAlignmentOptions.Center;

            // Descrição
            GameObject descricao = CreateText(header.transform, "DescricaoText", "Confira todas as funcionalidades desenvolvidas neste projeto.");
            RectTransform descRect = descricao.GetComponent<RectTransform>();
            descRect.anchoredPosition = new Vector2(0, -70);
            descRect.sizeDelta = new Vector2(-40, 30);
            TextMeshProUGUI descTMP = descricao.GetComponent<TextMeshProUGUI>();
            descTMP.fontSize = 16;
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Stats
            GameObject stats = CreateText(header.transform, "StatsText", "Total: 0 | Concluídas: 0 | Em Andamento: 0");
            RectTransform statsRect = stats.GetComponent<RectTransform>();
            statsRect.anchoredPosition = new Vector2(0, -110);
            statsRect.sizeDelta = new Vector2(-40, 30);
            TextMeshProUGUI statsTMP = stats.GetComponent<TextMeshProUGUI>();
            statsTMP.fontSize = 14;
            statsTMP.alignment = TextAlignmentOptions.Center;
            statsTMP.color = new Color(0.5f, 0.8f, 1f, 1f);

            return header;
        }

        private static GameObject CreateFilterButtons(Transform parent)
        {
            GameObject filterPanel = new GameObject("FilterButtons");
            filterPanel.transform.SetParent(parent, false);
            RectTransform filterRect = filterPanel.AddComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 1);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.pivot = new Vector2(0.5f, 1);
            filterRect.sizeDelta = new Vector2(-40, 60); // -40 para margem lateral
            filterRect.anchoredPosition = new Vector2(0, -190); // Abaixo do header (150 + 20 + 20)

            // Layout horizontal
            HorizontalLayoutGroup layout = filterPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Botões
            string[] buttonNames = { "Tudo", "Programação", "Arte", "VFX", "Mecânica", "Geral" };
            Color[] buttonColors = {
                new Color(0.3f, 0.6f, 1f),
                new Color(0.4f, 0.7f, 1f),
                new Color(1f, 0.4f, 0.6f),
                new Color(1f, 0.7f, 0.2f),
                new Color(0.5f, 1f, 0.5f),
                new Color(0.7f, 0.7f, 0.7f)
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                CreateFilterButton(filterPanel.transform, buttonNames[i], buttonColors[i]);
            }

            return filterPanel;
        }

        private static GameObject CreateFilterButton(Transform parent, string label, Color color)
        {
            GameObject btn = new GameObject($"Btn{label}");
            btn.transform.SetParent(parent, false);
            RectTransform btnRect = btn.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(120, 40);

            Image btnImage = btn.AddComponent<Image>();
            btnImage.color = color;

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = btnImage;

            // Texto do botão
            GameObject text = CreateText(btn.transform, "Text", label);
            RectTransform textRect = text.GetComponent<RectTransform>();
            SetFullScreen(textRect);
            TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static GameObject CreateScrollView(Transform parent)
        {
            // ScrollView
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.offsetMin = new Vector2(40, 40); // left, bottom (margem)
            scrollRect.offsetMax = new Vector2(-40, -270); // right, top (header 150 + filter 60 + margens)

            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetFullScreen(viewportRect);

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            // Layout vertical
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Scrollbar
            GameObject scrollbar = new GameObject("Scrollbar Vertical");
            scrollbar.transform.SetParent(scrollView.transform, false);
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image scrollbarBg = scrollbar.AddComponent<Image>();
            scrollbarBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Scrollbar scrollbarComp = scrollbar.AddComponent<Scrollbar>();
            scrollbarComp.direction = Scrollbar.Direction.BottomToTop;

            // Handle
            GameObject handle = new GameObject("Sliding Area");
            handle.transform.SetParent(scrollbar.transform, false);
            RectTransform handleAreaRect = handle.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(5, 5);
            handleAreaRect.offsetMax = new Vector2(-5, -5);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handle.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            SetFullScreen(handleRect);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            scrollbarComp.handleRect = handleRect;
            scrollbarComp.targetGraphic = handleImage;

            // Conecta ao ScrollRect
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.verticalScrollbar = scrollbarComp;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.horizontal = false;
            scroll.vertical = true;

            return scrollView;
        }

        #endregion

        #region Create Feature Card Prefab

        private static GameObject CreateFeatureCardPrefab()
        {
            // Card principal
            GameObject card = new GameObject("FeatureCard");
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(750, 160);

            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.18f, 0.18f, 0.19f, 1f);

            // Barra lateral de status
            GameObject statusBar = new GameObject("StatusBar");
            statusBar.transform.SetParent(card.transform, false);
            RectTransform barRect = statusBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 0.5f);
            barRect.sizeDelta = new Vector2(5, 0);
            barRect.anchoredPosition = Vector2.zero;

            Image barImage = statusBar.AddComponent<Image>();
            barImage.color = new Color(0.3f, 1f, 0.3f, 1f); // Verde por padrão

            // Ícone
            GameObject icone = new GameObject("IconeImage");
            icone.transform.SetParent(card.transform, false);
            RectTransform iconeRect = icone.AddComponent<RectTransform>();
            iconeRect.anchorMin = new Vector2(0, 0.5f);
            iconeRect.anchorMax = new Vector2(0, 0.5f);
            iconeRect.pivot = new Vector2(0, 0.5f);
            iconeRect.sizeDelta = new Vector2(60, 60);
            iconeRect.anchoredPosition = new Vector2(20, 0);

            Image iconeImage = icone.AddComponent<Image>();
            iconeImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Título
            GameObject titulo = CreateText(card.transform, "TituloText", "Nome da Feature");
            RectTransform tituloRect = titulo.GetComponent<RectTransform>();
            tituloRect.anchorMin = new Vector2(0, 1);
            tituloRect.anchorMax = new Vector2(1, 1);
            tituloRect.pivot = new Vector2(0, 1);
            tituloRect.sizeDelta = new Vector2(-200, 30);
            tituloRect.anchoredPosition = new Vector2(100, -15);
            TextMeshProUGUI tituloTMP = titulo.GetComponent<TextMeshProUGUI>();
            tituloTMP.fontSize = 20;
            tituloTMP.fontStyle = FontStyles.Bold;
            tituloTMP.alignment = TextAlignmentOptions.Left;

            // Categoria
            GameObject categoria = CreateText(card.transform, "CategoriaText", "Categoria");
            RectTransform catRect = categoria.GetComponent<RectTransform>();
            catRect.anchorMin = new Vector2(0, 1);
            catRect.anchorMax = new Vector2(0, 1);
            catRect.pivot = new Vector2(0, 1);
            catRect.sizeDelta = new Vector2(200, 20);
            catRect.anchoredPosition = new Vector2(100, -50);
            TextMeshProUGUI catTMP = categoria.GetComponent<TextMeshProUGUI>();
            catTMP.fontSize = 14;
            catTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Tipo
            GameObject tipo = CreateText(card.transform, "TipoText", "Tipo");
            RectTransform tipoRect = tipo.GetComponent<RectTransform>();
            tipoRect.anchorMin = new Vector2(0, 1);
            tipoRect.anchorMax = new Vector2(0, 1);
            tipoRect.pivot = new Vector2(0, 1);
            tipoRect.sizeDelta = new Vector2(200, 20);
            tipoRect.anchoredPosition = new Vector2(220, -50);
            TextMeshProUGUI tipoTMP = tipo.GetComponent<TextMeshProUGUI>();
            tipoTMP.fontSize = 14;
            tipoTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Descrição
            GameObject descricao = CreateText(card.transform, "DescricaoText", "Descrição da feature");
            RectTransform descRect = descricao.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.5f);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.pivot = new Vector2(0, 0.5f);
            descRect.sizeDelta = new Vector2(-200, 40);
            descRect.anchoredPosition = new Vector2(100, -10);
            TextMeshProUGUI descTMP = descricao.GetComponent<TextMeshProUGUI>();
            descTMP.fontSize = 14;
            descTMP.alignment = TextAlignmentOptions.TopLeft;

            // Responsável
            GameObject responsavel = CreateText(card.transform, "ResponsavelText", "Por: Equipe");
            RectTransform respRect = responsavel.GetComponent<RectTransform>();
            respRect.anchorMin = new Vector2(0, 0);
            respRect.anchorMax = new Vector2(0, 0);
            respRect.pivot = new Vector2(0, 0);
            respRect.sizeDelta = new Vector2(300, 20);
            respRect.anchoredPosition = new Vector2(100, 15);
            TextMeshProUGUI respTMP = responsavel.GetComponent<TextMeshProUGUI>();
            respTMP.fontSize = 12;
            respTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Status
            GameObject status = CreateText(card.transform, "StatusText", "Concluído");
            RectTransform statusRect = status.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 0);
            statusRect.anchorMax = new Vector2(1, 0);
            statusRect.pivot = new Vector2(1, 0);
            statusRect.sizeDelta = new Vector2(150, 25);
            statusRect.anchoredPosition = new Vector2(-15, 15);
            TextMeshProUGUI statusTMP = status.GetComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 14;
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.alignment = TextAlignmentOptions.Right;
            statusTMP.color = new Color(0.3f, 1f, 0.3f, 1f);

            // Tags container
            GameObject tagsContainer = new GameObject("TagsContainer");
            tagsContainer.transform.SetParent(card.transform, false);
            RectTransform tagsContRect = tagsContainer.AddComponent<RectTransform>();
            tagsContRect.anchorMin = new Vector2(0, 0);
            tagsContRect.anchorMax = new Vector2(1, 0);
            tagsContRect.pivot = new Vector2(0, 0);
            tagsContRect.sizeDelta = new Vector2(-400, 20);
            tagsContRect.anchoredPosition = new Vector2(100, 40);

            GameObject tags = CreateText(tagsContainer.transform, "TagsText", "Tag1 • Tag2 • Tag3");
            RectTransform tagsRect = tags.GetComponent<RectTransform>();
            SetFullScreen(tagsRect);
            TextMeshProUGUI tagsTMP = tags.GetComponent<TextMeshProUGUI>();
            tagsTMP.fontSize = 11;
            tagsTMP.color = new Color(0.5f, 0.7f, 1f, 1f);

            // Adiciona script
            FeatureCard featureCard = card.AddComponent<FeatureCard>();

            // Conecta referências via reflection
            featureCard.GetType().GetField("iconeImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, iconeImage);
            featureCard.GetType().GetField("tituloText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, tituloTMP);
            featureCard.GetType().GetField("categoriaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, catTMP);
            featureCard.GetType().GetField("tipoText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, tipoTMP);
            featureCard.GetType().GetField("descricaoText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, descTMP);
            featureCard.GetType().GetField("responsavelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, respTMP);
            featureCard.GetType().GetField("statusText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, statusTMP);
            featureCard.GetType().GetField("statusColorImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, barImage);
            featureCard.GetType().GetField("tagsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, tagsContainer);
            featureCard.GetType().GetField("tagsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(featureCard, tagsTMP);

            return card;
        }

        #endregion

        #region Helper Methods

        private static GameObject CreateText(Transform parent, string name, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 30);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;

            return textObj;
        }

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void ConnectFilterButtons(GameObject filterPanel, FeaturesPanel featuresPanel)
        {
            // Conecta cada botão ao método correspondente
            Transform tudo = filterPanel.transform.Find("BtnTudo");
            if (tudo != null)
            {
                Button btn = tudo.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.ShowAll);
            }

            Transform prog = filterPanel.transform.Find("BtnProgramação");
            if (prog != null)
            {
                Button btn = prog.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.FilterProgramacao);
            }

            Transform arte = filterPanel.transform.Find("BtnArte");
            if (arte != null)
            {
                Button btn = arte.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.FilterArte);
            }

            Transform vfx = filterPanel.transform.Find("BtnVFX");
            if (vfx != null)
            {
                Button btn = vfx.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.FilterVFX);
            }

            Transform mec = filterPanel.transform.Find("BtnMecânica");
            if (mec != null)
            {
                Button btn = mec.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.FilterMecanica);
            }

            Transform geral = filterPanel.transform.Find("BtnGeral");
            if (geral != null)
            {
                Button btn = geral.GetComponent<Button>();
                btn.onClick.AddListener(featuresPanel.FilterGeral);
            }
        }

        #endregion
    }
}

