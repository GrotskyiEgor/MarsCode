using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [Header("UI элементы")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 3.5f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeCanvasGroup != null && fadeCanvasGroup.transform.root == transform)
            {
                DontDestroyOnLoad(fadeCanvasGroup.transform.root.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    public static void LoadScene(string sceneName)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.TransitionCoroutine(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }
}