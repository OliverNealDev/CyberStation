using UnityEngine;

public class MusicController : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("The audio clip that plays once at the start.")]
    public AudioClip introClip;
    
    [Tooltip("The audio clip that loops forever after the intro.")]
    public AudioClip loopClip;

    private AudioSource introSource;
    private AudioSource loopSource;

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

        PlayMusic();
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
}