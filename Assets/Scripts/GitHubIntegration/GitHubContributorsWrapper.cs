using System;
using UnityEngine;

namespace GitHubIntegration
{
    /// <summary>
    /// Classe wrapper para desserializar o array de contribuidores.
    /// 
    /// Justificativa técnica:
    /// O JsonUtility da Unity não consegue desserializar arrays JSON na raiz (ex: [{"login":"user1"}...]).
    /// Esta classe "embrulha" o array em um objeto com uma propriedade 'items', permitindo a desserialização.
    /// 
    /// Processo:
    /// 1. Recebemos o JSON da API: [{"login":"user1", "contributions":50}, ...]
    /// 2. Transformamos em: {"items":[{"login":"user1", "contributions":50}, ...]}
    /// 3. Desserializamos com JsonUtility.FromJson<GitHubContributorsWrapper>(json)
    /// 
    /// Trade-off: Adiciona um passo extra de manipulação de string, mas evita dependências externas
    /// como Newtonsoft.Json, mantendo o projeto mais leve.
    /// </summary>
    [Serializable]
    public class GitHubContributorsWrapper
    {
        /// <summary>
        /// Array de contribuidores retornado pela API do GitHub
        /// </summary>
        [SerializeField] private GitHubContributor[] items;

        public GitHubContributor[] Items => items;

        /// <summary>
        /// Construtor para facilitar criação manual ou testes
        /// </summary>
        public GitHubContributorsWrapper(GitHubContributor[] items)
        {
            this.items = items;
        }

        /// <summary>
        /// Método utilitário para transformar o JSON de array em objeto wrapper.
        /// Exemplo: [{"login":"user"}] -> {"items":[{"login":"user"}]}
        /// </summary>
        /// <param name="arrayJson">JSON em formato de array</param>
        /// <returns>JSON embrulhado em um objeto com propriedade 'items'</returns>
        public static string WrapJsonArray(string arrayJson)
        {
            return $"{{\"items\":{arrayJson}}}";
        }
    }
}

