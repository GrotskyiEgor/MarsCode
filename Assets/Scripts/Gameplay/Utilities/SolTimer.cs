using System.Collections;
using UnityEngine;
using TMPro;
using MarsOutpost;

public class SolTimer : MonoBehaviour
{
    public static SolTimer Instance { get; private set; }

    [SerializeField] private float solDurationInSeconds = 120f;

    [Header("День и ночь")]
    [Tooltip("Если пусто, Directional Light найдётся автоматически.")]
    [SerializeField] private Light sun;
    [SerializeField] private float startAngle = 0f;      // 0 = рассвет, 90 = полдень, 180 = закат, 270 = полночь
    [SerializeField] private float sunYaw = 30f;         // поворот солнца по горизонту
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.05f;
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private Color duskColor = new Color(1f, 0.5f, 0.25f);
    [SerializeField] private Color nightColor = new Color(0.1f, 0.15f, 0.35f);
    [SerializeField] private Color dayAmbient = new Color(0.55f, 0.45f, 0.4f);
    [SerializeField] private Color nightAmbient = new Color(0.03f, 0.04f, 0.08f);

    private TMP_Text timerText;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timerText = GetComponent<TMP_Text>();
        FindSun();
        UpdateTimerDisplay(solDurationInSeconds);
        UpdateDayNight(0f);
    }

    private void FindSun()
    {
        if (sun != null) return;

        if (RenderSettings.sun != null)
        {
            sun = RenderSettings.sun;
            return;
        }

        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                sun = l;
                return;
            }
        }

        Debug.LogWarning("[SolTimer] Directional Light не найден на сцене.");
    }

    public void StartSol()
    {
        StopSol();
        timerCoroutine = StartCoroutine(RunSolTimer());
    }

    public void StopSol()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator RunSolTimer()
    {
        float timeRemaining = solDurationInSeconds;

        while (timeRemaining > 0f)
        {
            UpdateTimerDisplay(timeRemaining);
            UpdateDayNight(1f - timeRemaining / solDurationInSeconds);
            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        UpdateTimerDisplay(0f);
        timerCoroutine = null;

        GameManager.Instance?.EndSol();
    }

    // t: 0..1, прогресс сола
    private void UpdateDayNight(float t)
    {
        if (sun == null) return;

        float angle = startAngle + t * 360f;
        sun.transform.rotation = Quaternion.Euler(angle, sunYaw, 0f);

        // 1 в полдень, 0 на горизонте, -1 в полночь
        float elevation = Mathf.Sin(angle * Mathf.Deg2Rad);
        float daylight = Mathf.Clamp01(elevation);

        sun.intensity = Mathf.Lerp(nightIntensity, maxIntensity, daylight);

        // у горизонта цвет закатный, выше превращается в дневной
        Color col = Color.Lerp(duskColor, dayColor, Mathf.Clamp01(elevation * 2f));
        float nightBlend = Mathf.Clamp01(-elevation * 3f + 0.0f);
        sun.color = elevation >= 0f ? col : Color.Lerp(duskColor, nightColor, nightBlend);

        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);
    }

    private void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}