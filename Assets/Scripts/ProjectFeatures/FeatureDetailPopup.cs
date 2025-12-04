using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace ProjectFeatures
{
    /// <summary>
    /// Popup que mostra detalhes completos de uma feature.
    /// Exibe: Descrição longa, screenshot, todas as informações.
    /// Com animações suaves de abertura e fechamento!
    /// </summary>
    public class FeatureDetailPopup : MonoBehaviour
    {
        [Header("Referências da UI")]
        [SerializeField] private TextMeshProUGUI tituloText;
        [SerializeField] private TextMeshProUGUI categoriaText;
        [SerializeField] private TextMeshProUGUI tipoText;
        [SerializeField] private TextMeshProUGUI descricaoLongaText;
        [SerializeField] private TextMeshProUGUI responsavelText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI tagsText;
        [SerializeField] private Image screenshotImage;
        [SerializeField] private Image iconeImage;
        [SerializeField] private GameObject screenshotContainer;
        [SerializeField] private Button closeButton;

        [Header("Animação")]
        [Tooltip("Duração da animação de abertura/fechamento")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CanvasGroup canvasGroup;
        private RectTransform popupContentRect;
        private Coroutine currentAnimation;

        private void Awake()
        {
            // CanvasGroup para controlar alpha
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Encontra o PopupContent para animar scale
            Transform content = transform.Find("PopupContent");
            if (content != null)
            {
                popupContentRect = content.GetComponent<RectTransform>();
            }

            // Conecta botão de fechar
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            // Esconde ao iniciar
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Abre o popup com os detalhes de uma feature.
        /// </summary>
        public void Show(FeatureEntry feature)
        {
            if (feature == null)
            {
                Debug.LogWarning("[FeatureDetailPopup] Feature é null!");
                return;
            }

            // Título
            if (tituloText != null)
                tituloText.text = feature.titulo;

            // Categoria
            if (categoriaText != null)
                categoriaText.text = feature.GetCategoriaNome();

            // Tipo
            if (tipoText != null)
                tipoText.text = feature.tipo;

            // Descrição longa (se tiver, senão usa a curta)
            if (descricaoLongaText != null)
            {
                string desc = !string.IsNullOrEmpty(feature.descricaoLonga) 
                    ? feature.descricaoLonga 
                    : feature.descricaoCurta;
                descricaoLongaText.text = desc;
            }

            // Responsável
            if (responsavelText != null)
                responsavelText.text = $"Responsável: {feature.responsavel}";

            // Status
            if (statusText != null)
            {
                statusText.text = feature.GetStatusNome();
                statusText.color = feature.GetStatusColor();
            }

            // Tags
            if (tagsText != null)
            {
                if (feature.tags != null && feature.tags.Length > 0)
                {
                    tagsText.text = string.Join(" • ", feature.tags);
                }
                else
                {
                    tagsText.text = "Sem tags";
                }
            }

            // Screenshot
            if (screenshotContainer != null && screenshotImage != null)
            {
                if (feature.screenshot != null)
                {
                    screenshotImage.sprite = feature.screenshot;
                    screenshotContainer.SetActive(true);
                }
                else
                {
                    screenshotContainer.SetActive(false);
                }
            }

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

            // Mostra o popup com animação
            gameObject.SetActive(true);

            // Para animação anterior se houver
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            // Inicia animação de abertura
            currentAnimation = StartCoroutine(AnimateOpen());

            Debug.Log($"[FeatureDetailPopup] Exibindo detalhes de: {feature.titulo}");
        }

        /// <summary>
        /// Fecha o popup com animação.
        /// </summary>
        public void Close()
        {
            // Para animação anterior se houver
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            // Inicia animação de fechamento
            currentAnimation = StartCoroutine(AnimateClose());

            Debug.Log("[FeatureDetailPopup] Popup fechado");
        }

        /// <summary>
        /// Animação suave de abertura (fade in + scale up).
        /// </summary>
        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;

            // Estado inicial
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (popupContentRect != null)
            {
                popupContentRect.localScale = Vector3.one * 0.7f; // Começa pequeno
            }

            // Anima
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = animationCurve.Evaluate(t);

                // Fade in
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = curveValue;
                }

                // Scale up
                if (popupContentRect != null)
                {
                    float scale = Mathf.Lerp(0.7f, 1f, curveValue);
                    popupContentRect.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // Garante estado final
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (popupContentRect != null)
            {
                popupContentRect.localScale = Vector3.one;
            }

            currentAnimation = null;
        }

        /// <summary>
        /// Animação suave de fechamento (fade out + scale down).
        /// </summary>
        private IEnumerator AnimateClose()
        {
            float elapsed = 0f;

            // Anima
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = 1f - animationCurve.Evaluate(t);

                // Fade out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = curveValue;
                }

                // Scale down
                if (popupContentRect != null)
                {
                    float scale = Mathf.Lerp(0.7f, 1f, curveValue);
                    popupContentRect.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // Desativa no final
            gameObject.SetActive(false);

            // Restaura para próxima abertura
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (popupContentRect != null)
            {
                popupContentRect.localScale = Vector3.one;
            }

            currentAnimation = null;
        }
    }
}

