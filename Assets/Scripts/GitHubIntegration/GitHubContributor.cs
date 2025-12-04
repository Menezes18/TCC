using System;
using UnityEngine;

namespace GitHubIntegration
{

    [Serializable]
    public class GitHubContributor
    {
        /// <summary>
        /// Nome de usuário (login) do contribuidor no GitHub
        /// </summary>
        [SerializeField] private string login;

        /// <summary>
        /// Número total de contribuições (commits) deste usuário no repositório
        /// </summary>
        [SerializeField] private int contributions;

        /// <summary>
        /// URL do avatar do usuário (pode ser útil para exibir imagens no futuro)
        /// </summary>
        [SerializeField] private string avatar_url;

        /// <summary>
        /// Tipo de conta (geralmente "User")
        /// </summary>
        [SerializeField] private string type;

        // Propriedades públicas somente leitura
        public string Login => login;
        public int Contributions => contributions;
        public string AvatarUrl => avatar_url;
        public string Type => type;

        /// <summary>
        /// Construtor para facilitar testes unitários ou criação manual
        /// </summary>
        public GitHubContributor(string login, int contributions, string avatarUrl = "", string type = "User")
        {
            this.login = login;
            this.contributions = contributions;
            this.avatar_url = avatarUrl;
            this.type = type;
        }
    }
}

