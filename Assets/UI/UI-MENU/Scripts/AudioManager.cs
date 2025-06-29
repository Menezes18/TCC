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

        _volume = PlayerPrefs.GetFloat(KEY_MUSIC, 10f);
        
        ApplyVolume(_volume);
    }

    public float Volume => _volume;

    public void SetVolume(float value)
    {
        _volume = value;
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
        v = v / 100f;
        musicSource.volume = v;
    }
}