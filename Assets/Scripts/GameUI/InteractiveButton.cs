using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class InteractiveButton : MonoBehaviour
{
    private AudioSource audioSource;
    private Vector3 originalScale;
    private ITISKIRUHERE.AdvancedOutline outlineComponent;

    [Header("Настройки сцены")]
    [SerializeField] private string sceneToLoad = "RocketAssembly";

    [Header("Настройки увеличения при наведении")]
    [SerializeField] private float hoverScaleMultiplier = 1.15f;

    [Header("Настройки обводки (Тени)")]
    [SerializeField] private Color hoverOutlineColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField, Range(1f, 10f)] private float hoverOutlineWidth = 2.5f;

    [Header("Настройки музыки (Для 3D моделей)")]
    [SerializeField] private GameObject musicOnModel;
    [SerializeField] private GameObject musicOffModel;
    private static bool isMusicMuted = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        if (hoverScaleMultiplier < 1f)
        {
            hoverScaleMultiplier = 1.15f;
        }

        outlineComponent = GetComponent<ITISKIRUHERE.AdvancedOutline>();

        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = 0f;
        }

        UpdateMusicVisuals();
    }

    void OnMouseEnter()
    {
        transform.localScale = originalScale * hoverScaleMultiplier;

        if (outlineComponent != null)
        {
            outlineComponent.OutlineColor = hoverOutlineColor;
            outlineComponent.OutlineWidth = hoverOutlineWidth;
        }
    }

    void OnMouseExit()
    {
        transform.localScale = originalScale;

        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = 0f;
        }
    }

    void OnMouseDown()
    {
        transform.localScale = originalScale;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = 0f;
        }

        string buttonName = gameObject.name;

        if (buttonName.Contains("Play"))
        {
            Debug.Log("Нажата кнопка Play! Запуск затухания и загрузка сцены: " + sceneToLoad);
            LoadGameScene();
        }
        else if (buttonName.Contains("Quit"))
        {
            Debug.Log("Нажата кнопка Quit!");
            Invoke("QuitGame", 0.3f);
        }
        else if (buttonName.Contains("Gear"))
        {
            Debug.Log("Нажата кнопка Настроек (Gear)!");
        }
        else if (buttonName.Contains("Music") || buttonName.Contains("UnMusic"))
        {
            Debug.Log("Переключение музыки!");
            ToggleMusic();
        }

        if (audioSource != null && audioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(audioSource.clip, transform.position, audioSource.volume);
        }
    }

    void LoadGameScene()
    {
        string cleanSceneName = sceneToLoad;
        if (cleanSceneName.Contains("/"))
        {
            cleanSceneName = cleanSceneName.Substring(cleanSceneName.LastIndexOf('/') + 1);
        }

        SceneTransition.LoadScene(cleanSceneName);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;
        AudioListener.volume = isMusicMuted ? 0f : 1f;

        InteractiveButton[] allButtons = Object.FindObjectsByType<InteractiveButton>();
        foreach (var btn in allButtons)
        {
            btn.UpdateMusicVisuals();
        }
    }

    public void UpdateMusicVisuals()
    {
        if (musicOnModel != null) musicOnModel.SetActive(!isMusicMuted);
        if (musicOffModel != null) musicOffModel.SetActive(isMusicMuted);
    }
}