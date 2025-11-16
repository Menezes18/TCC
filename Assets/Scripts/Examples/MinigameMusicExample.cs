using UnityEngine;

/// <summary>
/// Exemplo de uso do AudioManager em um minigame.
/// Mostra como controlar a música durante diferentes fases do jogo.
/// </summary>
public class MinigameMusicExample : MonoBehaviour
{
    [Header("Músicas Opcionais (Sobrescreve cena)")]
    [Tooltip("Se definido, usa esta música em vez da configurada para a cena")]
    [SerializeField] private AudioClip customStartMusic;
    
    [Tooltip("Música durante contagem regressiva")]
    [SerializeField] private AudioClip countdownMusic;
    
    [Tooltip("Música durante o gameplay")]
    [SerializeField] private AudioClip gameplayMusic;
    
    [Tooltip("Música quando o tempo está acabando")]
    [SerializeField] private AudioClip rushMusic;

    [Header("Configurações")]
    [SerializeField] private bool useCustomMusic = false;
    [SerializeField] private float rushMusicThreshold = 10f; // Segundos restantes
    
    private AudioManager _audioManager;
    private bool _isRushMode = false;

    void Start()
    {
        _audioManager = AudioManager.Instance;
        
        if (_audioManager == null)
        {
            Debug.LogWarning("[MinigameMusicExample] AudioManager não encontrado!");
            return;
        }

        // Se não usar música customizada, o AudioManager automaticamente
        // tocará a música configurada para esta cena
        if (useCustomMusic && customStartMusic != null)
        {
            _audioManager.PlayMusicWithTransition(customStartMusic);
        }
    }

    /// <summary>
    /// Chamado quando o minigame começa (após contagem regressiva)
    /// </summary>
    public void OnGameStart()
    {
        if (_audioManager == null) return;

        if (gameplayMusic != null)
        {
            _audioManager.PlayMusicWithTransition(gameplayMusic);
        }
        else
        {
            // Usa a música da cena configurada no AudioManager
            _audioManager.PlayMusicForCurrentScene();
        }
    }

    /// <summary>
    /// Chamado durante contagem regressiva
    /// </summary>
    public void OnCountdownStart()
    {
        if (_audioManager == null || countdownMusic == null) return;
        
        _audioManager.PlayMusicWithTransition(countdownMusic);
    }

    /// <summary>
    /// Chamado quando o tempo está acabando
    /// </summary>
    public void OnRushMode()
    {
        if (_audioManager == null || _isRushMode) return;
        
        _isRushMode = true;

        if (rushMusic != null)
        {
            // Transição mais rápida para criar urgência
            _audioManager.PlayMusicWithTransition(rushMusic);
        }
        else
        {
            // Aumenta o volume da música atual
            float currentVolume = _audioManager.Volume;
            _audioManager.SetVolume(Mathf.Min(currentVolume + 10f, 100f));
        }
    }

    /// <summary>
    /// Chamado quando o minigame termina
    /// </summary>
    public void OnGameEnd(bool won)
    {
        if (_audioManager == null) return;

        // Para a música com fade out
        _audioManager.StopMusic(1.5f);
        
        // Nota: As cenas de vitória/derrota terão suas próprias músicas
        // configuradas no AudioManager
    }

    /// <summary>
    /// Atualiza baseado no tempo restante
    /// </summary>
    public void UpdateGameTime(float timeRemaining)
    {
        if (!_isRushMode && timeRemaining <= rushMusicThreshold)
        {
            OnRushMode();
        }
    }

    /// <summary>
    /// Pausa a música quando o jogo pausa
    /// </summary>
    public void OnGamePause()
    {
        if (_audioManager != null)
        {
            _audioManager.PauseMusic();
        }
    }

    /// <summary>
    /// Retoma a música quando o jogo volta
    /// </summary>
    public void OnGameResume()
    {
        if (_audioManager != null)
        {
            _audioManager.ResumeMusic();
        }
    }
}
