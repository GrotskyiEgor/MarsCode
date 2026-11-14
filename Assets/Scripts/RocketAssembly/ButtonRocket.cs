using UnityEngine;

public class ButtonRocket : MonoBehaviour
{
    private Vector3 originalScale;

    [Header("Настройки")]
    [SerializeField] private float hoverScaleMultiplier = 1.15f;
    [SerializeField] private string sceneToLoad = "RocketVideo";

    void Start()
    {
        originalScale = transform.localScale;
    }

    void OnMouseEnter()
    {
        transform.localScale = originalScale * hoverScaleMultiplier;
    }

    void OnMouseExit()
    {
        transform.localScale = originalScale;
    }

    void OnMouseDown()
    {
     
        transform.localScale = originalScale;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Нажата кнопка! Запускаем затухание и переход на сцену: " + sceneToLoad);
            SceneTransition.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("Кнопка нажата, но сцена не задана в инспекторе.");
        }
    }
}