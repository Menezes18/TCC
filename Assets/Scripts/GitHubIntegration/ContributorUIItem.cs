using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GitHubIntegration
{
    /// <summary>
    /// Script simples para o prefab de cada contribuidor.
    /// Exibe: Foto + Nome + Contribuições
    /// 
    /// Estrutura recomendada do prefab:
    /// - ContributorItem (GameObject raiz)
    ///   ├─ AvatarImage (Image) - Foto do usuário
    ///   ├─ NameText (TextMeshProUGUI) - Nome do usuário
    ///   └─ ContributionsText (TextMeshProUGUI) - "150 contribuições"
    /// </summary>
    public class ContributorUIItem : MonoBehaviour
    {
        [Header("Referências da UI")]
        [Tooltip("Imagem para o avatar do contribuidor")]
        [SerializeField] private Image avatarImage;

        [Tooltip("Texto que mostra o nome do contribuidor")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Tooltip("Texto que mostra o número de contribuições")]
        [SerializeField] private TextMeshProUGUI contributionsText;

        [Header("Configurações Visuais")]
        [Tooltip("Sprite padrão enquanto a imagem carrega ou se falhar")]
        [SerializeField] private Sprite defaultAvatarSprite;

        /// <summary>
        /// Configura todos os dados visuais do item.
        /// Chamado pelo GitHubContributorsDisplay após instanciar o prefab.
        /// </summary>
        /// <param name="ranking">Posição (não usado, mantido por compatibilidade)</param>
        /// <param name="contributor">Dados do contribuidor</param>
        public void Setup(int ranking, GitHubContributor contributor)
        {
            // Configura nome do contribuidor
            if (nameText != null)
            {
                nameText.text = contributor.Login;
            }

            // Configura número de contribuições
            if (contributionsText != null)
            {
                string contributionWord = contributor.Contributions == 1 ? "contribuição" : "contribuições";
                contributionsText.text = $"{contributor.Contributions} {contributionWord}";
            }

            // Configura avatar padrão (a imagem será carregada depois)
            if (avatarImage != null && defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }

        /// <summary>
        /// Define a sprite do avatar quando a imagem for baixada.
        /// Chamado pelo GitHubContributorsDisplay após download.
        /// </summary>
        public void SetAvatar(Sprite avatarSprite)
        {
            if (avatarImage != null && avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
        }

        /// <summary>
        /// Define a textura do avatar quando a imagem for baixada.
        /// Converte automaticamente para Sprite.
        /// </summary>
        public void SetAvatarTexture(Texture2D texture)
        {
            if (texture != null)
            {
                // Converte a textura em sprite
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                SetAvatar(sprite);
            }
        }
    }
}

