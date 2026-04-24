using UnityEngine;

public class MusicController : MonoBehaviour
{
    private const string MusicVolumePrefsKey = "Settings.MusicVolume";

    public static MusicController Instance { get; private set; }
    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePrefsKey, 1f));

    [Header("Audio Clips")]
    [Tooltip("The audio clip that plays once at the start.")]
    public AudioClip introClip;
    
    [Tooltip("The audio clip that loops forever after the intro.")]
    public AudioClip loopClip;

    private AudioSource introSource;
    private AudioSource loopSource;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (introClip == null || loopClip == null)
        {
            Debug.LogError("MusicController: Please assign both Intro and Loop clips in the inspector.");
            return;
        }

        introSource = gameObject.AddComponent<AudioSource>();
        loopSource = gameObject.AddComponent<AudioSource>();

        introSource.clip = introClip;
        introSource.loop = false;
        introSource.playOnAwake = false;

        loopSource.clip = loopClip;
        loopSource.loop = true;
        loopSource.playOnAwake = false;

        ApplyVolume(Volume);
        PlayMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayMusic()
    {
        double dspTime = AudioSettings.dspTime;
        double introDuration = (double)introClip.samples / introClip.frequency;

        introSource.PlayScheduled(dspTime + 0.1f);
        loopSource.PlayScheduled(dspTime + 0.1f + introDuration);
    }
    
    public void StopMusic()
    {
        introSource.Stop();
        loopSource.Stop();
    }

    public static void SetVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumePrefsKey, clampedVolume);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.ApplyVolume(clampedVolume);
        }
    }

    private void ApplyVolume(float volume)
    {
        if (introSource != null)
        {
            introSource.volume = volume;
        }

        if (loopSource != null)
        {
            loopSource.volume = volume;
        }
    }
}
