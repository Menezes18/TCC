using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ProjectFeatures
{
    /// <summary>
    /// Gerencia o painel de features no jogo.
    /// Exibe as features, filtros e estatísticas.
    /// 
    /// Sistema simples: apenas configure o database e está pronto para usar!
    /// </summary>
    public class FeaturesPanel : MonoBehaviour
    {
        [Header("Database")]
        [Tooltip("Database com todas as features do projeto")]
        [SerializeField] private FeaturesDatabase database;

        [Header("UI - Container")]
        [Tooltip("Container onde os cards serão instanciados (Content do ScrollView)")]
        [SerializeField] private Transform cardsContainer;

        [Tooltip("Prefab do card de feature")]
        [SerializeField] private GameObject featureCardPrefab;

        [Header("UI - Textos")]
        [SerializeField] private TextMeshProUGUI tituloText;
        [SerializeField] private TextMeshProUGUI descricaoText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("UI - Feedback")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject noResultsPanel;

        [Header("UI - Popup de Detalhes")]
        [Tooltip("Popup que mostra detalhes completos ao clicar em um card")]
        [SerializeField] private FeatureDetailPopup detailPopup;

        [Header("Configurações")]
        [Tooltip("Filtro inicial (null = Todas)")]
        [SerializeField] private FeatureCategory? initialFilter = null;

        [Tooltip("Conectar botões automaticamente por código (desmarque se preferir conectar manualmente no Inspector)")]
        [SerializeField] private bool autoConnectButtons = true;

        // Estado
        private List<GameObject> instantiatedCards = new List<GameObject>();
        private FeatureCategory? currentFilter = null;

        private void Start()
        {
            // Conecta os botões automaticamente se configurado
            if (autoConnectButtons)
            {
                SetupFilterButtons();
            }

            if (database == null)
            {
                Debug.LogError("[FeaturesPanel] Database não configurado!");
                return;
            }

            if (cardsContainer == null || featureCardPrefab == null)
            {
                Debug.LogError("[FeaturesPanel] Container ou prefab não configurado!");
                return;
            }

            // Configura textos iniciais
            SetupHeaderTexts();

            // Aplica filtro inicial
            currentFilter = initialFilter;

            // Exibe features
            RefreshDisplay();
        }

        /// <summary>
        /// Conecta automaticamente os botões de filtro aos métodos.
        /// Busca os botões na hierarquia e adiciona os listeners.
        /// </summary>
        private void SetupFilterButtons()
        {
            // Busca o container de botões
            Transform filterButtons = transform.Find("FilterButtons");
            if (filterButtons == null)
            {
                Debug.LogWarning("[FeaturesPanel] FilterButtons não encontrado. Botões não serão conectados automaticamente.");
                return;
            }

            // Conecta cada botão ao seu método correspondente
            ConnectButton(filterButtons, "BtnTudo", ShowAll);
            ConnectButton(filterButtons, "BtnProgramação", FilterProgramacao);
            ConnectButton(filterButtons, "BtnArte", FilterArte);
            ConnectButton(filterButtons, "BtnVFX", FilterVFX);
            ConnectButton(filterButtons, "BtnMecânica", FilterMecanica);
            ConnectButton(filterButtons, "BtnGeral", FilterGeral);

            Debug.Log("[FeaturesPanel] ✅ Botões de filtro conectados automaticamente!");
        }

        /// <summary>
        /// Conecta um botão específico a um método.
        /// </summary>
        private void ConnectButton(Transform parent, string buttonName, UnityEngine.Events.UnityAction method)
        {
            Transform btnTransform = parent.Find(buttonName);
            if (btnTransform != null)
            {
                UnityEngine.UI.Button button = btnTransform.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    // Remove listeners antigos para evitar duplicatas
                    button.onClick.RemoveAllListeners();
                    // Adiciona o novo listener
                    button.onClick.AddListener(method);
                    Debug.Log($"[FeaturesPanel] ✅ Botão '{buttonName}' conectado");
                }
                else
                {
                    Debug.LogWarning($"[FeaturesPanel] Botão '{buttonName}' encontrado mas sem componente Button!");
                }
            }
            else
            {
                Debug.LogWarning($"[FeaturesPanel] Botão '{buttonName}' não encontrado!");
            }
        }

        /// <summary>
        /// Configura os textos do cabeçalho.
        /// </summary>
        private void SetupHeaderTexts()
        {
            if (tituloText != null)
                tituloText.text = database.tituloTela;

            if (descricaoText != null)
                descricaoText.text = database.descricaoProjeto;

            UpdateStatsText();
        }

        /// <summary>
        /// Atualiza o texto de estatísticas.
        /// </summary>
        private void UpdateStatsText()
        {
            if (statsText == null) return;

            var stats = database.GetStats();
            statsText.text = $"Total: {stats.totalFeatures} | " +
                           $"Concluídas: {stats.concluidas} | " +
                           $"Em Andamento: {stats.emAndamento}";
        }

        /// <summary>
        /// Atualiza a exibição de features baseado no filtro atual.
        /// </summary>
        public void RefreshDisplay()
        {
            // Limpa cards anteriores
            ClearCards();

            // Mostra loading
            ShowLoading(true);

            // Busca features filtradas
            List<FeatureEntry> features = GetFilteredFeatures();

            // Esconde loading
            ShowLoading(false);

            // Mostra mensagem se não houver resultados
            if (features.Count == 0)
            {
                ShowNoResults(true);
                return;
            }

            ShowNoResults(false);

            // Cria cards
            foreach (var feature in features)
            {
                CreateCard(feature);
            }
        }

        /// <summary>
        /// Retorna features filtradas baseado no filtro atual.
        /// </summary>
        private List<FeatureEntry> GetFilteredFeatures()
        {
            if (currentFilter.HasValue)
            {
                return database.GetFeaturesByCategory(currentFilter.Value);
            }

            return database.GetAllFeatures();
        }

        /// <summary>
        /// Cria um card para uma feature.
        /// </summary>
        private void CreateCard(FeatureEntry feature)
        {
            GameObject cardObj = Instantiate(featureCardPrefab, cardsContainer);
            instantiatedCards.Add(cardObj);

            FeatureCard card = cardObj.GetComponent<FeatureCard>();
            if (card != null)
            {
                // Passa a feature E o popup de detalhes
                card.Setup(feature, detailPopup);
            }
            else
            {
                Debug.LogWarning("[FeaturesPanel] Prefab não tem componente FeatureCard!");
            }
        }

        /// <summary>
        /// Limpa todos os cards instanciados.
        /// </summary>
        private void ClearCards()
        {
            foreach (var card in instantiatedCards)
            {
                if (card != null)
                    Destroy(card);
            }
            instantiatedCards.Clear();
        }

        /// <summary>
        /// Mostra/esconde painel de loading.
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(show);
        }

        /// <summary>
        /// Mostra/esconde mensagem de sem resultados.
        /// </summary>
        private void ShowNoResults(bool show)
        {
            if (noResultsPanel != null)
                noResultsPanel.SetActive(show);
        }

        #region Métodos Públicos - Filtros (Conectar aos botões)

        /// <summary>
        /// Mostra todas as features (sem filtro).
        /// </summary>
        public void ShowAll()
        {
            currentFilter = null;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: Todas");
        }

        /// <summary>
        /// Filtra por Programação.
        /// </summary>
        public void FilterProgramacao()
        {
            currentFilter = FeatureCategory.Programacao;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: Programação");
        }

        /// <summary>
        /// Filtra por Arte.
        /// </summary>
        public void FilterArte()
        {
            currentFilter = FeatureCategory.Arte;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: Arte");
        }

        /// <summary>
        /// Filtra por VFX.
        /// </summary>
        public void FilterVFX()
        {
            currentFilter = FeatureCategory.VFX;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: VFX");
        }

        /// <summary>
        /// Filtra por Mecânica.
        /// </summary>
        public void FilterMecanica()
        {
            currentFilter = FeatureCategory.Mecanica;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: Mecânica");
        }

        /// <summary>
        /// Filtra por Geral.
        /// </summary>
        public void FilterGeral()
        {
            currentFilter = FeatureCategory.Geral;
            RefreshDisplay();
            Debug.Log("[FeaturesPanel] Filtro: Geral");
        }

        #endregion
    }
}

