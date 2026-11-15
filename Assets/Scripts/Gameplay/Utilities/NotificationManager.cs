using System.Collections;
using UnityEngine;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoPanelText;

    [Header("Настройки текста")]
    [SerializeField] private float textSpeed = 0.03f;

    [Header("Настройки уведомления")]
    [Tooltip("Сколько секунд панель остаётся на экране ПОСЛЕ окончания озвучки.")]
    [SerializeField] private float notificationDuration = 5f;
    [SerializeField] private bool waitForVoiceEnd = true;

    [Header("Звуки интерфейса")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private AudioClip disappearSound;

    [Header("Громкость звуков")]
    [Range(0f, 1f)]
    [SerializeField] private float appearVolume = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float disappearVolume = 0.45f;

    [Header("Озвучка")]
    [SerializeField] private AudioSource voiceSource;

    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 1f;

    private Coroutine notificationCoroutine;
    private Coroutine typingCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine voiceDelayCoroutine;

    private string currentText;
    private bool isTyping;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void ShowNotification(string text)
    {
        ShowNotification(text, null);
    }

    public void ShowNotification(string text, AudioClip voice)
    {
        StopCurrentNotification();

        currentText = text;

        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (audioSource != null)
            audioSource.volume = 1f;

        PlaySound(appearSound, appearVolume);

        if (ambientSound != null && audioSource != null)
        {
            audioSource.clip = ambientSound;
            audioSource.loop = true;
            audioSource.volume = ambientVolume;
            audioSource.Play();
        }

        if (voice != null && voiceSource != null)
        {
            float delay = appearSound != null ? Mathf.Min(appearSound.length, 0.4f) : 0f;
            voiceDelayCoroutine = StartCoroutine(PlayVoiceAfterDelay(voice, delay));
        }

        typingCoroutine = StartCoroutine(TypeText());
        notificationCoroutine = StartCoroutine(NotificationTimer(voice));
    }

    private IEnumerator PlayVoiceAfterDelay(AudioClip voice, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        voiceSource.clip = voice;
        voiceSource.volume = voiceVolume;
        voiceSource.loop = false;
        voiceSource.Play();
        voiceDelayCoroutine = null;
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        infoPanelText.text = "";

        foreach (char letter in currentText)
        {
            infoPanelText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private IEnumerator NotificationTimer(AudioClip voice)
    {
        while (isTyping)
            yield return null;

        if (waitForVoiceEnd && voice != null)
        {
            while (voiceDelayCoroutine != null || (voiceSource != null && voiceSource.isPlaying))
                yield return null;
        }

        float fadePreTime = 1.5f;
        float waitBeforeFade = Mathf.Max(0f, notificationDuration - fadePreTime);

        if (waitBeforeFade > 0f)
            yield return new WaitForSeconds(waitBeforeFade);

        float disappearLength = disappearSound != null ? disappearSound.length : 0f;
        float totalFadeDuration = fadePreTime + disappearLength;

        if (ambientSound != null && audioSource != null && audioSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(FadeOutAmbient(totalFadeDuration));
        }

        if (fadePreTime > 0f)
            yield return new WaitForSeconds(fadePreTime);

        notificationCoroutine = null;
        CloseNotification(false);
    }

    private IEnumerator FadeOutAmbient(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
        audioSource.volume = 1f;
        fadeCoroutine = null;
    }

    public void HideNotification()
    {
        StopCurrentNotification();
        CloseNotification(true);
    }

    private void CloseNotification(bool stopAudioImmediately)
    {
        if (stopAudioImmediately && audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
            audioSource.volume = 1f;
        }

        if (voiceSource != null)
            voiceSource.Stop();

        PlaySound(disappearSound, disappearVolume);

        if (infoPanel != null)
            infoPanel.SetActive(false);

        isTyping = false;
    }

    private void StopCurrentNotification()
    {
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (voiceDelayCoroutine != null)
        {
            StopCoroutine(voiceDelayCoroutine);
            voiceDelayCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
            audioSource.volume = 1f;
        }

        if (voiceSource != null)
            voiceSource.Stop();

        isTyping = false;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        float scale = audioSource.volume > 0f ? volume / audioSource.volume : volume;
        audioSource.PlayOneShot(clip, scale);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}