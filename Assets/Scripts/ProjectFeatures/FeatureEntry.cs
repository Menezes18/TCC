using UnityEngine;

namespace ProjectFeatures
{
    /// <summary>
    /// Representa uma feature/funcionalidade do projeto.
    /// Cada coisa feita no TCC vira uma entrada dessa.
    /// </summary>
    [CreateAssetMenu(fileName = "Nova Feature", menuName = "TCC/Feature Entry", order = 0)]
    public class FeatureEntry : ScriptableObject
    {
        [Header("Informações Básicas")]
        [Tooltip("Nome da feature")]
        public string titulo = "Nova Feature";

        [Tooltip("Categoria principal")]
        public FeatureCategory categoria = FeatureCategory.Geral;

        [Tooltip("Tipo mais específico (ex: Sistema, Mecânica, UI, Ferramenta)")]
        public string tipo = "Sistema";

        [Header("Descrição")]
        [Tooltip("Descrição curta (1-2 frases)")]
        [TextArea(2, 3)]
        public string descricaoCurta = "";

        [Tooltip("Descrição detalhada (opcional)")]
        [TextArea(4, 8)]
        public string descricaoLonga = "";

        [Header("Responsabilidade")]
        [Tooltip("Quem fez essa feature")]
        public string responsavel = "Equipe TCC";

        [Tooltip("Tags para filtros adicionais (ex: Mirror, Minigame, Menu)")]
        public string[] tags = new string[0];

        [Header("Status")]
        [Tooltip("Estado atual da feature")]
        public FeatureStatus status = FeatureStatus.Concluido;

        [Header("Visual (Opcional)")]
        [Tooltip("Imagem ou ícone representando a feature")]
        public Sprite icone;

        [Tooltip("Screenshot da feature em ação")]
        public Sprite screenshot;

        /// <summary>
        /// Retorna um nome amigável da categoria.
        /// </summary>
        public string GetCategoriaNome()
        {
            return categoria switch
            {
                FeatureCategory.Programacao => "Programação",
                FeatureCategory.Mecanica => "Mecânica",
                FeatureCategory.VFX => "VFX",
                FeatureCategory.Arte => "Arte",
                FeatureCategory.Geral => "Geral",
                _ => "Geral"
            };
        }

        /// <summary>
        /// Retorna um nome amigável do status.
        /// </summary>
        public string GetStatusNome()
        {
            return status switch
            {
                FeatureStatus.Planejado => "Planejado",
                FeatureStatus.EmAndamento => "Em Andamento",
                FeatureStatus.Concluido => "Concluído",
                _ => "Concluído"
            };
        }

        /// <summary>
        /// Retorna a cor associada ao status.
        /// </summary>
        public Color GetStatusColor()
        {
            return status switch
            {
                FeatureStatus.Planejado => new Color(1f, 0.8f, 0f), // Amarelo
                FeatureStatus.EmAndamento => new Color(0f, 0.7f, 1f), // Azul
                FeatureStatus.Concluido => new Color(0.3f, 1f, 0.3f), // Verde
                _ => Color.white
            };
        }

        /// <summary>
        /// Verifica se a feature contém uma tag específica.
        /// </summary>
        public bool HasTag(string tag)
        {
            if (tags == null || tags.Length == 0) return false;
            
            foreach (string t in tags)
            {
                if (t.ToLower().Contains(tag.ToLower()))
                    return true;
            }
            return false;
        }
    }
}

