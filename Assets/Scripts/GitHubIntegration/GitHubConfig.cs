using UnityEngine;

namespace GitHubIntegration
{
    /// <summary>
    /// ScriptableObject para configuração segura do GitHub.
    /// 
    /// Vantagens de usar ScriptableObject:
    /// 1. Separação de dados e lógica (Data-Oriented Design)
    /// 2. Fácil de excluir do controle de versão (.gitignore)
    /// 3. Compartilhável entre múltiplos GameObjects
    /// 4. Editável sem recompilar código
    /// 
    /// Como usar:
    /// 1. Clique com botão direito na pasta Project → Create → GitHub → Config
    /// 2. Preencha os dados
    /// 3. Adicione *.asset ao .gitignore se tiver token sensível
    /// 4. Referencie este asset no script GitHubContributorsDisplay (futuro)
    /// 
    /// Boa prática para segurança:
    /// - Nunca commite tokens no Git
    /// - Use diferentes configs para dev/prod
    /// - Considere criptografar tokens sensíveis
    /// </summary>
    [CreateAssetMenu(fileName = "GitHubConfig", menuName = "GitHub/Config", order = 1)]
    public class GitHubConfig : ScriptableObject
    {
        [Header("Configuração do Repositório")]
        [Tooltip("Nome do dono do repositório")]
        public string owner = "Menezes18";

        [Tooltip("Nome do repositório")]
        public string repo = "TCC";

        [Header("Autenticação (Opcional)")]
        [Tooltip("Token de acesso pessoal do GitHub para repos privados ou aumentar rate limit")]
        [TextArea(3, 5)]
        public string personalAccessToken = "";

        [Header("Configurações Avançadas")]
        [Tooltip("Timeout da requisição em segundos")]
        [Range(5, 60)]
        public int timeoutSeconds = 10;

        [Tooltip("Ativar logs detalhados no Console")]
        public bool verboseLogging = true;

        /// <summary>
        /// Valida se a configuração está correta
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
        }

        /// <summary>
        /// Retorna a URL completa do repositório
        /// </summary>
        public string GetRepositoryUrl()
        {
            return $"https://github.com/{owner}/{repo}";
        }

        /// <summary>
        /// Verifica se tem token configurado
        /// </summary>
        public bool HasToken()
        {
            return !string.IsNullOrWhiteSpace(personalAccessToken);
        }
    }
}

