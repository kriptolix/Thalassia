using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public enum DayPhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    [Header("References")]
    [SerializeField] private Light sun;

    [Header("Cycle")]
    [Tooltip("20 minutos = 1200 segundos")]
    [SerializeField] private float cycleDuration = 1200f;

    [Range(0f, 24f)]
    [SerializeField] private float currentHour = 6f;

    public float CurrentHour => currentHour;
    public int DayCount { get; private set; } = 1;
    public DayPhase CurrentPhase { get; private set; }

    private float secondsPerGameHour;

    private void Start()
    {
        secondsPerGameHour = cycleDuration / 24f;
        UpdateSun();
    }

    private void Update()
    {
        currentHour += Time.deltaTime / secondsPerGameHour;

        if (currentHour >= 24f)
        {
            currentHour -= 24f;
            DayCount++;
        }

        UpdatePhase();
        UpdateSun();
    }

    private void UpdateSun()
    {
        // 6h = nascer do sol
        // 12h = sol no ponto mais alto
        // 18h = pôr do sol
        float angle = (currentHour / 24f) * 360f - 90f;

        sun.transform.rotation = Quaternion.Euler(angle, 170f, 0f);

        // Intensidade simples
        if (currentHour >= 6f && currentHour <= 18f)
        {
            float t = Mathf.Sin((currentHour - 6f) / 12f * Mathf.PI);
            sun.intensity = Mathf.Lerp(0.2f, 8f, t);
        }
        else
        {
            sun.intensity = 0.05f;
        }
    }

    private void UpdatePhase()
    {
        if (currentHour >= 5f && currentHour < 7f)
            CurrentPhase = DayPhase.Dawn;
        else if (currentHour >= 7f && currentHour < 17f)
            CurrentPhase = DayPhase.Day;
        else if (currentHour >= 17f && currentHour < 19f)
            CurrentPhase = DayPhase.Dusk;
        else
            CurrentPhase = DayPhase.Night;
    }
}