using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectFeatures
{
    /// <summary>
    /// Script do prefab do card de feature.
    /// Exibe visualmente uma feature na UI do jogo.
    /// Clique no card para ver detalhes completos!
    /// </summary>
    public class FeatureCard : MonoBehaviour
    {
        [Header("Referências da UI")]
        [SerializeField] private TextMeshProUGUI tituloText;
        [SerializeField] private TextMeshProUGUI categoriaText;
        [SerializeField] private TextMeshProUGUI tipoText;
        [SerializeField] private TextMeshProUGUI descricaoText;
        [SerializeField] private TextMeshProUGUI responsavelText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image iconeImage;
        [SerializeField] private Image statusColorImage; // Barra lateral colorida por status
        [SerializeField] private GameObject tagsContainer;
        [SerializeField] private TextMeshProUGUI tagsText;
        [SerializeField] private Button cardButton; // Botão para clicar no card

        private FeatureEntry feature;
        private FeatureDetailPopup detailPopup;

        /// <summary>
        /// Configura o card com os dados de uma feature.
        /// </summary>
        public void Setup(FeatureEntry featureData, FeatureDetailPopup popup = null)
        {
            feature = featureData;
            detailPopup = popup;

            // Conecta botão se houver
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(OnCardClicked);
            }

            // Título
            if (tituloText != null)
                tituloText.text = feature.titulo;

            // Categoria
            if (categoriaText != null)
                categoriaText.text = feature.GetCategoriaNome();

            // Tipo
            if (tipoText != null)
            {
                tipoText.text = feature.tipo;
                tipoText.gameObject.SetActive(!string.IsNullOrEmpty(feature.tipo));
            }

            // Descrição
            if (descricaoText != null)
                descricaoText.text = feature.descricaoCurta;

            // Responsável
            if (responsavelText != null)
                responsavelText.text = $"Por: {feature.responsavel}";

            // Status
            if (statusText != null)
            {
                statusText.text = feature.GetStatusNome();
                statusText.color = feature.GetStatusColor();
            }

            // Cor de status (barra lateral)
            if (statusColorImage != null)
                statusColorImage.color = feature.GetStatusColor();

            // Ícone
            if (iconeImage != null)
            {
                if (feature.icone != null)
                {
                    iconeImage.sprite = feature.icone;
                    iconeImage.gameObject.SetActive(true);
                }
                else
                {
                    iconeImage.gameObject.SetActive(false);
                }
            }

            // Tags
            if (tagsContainer != null && tagsText != null)
            {
                if (feature.tags != null && feature.tags.Length > 0)
                {
                    tagsText.text = string.Join(" • ", feature.tags);
                    tagsContainer.SetActive(true);
                }
                else
                {
                    tagsContainer.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Chamado quando o card é clicado.
        /// Abre popup com detalhes completos.
        /// </summary>
        public void OnCardClicked()
        {
            if (feature == null)
            {
                Debug.LogWarning("[FeatureCard] Feature é null!");
                return;
            }

            Debug.Log($"[FeatureCard] Clicado em: {feature.titulo}");

            // Abre popup de detalhes
            if (detailPopup != null)
            {
                detailPopup.Show(feature);
            }
            else
            {
                Debug.LogWarning("[FeatureCard] Popup de detalhes não configurado!");
            }
        }
    }
}

