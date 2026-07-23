using System.Collections;
using UnityEngine;

public class WindAmbience : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] windClips;

    [Header("Volume")]
    [SerializeField] private Vector2 volumeRange = new(0.85f, 1f);

    [Header("Pitch")]
    [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

    [Header("Delay Between Clips")]
    [SerializeField] private Vector2 delayRange = new(2f, 6f);

    private int _lastIndex = -1;

    private void Start()
    {
        if (audioSource == null || windClips.Length == 0)
        {
            Debug.LogError("WindAmbience: configure o AudioSource e os clips.");
            enabled = false;
            return;
        }

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(delayRange.x, delayRange.y));

            int index;

            do
            {
                index = Random.Range(0, windClips.Length);
            }
            while (windClips.Length > 1 && index == _lastIndex);

            _lastIndex = index;

            audioSource.clip = windClips[index];
            audioSource.volume = Random.Range(volumeRange.x, volumeRange.y);
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

            audioSource.Play();

            yield return new WaitForSeconds(audioSource.clip.length / audioSource.pitch);
        }
    }
}