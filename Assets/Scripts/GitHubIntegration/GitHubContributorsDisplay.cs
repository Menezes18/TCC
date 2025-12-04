using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

namespace GitHubIntegration
{
    /// <summary>
    /// Script principal para exibir ranking de contribuições do GitHub na Unity.
    /// Sistema visual com prefabs e fotos dos contribuidores.
    /// 
    /// Arquitetura:
    /// - Busca dados da API do GitHub
    /// - Instancia prefabs para cada contribuidor
    /// - Baixa fotos dos avatares automaticamente
    /// - Sistema simples e visual
    /// </summary>
    public class GitHubContributorsDisplay : MonoBehaviour
    {
        #region Campos Configuráveis no Inspector

        [Header("Configurações do Repositório")]
        [Tooltip("Nome do dono do repositório (owner) no GitHub")]
        [SerializeField] private string owner = "Menezes18";

        [Tooltip("Nome do repositório no GitHub")]
        [SerializeField] private string repo = "TCC";

        [Header("Configurações da UI")]
        [Tooltip("Prefab do item de contribuidor (deve ter o script ContributorUIItem)")]
        [SerializeField] private GameObject contributorItemPrefab;

        [Tooltip("Container onde os itens serão instanciados (ex: Content de um ScrollView)")]
        [SerializeField] private Transform contributorsContainer;

        [Tooltip("Texto de feedback (loading/erro) - OPCIONAL")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Configurações Avançadas (Opcional)")]
        [Tooltip("Token de acesso pessoal do GitHub (deixe vazio para repositórios públicos)")]
        [SerializeField] private string personalAccessToken = "";

        [Tooltip("Timeout da requisição em segundos")]
        [SerializeField] private int timeoutSeconds = 10;

        [Tooltip("Exibir mensagens de log detalhadas no Console")]
        [SerializeField] private bool verboseLogging = true;

        [Tooltip("Baixar fotos dos avatares automaticamente")]
        [SerializeField] private bool downloadAvatars = true;

        #endregion

        #region Constantes

        // URL base da API do GitHub
        private const string GITHUB_API_BASE_URL = "https://api.github.com";
        
        // User-Agent é obrigatório pela API do GitHub
        private const string USER_AGENT = "Unity-GitHub-Stats-TCC";

        #endregion

        #region Variáveis Privadas

        // Lista de itens instanciados para facilitar limpeza
        private List<GameObject> instantiatedItems = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Validações iniciais
            if (!ValidateConfiguration())
            {
                return;
            }

            // Inicia a busca dos contribuidores
            StartCoroutine(FetchAndDisplayContributors());
        }

        #endregion

        #region Métodos Principais

        /// <summary>
        /// Valida a configuração do script.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (contributorItemPrefab == null)
            {
                Debug.LogError("[GitHubContributorsDisplay] Prefab do item não foi configurado no Inspector!");
                return false;
            }

            if (contributorsContainer == null)
            {
                Debug.LogError("[GitHubContributorsDisplay] Container não foi configurado no Inspector!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            {
                Debug.LogError("[GitHubContributorsDisplay] Owner e Repo não podem estar vazios!");
                DisplayFeedback("Configuração inválida: Owner ou Repo não definidos.", true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Coroutine principal que orquestra todo o fluxo.
        /// </summary>
        private IEnumerator FetchAndDisplayContributors()
        {
            // Limpa items anteriores
            ClearPreviousItems();

            // Mostra mensagem de carregamento
            DisplayFeedback($"Carregando contribuições de {owner}/{repo}...", false);

            // Monta a URL da API
            string url = BuildApiUrl();
            LogVerbose($"Buscando contribuidores em: {url}");

            // Cria e configura a requisição
            using (UnityWebRequest request = CreateWebRequest(url))
            {
                // Envia a requisição e aguarda a resposta
                yield return request.SendWebRequest();

                // Processa o resultado
                if (HasRequestFailed(request))
                {
                    HandleRequestError(request);
                    yield break;
                }

                // Processa os dados
                GitHubContributor[] contributors = ProcessContributorsData(request.downloadHandler.text);
                
                if (contributors != null && contributors.Length > 0)
                {
                    // Cria os items visuais
                    CreateContributorItems(contributors);

                    // Baixa avatares se configurado
                    if (downloadAvatars)
                    {
                        yield return StartCoroutine(DownloadAllAvatars(contributors));
                    }

                    DisplayFeedback($"✓ {contributors.Length} contribuidores carregados!", false);
                    
                    // Esconde feedback após 2 segundos
                    yield return new WaitForSeconds(2f);
                    HideFeedback();
                }
            }
        }

        /// <summary>
        /// Limpa items criados anteriormente.
        /// </summary>
        private void ClearPreviousItems()
        {
            foreach (GameObject item in instantiatedItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            instantiatedItems.Clear();
        }

        /// <summary>
        /// Monta a URL da API de contribuidores do GitHub.
        /// Endpoint: GET /repos/{owner}/{repo}/contributors
        /// </summary>
        private string BuildApiUrl()
        {
            return $"{GITHUB_API_BASE_URL}/repos/{owner}/{repo}/contributors";
        }

        /// <summary>
        /// Cria e configura o UnityWebRequest com headers necessários.
        /// 
        /// Headers importantes:
        /// - User-Agent: Obrigatório pelo GitHub (identificação da aplicação)
        /// - Authorization: Opcional, para aumentar rate limits ou acessar repos privados
        /// - Accept: Especifica a versão da API e formato de resposta
        /// 
        /// Referência: https://docs.github.com/en/rest/overview/resources-in-the-rest-api
        /// </summary>
        private UnityWebRequest CreateWebRequest(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);

            // Configura timeout
            request.timeout = timeoutSeconds;

            // Headers obrigatórios/recomendados
            request.SetRequestHeader("User-Agent", USER_AGENT);
            request.SetRequestHeader("Accept", "application/vnd.github.v3+json");

            // Adiciona token de autenticação se fornecido
            if (!string.IsNullOrWhiteSpace(personalAccessToken))
            {
                request.SetRequestHeader("Authorization", $"token {personalAccessToken}");
                LogVerbose("Token de autenticação configurado.");
            }

            return request;
        }

        /// <summary>
        /// Verifica se a requisição falhou.
        /// Unity 2020+: usar result ao invés de isNetworkError/isHttpError (deprecated)
        /// </summary>
        private bool HasRequestFailed(UnityWebRequest request)
        {
            #if UNITY_2020_1_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
            #else
            return request.isNetworkError || request.isHttpError;
            #endif
        }

        /// <summary>
        /// Trata erros de requisição HTTP/rede.
        /// </summary>
        private void HandleRequestError(UnityWebRequest request)
        {
            long statusCode = request.responseCode;
            string errorMessage = "";

            switch (statusCode)
            {
                case 403:
                    errorMessage = "Limite de requisições excedido. Configure um token no Inspector.";
                    break;

                case 404:
                    errorMessage = $"Repositório '{owner}/{repo}' não encontrado.";
                    break;

                case 0:
                    errorMessage = "Sem conexão com a internet ou timeout.";
                    break;

                default:
                    errorMessage = $"Erro ao buscar dados (código {statusCode}).";
                    break;
            }

            DisplayFeedback(errorMessage, true);
        }

        /// <summary>
        /// Processa o JSON e retorna array de contribuidores ordenado.
        /// </summary>
        private GitHubContributor[] ProcessContributorsData(string jsonResponse)
        {
            try
            {
                LogVerbose($"JSON recebido (primeiros 200 chars): {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");

                // Embrulha o array JSON
                string wrappedJson = GitHubContributorsWrapper.WrapJsonArray(jsonResponse);

                // Desserializa
                GitHubContributorsWrapper wrapper = JsonUtility.FromJson<GitHubContributorsWrapper>(wrappedJson);

                if (wrapper?.Items == null || wrapper.Items.Length == 0)
                {
                    DisplayFeedback("Nenhum contribuidor encontrado para este repositório.", true);
                    Debug.LogWarning("[GitHubContributorsDisplay] Array de contribuidores vazio ou nulo.");
                    return null;
                }

                // Ordena por contribuições (maior para menor)
                GitHubContributor[] sortedContributors = wrapper.Items
                    .OrderByDescending(c => c.Contributions)
                    .ToArray();

                LogVerbose($"Total de contribuidores encontrados: {sortedContributors.Length}");
                return sortedContributors;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHubContributorsDisplay] Erro ao processar JSON: {ex.Message}\n{ex.StackTrace}");
                DisplayFeedback("Erro ao processar dados do GitHub.", true);
                return null;
            }
        }

        /// <summary>
        /// Cria os itens visuais para cada contribuidor.
        /// </summary>
        private void CreateContributorItems(GitHubContributor[] contributors)
        {
            for (int i = 0; i < contributors.Length; i++)
            {
                // Instancia o prefab
                GameObject itemObj = Instantiate(contributorItemPrefab, contributorsContainer);
                instantiatedItems.Add(itemObj);

                // Configura o componente ContributorUIItem
                ContributorUIItem item = itemObj.GetComponent<ContributorUIItem>();
                if (item != null)
                {
                    item.Setup(i + 1, contributors[i]);
                }
                else
                {
                    Debug.LogWarning($"[GitHubContributorsDisplay] Prefab não tem o componente ContributorUIItem!");
                }
            }

            LogVerbose($"{contributors.Length} items criados com sucesso!");
        }

        /// <summary>
        /// Baixa todos os avatares dos contribuidores.
        /// </summary>
        private IEnumerator DownloadAllAvatars(GitHubContributor[] contributors)
        {
            LogVerbose("Iniciando download de avatares...");

            for (int i = 0; i < contributors.Length && i < instantiatedItems.Count; i++)
            {
                GitHubContributor contributor = contributors[i];
                GameObject itemObj = instantiatedItems[i];

                if (itemObj != null && !string.IsNullOrEmpty(contributor.AvatarUrl))
                {
                    yield return StartCoroutine(DownloadAvatar(contributor.AvatarUrl, itemObj));
                }
            }

            LogVerbose("Download de avatares concluído!");
        }

        /// <summary>
        /// Baixa um avatar específico e aplica no item.
        /// </summary>
        private IEnumerator DownloadAvatar(string url, GameObject itemObj)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();

                if (!HasRequestFailed(request))
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    
                    if (texture != null && itemObj != null)
                    {
                        ContributorUIItem item = itemObj.GetComponent<ContributorUIItem>();
                        if (item != null)
                        {
                            item.SetAvatarTexture(texture);
                        }
                    }
                }
                else
                {
                    LogVerbose($"Erro ao baixar avatar: {url}");
                }
            }
        }

        #endregion

        #region Métodos de UI

        /// <summary>
        /// Exibe feedback para o usuário (loading, sucesso, erro).
        /// </summary>
        private void DisplayFeedback(string message, bool isError)
        {
            if (feedbackText != null)
            {
                if (isError)
                {
                    feedbackText.text = $"<color=red>✗ {message}</color>";
                }
                else
                {
                    feedbackText.text = $"<i>{message}</i>";
                }
                feedbackText.gameObject.SetActive(true);
            }

            if (isError)
            {
                Debug.LogError($"[GitHubContributorsDisplay] {message}");
            }
        }

        /// <summary>
        /// Esconde o texto de feedback.
        /// </summary>
        private void HideFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Métodos Utilitários

        /// <summary>
        /// Loga mensagens apenas se verbose logging estiver ativado.
        /// Útil para debug sem poluir o console em produção.
        /// </summary>
        private void LogVerbose(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[GitHubContributorsDisplay] {message}");
            }
        }

        #endregion

        #region Métodos Públicos (API do Script)

        /// <summary>
        /// Método público para atualizar o ranking manualmente.
        /// Útil para botão de "Atualizar" na UI.
        /// </summary>
        public void RefreshRanking()
        {
            StopAllCoroutines(); // Para requisições em andamento
            StartCoroutine(FetchAndDisplayContributors());
        }

        /// <summary>
        /// Método público para mudar o repositório em runtime.
        /// Útil para sistemas onde o usuário pode escolher diferentes repos.
        /// </summary>
        public void SetRepository(string newOwner, string newRepo)
        {
            if (string.IsNullOrWhiteSpace(newOwner) || string.IsNullOrWhiteSpace(newRepo))
            {
                Debug.LogWarning("[GitHubContributorsDisplay] Owner e Repo não podem estar vazios!");
                return;
            }

            owner = newOwner;
            repo = newRepo;
            RefreshRanking();
        }

        #endregion
    }
}

