using System.Collections;
using UnityEngine;
using TMPro;
using MarsOutpost;


// SolTimer.Instance.StartSol();


public class SolTimer : MonoBehaviour
{
    public static SolTimer Instance { get; private set; }

    [SerializeField] private float solDurationInSeconds = 120f;
    [SerializeField] private bool startOnAwake = true;

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
    }

    // private void Start()
    // {
    //     if (startOnAwake)
    //     {
    //         StartSol();
    //     }
    // }

    public void StartSol()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        timerCoroutine = StartCoroutine(RunSolTimer());
    }

    private IEnumerator RunSolTimer()
    {
        Debug.Log("Sol Timer, Start sol");

        float timeRemaining = solDurationInSeconds;

        while (timeRemaining > 0f)
        {
            UpdateTimerDisplay(timeRemaining);
            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        UpdateTimerDisplay(0f);
        timerCoroutine = null;


        Debug.Log("Sol Timer, end sol");
        GameManager.Instance.EndSol();
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
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
