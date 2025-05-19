using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [Header("Referências de UI")]
    [SerializeField] private GameObject panel;   
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    private AsyncOperation op;
    private string targetSceneName;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 1) Garante que este objeto persista entre cenas
            DontDestroyOnLoad(gameObject);
            
            // 2) Destaca o painel (que por padrão está na hierarquia da cena)
            if (panel != null)
            {
                panel.transform.SetParent(null);
                DontDestroyOnLoad(panel);
                panel.SetActive(false);
            }
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// Inicia o carregamento assíncrono com timeout de 10s.
    /// </summary>
    public void Show(string sceneName)
    {
        targetSceneName = sceneName;
        if (panel != null) panel.SetActive(true);
        StartCoroutine(LoadWithTimeout());
    }

    private IEnumerator LoadWithTimeout()
    {
        op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        float maxWait = 120f;

        // Atualiza barra até 90% ou até estourar o timeout
        while (op.progress < 0.9f && elapsed < maxWait)
        {
            elapsed += Time.unscaledDeltaTime;
            float prog = op.progress / 0.9f;
            if (progressBar != null) progressBar.value = prog;
            if (progressText != null) progressText.text = $"{(int)(prog * 100)}%";
            yield return null;
        }

        // Se timeout, força a barra para 100%
        if (elapsed >= maxWait)
        {
            if (progressBar != null) progressBar.value = 1f;
            if (progressText != null) progressText.text = "100%";
        }

        // 3) Esconde o painel ANTES de ativar a cena, evitando erros
        if (panel != null) panel.SetActive(false);

        // 4) Finalmente permite que a Unity faça a troca
        op.allowSceneActivation = true;
    }

    /// <summary>
    /// Caso você queira esconder manualmente em outro ponto.
    /// </summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
