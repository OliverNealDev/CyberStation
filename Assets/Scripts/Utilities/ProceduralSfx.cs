using UnityEngine;

public enum HudSound
{
    DirectiveComplete,
    EventWarning,
    EventSuccess,
    EventFailure,
    DayReport
}

// Short synthesised cues for the new HUD. Generated once at startup rather than shipped as
// audio files, and deliberately soft sine tones so they sit under the existing SFX set.
public static class ProceduralSfx
{
    private const int SampleRate = 44100;

    private static AudioClip directiveComplete;
    private static AudioClip eventWarning;
    private static AudioClip eventSuccess;
    private static AudioClip eventFailure;
    private static AudioClip dayReport;
    private static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        source = null;
    }

    public static void Play(HudSound sound)
    {
        if (source == null)
        {
            GameObject sourceObject = new GameObject("ProceduralSfx");
            Object.DontDestroyOnLoad(sourceObject);
            source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
        }

        AudioClip clip = GetClip(sound);
        if (clip != null)
        {
            source.PlayOneShot(clip, 0.55f * SoundEffectController.Volume);
        }
    }

    private static AudioClip GetClip(HudSound sound)
    {
        switch (sound)
        {
            case HudSound.DirectiveComplete:
                return directiveComplete != null ? directiveComplete : directiveComplete = CreateArpeggio("DirectiveComplete", new[] { 659.25f, 830.61f, 987.77f, 1318.51f }, 0.075f, 0.35f);
            case HudSound.EventWarning:
                return eventWarning != null ? eventWarning : eventWarning = CreateArpeggio("EventWarning", new[] { 880f, 698.46f, 880f, 698.46f }, 0.12f, 0.12f);
            case HudSound.EventSuccess:
                return eventSuccess != null ? eventSuccess : eventSuccess = CreateArpeggio("EventSuccess", new[] { 523.25f, 783.99f, 1046.5f }, 0.09f, 0.3f);
            case HudSound.EventFailure:
                return eventFailure != null ? eventFailure : eventFailure = CreateArpeggio("EventFailure", new[] { 392f, 311.13f, 233.08f }, 0.12f, 0.25f);
            case HudSound.DayReport:
                return dayReport != null ? dayReport : dayReport = CreateArpeggio("DayReport", new[] { 587.33f, 739.99f, 880f, 1174.66f, 1479.98f }, 0.06f, 0.45f);
            default:
                return null;
        }
    }

    private static AudioClip CreateArpeggio(string name, float[] frequencies, float noteSpacing, float tail)
    {
        float duration = (noteSpacing * (frequencies.Length - 1)) + tail;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int note = 0; note < frequencies.Length; note++)
        {
            int start = Mathf.RoundToInt(note * noteSpacing * SampleRate);
            int length = Mathf.Min(sampleCount - start, Mathf.RoundToInt(tail * SampleRate));
            float frequency = frequencies[note];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.006f);
                float envelope = attack * Mathf.Exp(-t * 9f);

                // A quiet fifth above the fundamental gives the tone a little neon shimmer.
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t) + (0.25f * Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t));
                samples[start + i] += wave * envelope * 0.28f;
            }
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
