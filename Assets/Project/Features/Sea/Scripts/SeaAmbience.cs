using UnityEngine;

public class SeaAmbience : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;
    [SerializeField] private AudioClip seaClip;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float crossfadeDuration = 3f;

    private double nextStartTime;
    private bool usingA = true;

    private void Start()
    {
        if (seaClip == null || sourceA == null || sourceB == null)
        {
            Debug.LogError("SeaAmbience: Configure o AudioClip e os dois AudioSources.");
            enabled = false;
            return;
        }

        ConfigureSource(sourceA);
        ConfigureSource(sourceB);

        nextStartTime = AudioSettings.dspTime + 0.1f;

        sourceA.clip = seaClip;
        sourceA.volume = volume;
        sourceA.PlayScheduled(nextStartTime);

        nextStartTime += seaClip.length - crossfadeDuration;

        ScheduleNext(sourceB);
    }

    private void Update()
    {
        // Quando chega perto do próximo ciclo, agenda o outro source
        if (AudioSettings.dspTime >= nextStartTime - crossfadeDuration)
        {
            if (usingA)
            {
                ScheduleNext(sourceB);
            }
            else
            {
                ScheduleNext(sourceA);
            }

            nextStartTime += seaClip.length - crossfadeDuration;
            usingA = !usingA;
        }
    }

    private void ScheduleNext(AudioSource source)
    {
        source.clip = seaClip;
        source.volume = volume;

        double startTime = nextStartTime;

        source.PlayScheduled(startTime);

        // Fade in/out usando o mixer seria o ideal.
        // Aqui usamos os volumes dos AudioSources diretamente.
        StartCoroutine(FadeVolume(source, 0f, volume, crossfadeDuration));
    }

    private System.Collections.IEnumerator FadeVolume(
        AudioSource source,
        float startVolume,
        float endVolume,
        float duration)
    {
        float timer = 0f;

        source.volume = startVolume;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, endVolume, timer / duration);
            yield return null;
        }

        source.volume = endVolume;
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }
}