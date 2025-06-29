using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string exposedParam = "MusicVol";

    const string KEY_MUSIC = "MusicVolume";
    float _volume = 0.7f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _volume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.7f);
        
        ApplyVolume(_volume);
    }

    public float Volume => _volume;

    public void SetVolume(float value)
    {
        _volume = Mathf.Clamp01(value);
        ApplyVolume(_volume);
        PlayerPrefs.SetFloat(KEY_MUSIC, _volume);
    }

    public void PlayLoop(AudioClip clip)
    {
        if (!clip) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayOneShot(AudioClip clip) => musicSource.PlayOneShot(clip);

    void ApplyVolume(float v)
    {
        if (mixer)
            mixer.SetFloat(exposedParam, Mathf.Lerp(-80f, 0f, v <= .0001f ? 0f : v));
        else
            musicSource.volume = v;
    }
}