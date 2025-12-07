using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectFeatures
{
    /// <summary>
    /// Database central com TODAS as features do projeto.
    /// Use o Editor customizado para adicionar features facilmente.
    /// </summary>
    [CreateAssetMenu(fileName = "FeaturesDatabase", menuName = "TCC/Features Database", order = 0)]
    public class FeaturesDatabase : ScriptableObject
    {
        [Header("Todas as Features do Projeto")]
        [Tooltip("Lista completa de features desenvolvidas no TCC")]
        public List<FeatureEntry> features = new List<FeatureEntry>();

        [Header("Configurações")]
        [Tooltip("Título da tela de features")]
        public string tituloTela = "Features do Projeto - TCC";

        [Tooltip("Descrição geral do projeto")]
        [TextArea(3, 5)]
        public string descricaoProjeto = "Confira todas as funcionalidades desenvolvidas neste projeto.";

        /// <summary>
        /// Retorna todas as features.
        /// </summary>
        public List<FeatureEntry> GetAllFeatures()
        {
            return features.Where(f => f != null).ToList();
        }

        /// <summary>
        /// Retorna features filtradas por categoria.
        /// </summary>
        public List<FeatureEntry> GetFeaturesByCategory(FeatureCategory category)
        {
            return features.Where(f => f != null && f.categoria == category).ToList();
        }

        /// <summary>
        /// Retorna features filtradas por status.
        /// </summary>
        public List<FeatureEntry> GetFeaturesByStatus(FeatureStatus status)
        {
            return features.Where(f => f != null && f.status == status).ToList();
        }

        /// <summary>
        /// Retorna features que contém uma tag específica.
        /// </summary>
        public List<FeatureEntry> GetFeaturesByTag(string tag)
        {
            return features.Where(f => f != null && f.HasTag(tag)).ToList();
        }

        /// <summary>
        /// Retorna features por responsável.
        /// </summary>
        public List<FeatureEntry> GetFeaturesByResponsavel(string responsavel)
        {
            return features.Where(f => f != null && 
                f.responsavel.ToLower().Contains(responsavel.ToLower())).ToList();
        }

        /// <summary>
        /// Busca features por texto (busca em título e descrição).
        /// </summary>
        public List<FeatureEntry> SearchFeatures(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllFeatures();

            searchText = searchText.ToLower();

            return features.Where(f => f != null && 
                (f.titulo.ToLower().Contains(searchText) ||
                 f.descricaoCurta.ToLower().Contains(searchText) ||
                 f.tipo.ToLower().Contains(searchText))).ToList();
        }

        /// <summary>
        /// Retorna estatísticas do projeto.
        /// </summary>
        public ProjectStats GetStats()
        {
            var stats = new ProjectStats
            {
                totalFeatures = features.Count,
                concluidas = features.Count(f => f != null && f.status == FeatureStatus.Concluido),
                emAndamento = features.Count(f => f != null && f.status == FeatureStatus.EmAndamento),
                planejadas = features.Count(f => f != null && f.status == FeatureStatus.Planejado),
                porCategoria = new Dictionary<FeatureCategory, int>()
            };

            foreach (FeatureCategory cat in System.Enum.GetValues(typeof(FeatureCategory)))
            {
                stats.porCategoria[cat] = features.Count(f => f != null && f.categoria == cat);
            }

            return stats;
        }
    }

    /// <summary>
    /// Estatísticas do projeto.
    /// </summary>
    public class ProjectStats
    {
        public int totalFeatures;
        public int concluidas;
        public int emAndamento;
        public int planejadas;
        public Dictionary<FeatureCategory, int> porCategoria;
    }
}

