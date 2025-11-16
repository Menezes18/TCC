using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    #region Classes Internas
    
    [System.Serializable]
    public class SceneMusic
    {
        [Tooltip("Nome da cena (ex: HotPotato, Menu, Lobby)")]
        public string sceneName;
        
        [Tooltip("Clipe de áudio para esta cena")]
        public AudioClip musicClip;
        
        [Tooltip("Volume específico (0-1). Use 1.0 para normal, 0.7 para mais suave")]
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;
    }
    
    #endregion

    #region Singleton
    
    public static AudioManager Instance { get; private set; }
    
    #endregion

    #region Configuração Inspector

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Audio Mixer (Opcional)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string exposedParam = "MusicVol";

    [Header("Configuração de Músicas por Cena")]
    [Tooltip("Preencha com: Scene Name = nome da cena | Music Clip = música | Volume = 0.7-1.0")]
    [SerializeField] private List<SceneMusic> sceneMusicList = new List<SceneMusic>();

    [Header("Playlist do Lobby (Selecionável)")]
    [Tooltip("Adicione várias músicas aqui. O jogador pode escolher qual tocar no lobby.")]
    [SerializeField] private List<AudioClip> lobbyMusicPlaylist = new List<AudioClip>();

    [Header("Configurações Gerais")]
    [SerializeField] private bool autoPlayOnSceneLoad = true;
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private bool fadeInOnStart = true;
    [SerializeField] private float fadeInDuration = 2f;

    #endregion

    #region Constantes e Variáveis Privadas
    
    const string KEY_MUSIC = "MusicVolume";
    const string KEY_SELECTED_LOBBY_MUSIC = "SelectedLobbyMusic";
    
    private float _volume = 0.7f;
    private string _currentSceneName;
    private AudioClip _currentClip;
    private bool _isTransitioning = false;
    private int _selectedLobbyMusicIndex = 0;
    private Dictionary<string, SceneMusic> _sceneMusicDict;
    
    #endregion
    
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        
        Debug.LogError("[AudioManager] Instância " + gameObject.name + " criada.");

        Instance = this;
        DontDestroyOnLoad(gameObject);


        _volume = PlayerPrefs.GetFloat(KEY_MUSIC, 70f);
        _selectedLobbyMusicIndex = PlayerPrefs.GetInt(KEY_SELECTED_LOBBY_MUSIC, 0);
        
        ApplyVolume(_volume);
        

        BuildSceneMusicDictionary();
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        if (autoPlayOnSceneLoad && fadeInOnStart)
        {
            musicSource.volume = 0f;
            PlayMusicForCurrentScene();
            StartCoroutine(FadeIn(fadeInDuration));
        }
        else if (autoPlayOnSceneLoad)
        {
            PlayMusicForCurrentScene();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void BuildSceneMusicDictionary()
    {
        _sceneMusicDict = new Dictionary<string, SceneMusic>();
        foreach (var sceneMusic in sceneMusicList)
        {
            if (!string.IsNullOrEmpty(sceneMusic.sceneName) && sceneMusic.musicClip != null)
            {
                _sceneMusicDict[sceneMusic.sceneName.ToLower()] = sceneMusic;
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoPlayOnSceneLoad && mode == LoadSceneMode.Single)
        {
            _currentSceneName = scene.name;
            PlayMusicForCurrentScene();
        }
    }

    #endregion
    
    #region Volume Control

    public float Volume => _volume;

    public void SetVolume(float value)
    {
        _volume = Mathf.Clamp(value, 0f, 100f);
        ApplyVolume(_volume);
        PlayerPrefs.SetFloat(KEY_MUSIC, _volume);
        PlayerPrefs.Save();
    }

    void ApplyVolume(float v)
    {
        v = Mathf.Clamp(v / 100f, 0f, 1f);
        if (musicSource != null)
        {
            musicSource.volume = v;
        }
    }

    #endregion

    #region Music Playback

    public void PlayMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayMusicForScene(sceneName);
    }


    public void PlayMusicForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        string sceneKey = sceneName.ToLower();
        
        if (IsLobbyScene(sceneName) && _selectedLobbyMusicIndex >= 0 && 
            _selectedLobbyMusicIndex < lobbyMusicPlaylist.Count)
        {
            AudioClip selectedClip = lobbyMusicPlaylist[_selectedLobbyMusicIndex];
            if (selectedClip != null)
            {
                PlayMusicWithTransition(selectedClip, 1f);
                return;
            }
        }

        if (_sceneMusicDict.TryGetValue(sceneKey, out SceneMusic sceneMusic))
        {
            if (sceneMusic.musicClip != null)
            {
                PlayMusicWithTransition(sceneMusic.musicClip, sceneMusic.volumeMultiplier);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Nenhuma música configurada para a cena: {sceneName}");
        }
    }


    public void PlayMusicWithTransition(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;
        
        if (_currentClip == clip && musicSource.isPlaying) return;

        _currentClip = clip;

        if (_isTransitioning)
        {
            StopAllCoroutines();
        }

        if (musicSource.isPlaying)
        {
            StartCoroutine(CrossfadeMusic(clip, volumeMultiplier));
        }
        else
        {
            PlayMusicImmediate(clip, volumeMultiplier);
        }
    }


    public void PlayMusicImmediate(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || musicSource == null) return;

        _currentClip = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        
        float targetVolume = (_volume / 100f) * volumeMultiplier;
        musicSource.volume = targetVolume;
        
        musicSource.Play();
    }


    public void StopMusic(float fadeDuration = 1f)
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOut(fadeDuration));
        }
    }


    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }


    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }

    #endregion

    #region Lobby Music Selection


    public List<AudioClip> GetLobbyPlaylist() => new List<AudioClip>(lobbyMusicPlaylist);


    public int GetSelectedLobbyMusicIndex() => _selectedLobbyMusicIndex;


    public void SelectLobbyMusic(int index)
    {
        if (index < 0 || index >= lobbyMusicPlaylist.Count)
        {
            Debug.LogWarning($"[AudioManager] Índice de música inválido: {index}");
            return;
        }

        _selectedLobbyMusicIndex = index;
        PlayerPrefs.SetInt(KEY_SELECTED_LOBBY_MUSIC, _selectedLobbyMusicIndex);
        PlayerPrefs.Save();

        if (IsLobbyScene(SceneManager.GetActiveScene().name))
        {
            AudioClip selectedClip = lobbyMusicPlaylist[_selectedLobbyMusicIndex];
            if (selectedClip != null)
            {
                PlayMusicWithTransition(selectedClip, 1f);
            }
        }
    }


    public void SelectNextLobbyMusic()
    {
        if (lobbyMusicPlaylist.Count == 0) return;
        
        int nextIndex = (_selectedLobbyMusicIndex + 1) % lobbyMusicPlaylist.Count;
        SelectLobbyMusic(nextIndex);
    }


    public void SelectPreviousLobbyMusic()
    {
        if (lobbyMusicPlaylist.Count == 0) return;
        
        int prevIndex = _selectedLobbyMusicIndex - 1;
        if (prevIndex < 0) prevIndex = lobbyMusicPlaylist.Count - 1;
        SelectLobbyMusic(prevIndex);
    }


    public string GetSelectedLobbyMusicName()
    {
        if (_selectedLobbyMusicIndex >= 0 && _selectedLobbyMusicIndex < lobbyMusicPlaylist.Count)
        {
            AudioClip clip = lobbyMusicPlaylist[_selectedLobbyMusicIndex];
            return clip != null ? clip.name : "Desconhecido";
        }
        return "Nenhuma";
    }

    #endregion

    #region Sound Effects

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else if (musicSource != null)
        {
            musicSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Utilities

    private bool IsLobbyScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        
        string lower = sceneName.ToLower();
        return lower.Contains("lobby") || lower.Contains("menu") || lower == "mainmenu";
    }


    public bool HasMusicForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return _sceneMusicDict.ContainsKey(sceneName.ToLower());
    }


    public string GetCurrentMusicInfo()
    {
        if (_currentClip != null)
        {
            return $"Tocando: {_currentClip.name}";
        }
        return "Nenhuma música tocando";
    }

    #endregion

    #region Coroutines

    IEnumerator CrossfadeMusic(AudioClip newClip, float volumeMultiplier)
    {
        _isTransitioning = true;
        
        float targetVolume = (_volume / 100f) * volumeMultiplier;
        float halfDuration = crossfadeDuration * 0.5f;
        
        // Fade out da música atual
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        // Trocar a música
        musicSource.clip = newClip;
        musicSource.Play();
        
        // Fade in da nova música
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
        _isTransitioning = false;
    }

    IEnumerator FadeOut(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = startVolume;
    }

    IEnumerator FadeIn(float duration)
    {
        float targetVolume = _volume / 100f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
    }

    #endregion
}