using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public enum CutsceneID
{
    None,
    CreateRoom,
    JoinRoom,
    JoinListRoom
}

public class ManagerCutscene : MonoBehaviour
{
    private static ManagerCutscene _instance;
    
    public static ManagerCutscene Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ManagerCutscene>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("ManagerCutscene");
                    _instance = go.AddComponent<ManagerCutscene>();
                }
            }
            return _instance;
        }
    }

    [Header("Cutscene Settings")]
    public CutsceneID id;
    public UnityEvent callCreateRoomEvent;
    public UnityEvent callJoinRoomEvent;


    public UnityEvent callJoinListRoomEvent;
    public UnityEvent callCutsceneEvent;

    [Header("Audio Volume Control")]
    [SerializeField] private List<AudioSource> audioSources = new List<AudioSource>();
    
    [Range(0f, 1f)]
    [SerializeField] private float cutsceneVolumeMultiplier = 0.3f;
    
    [SerializeField] private float volumeFadeDuration = 0.5f;

    private Dictionary<AudioSource, float> _originalVolumes = new Dictionary<AudioSource, float>();
    private bool _isVolumeReduced = false;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (audioSources.Count == 0)
        {
            TryFindAudioManager();
        }
        
        StoreOriginalVolumes();
    }


    public void setCutsceneID(CutsceneID id)
    {
        this.id = id;
    }

    public void callCutsceneJoinListRoomEvent()
    {
        LowerVolumeForCutscene();
        callCutsceneEvent?.Invoke();
    }
    
    public void callCutscene()
    {
        LowerVolumeForCutscene();

        if (id == CutsceneID.CreateRoom)
        {
            callCreateRoomEvent?.Invoke();
            Debug.LogError("callCutsceneCreateRoomEvent");
        }
        else if (id == CutsceneID.JoinRoom)
        {
            callJoinRoomEvent?.Invoke();
            Debug.LogError("callCutsceneJoinRoomEvent");
        }
        else if (id == CutsceneID.JoinListRoom)
        {
            Debug.LogError("callCutsceneJoinListRoomEvent");
            callJoinListRoomEvent?.Invoke();
        }
    }

    public void setCutsceneIDByInt(int id)
    {
        switch (id)
        {
            case 0:
                this.id = CutsceneID.None;
                break;
            case 1:
                this.id = CutsceneID.CreateRoom;
                break;
            case 2:
                this.id = CutsceneID.JoinRoom;
                break;
            case 3:
                this.id = CutsceneID.JoinListRoom;
                break;
        }
        LowerVolumeForCutscene();
    }

    public static void CallCutsceneByID(CutsceneID cutsceneID)
    {
        if (Instance != null)
        {
            Instance.setCutsceneID(cutsceneID);
            Instance.callCutscene();
        }
    }


    public static void CallCurrentCutscene()
    {
        if (Instance != null)
        {
            Instance.callCutscene();
        }
    }

    #region Audio Volume Control


    private void TryFindAudioManager()
    {
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            Debug.Log("[ManagerCutscene] AudioManager encontrado. Configure os AudioSources manualmente no Inspector.");
        }
    }


    private void StoreOriginalVolumes()
    {
        _originalVolumes.Clear();
        foreach (var audioSource in audioSources)
        {
            if (audioSource != null && !_originalVolumes.ContainsKey(audioSource))
            {
                _originalVolumes[audioSource] = audioSource.volume;
            }
        }
    }


    public void LowerVolumeForCutscene()
    {
        if (_isVolumeReduced) return;

        StoreOriginalVolumes();

        StartCoroutine(FadeVolumeToCutsceneLevel());
    }


    public void RestoreVolume()
    {
        if (!_isVolumeReduced) return;

        StartCoroutine(FadeVolumeToOriginal());
    }


    private IEnumerator FadeVolumeToCutsceneLevel()
    {
        _isVolumeReduced = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < volumeFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / volumeFadeDuration);
            
            foreach (var kvp in _originalVolumes)
            {
                if (kvp.Key != null)
                {
                    float targetVolume = kvp.Value * cutsceneVolumeMultiplier;
                    kvp.Key.volume = Mathf.Lerp(kvp.Value, targetVolume, t);
                }
            }
            
            yield return null;
        }
        

        foreach (var kvp in _originalVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.volume = kvp.Value * cutsceneVolumeMultiplier;
            }
        }
    }


    private IEnumerator FadeVolumeToOriginal()
    {
        _isVolumeReduced = false;
        
        float elapsedTime = 0f;
        Dictionary<AudioSource, float> startVolumes = new Dictionary<AudioSource, float>();
        
        // Captura os volumes atuais como ponto de partida
        foreach (var kvp in _originalVolumes)
        {
            if (kvp.Key != null)
            {
                startVolumes[kvp.Key] = kvp.Key.volume;
            }
        }
        
        while (elapsedTime < volumeFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / volumeFadeDuration);
            
            foreach (var kvp in _originalVolumes)
            {
                if (kvp.Key != null && startVolumes.ContainsKey(kvp.Key))
                {
                    float startVolume = startVolumes[kvp.Key];
                    kvp.Key.volume = Mathf.Lerp(startVolume, kvp.Value, t);
                }
            }
            
            yield return null;
        }
        
        foreach (var kvp in _originalVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.volume = kvp.Value;
            }
        }
    }


    public void AddAudioSource(AudioSource audioSource)
    {
        if (audioSource != null && !audioSources.Contains(audioSource))
        {
            audioSources.Add(audioSource);
            if (!_originalVolumes.ContainsKey(audioSource))
            {
                _originalVolumes[audioSource] = audioSource.volume;
            }
        }
    }


    public void RemoveAudioSource(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSources.Remove(audioSource);
            _originalVolumes.Remove(audioSource);
        }
    }

    #endregion
}
