using UnityEngine;

public class ButtonRocket : MonoBehaviour
{
    private Vector3 originalScale;

    [Header("Настройки")]
    [SerializeField] private float hoverScaleMultiplier = 1.15f;
    [SerializeField] private string sceneToLoad = "RocketVideo";

    private SelectManager selectionManager;

    void Start()
    {
        selectionManager = FindAnyObjectByType<SelectManager>();
        
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
            if (selectionManager.IsMaxAllowedCardSelect())
            {
                Debug.Log("Нажата кнопка! Запускаем затухание и переход на сцену: " + sceneToLoad);
                SceneTransition.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.Log("Не выбрано нужное количество астронавтов.");
            }
        }
        else
        {
            Debug.Log("Кнопка нажата, но сцена не задана в инспекторе.");
        }
    }
}