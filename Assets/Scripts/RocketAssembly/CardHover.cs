using UnityEngine;

public class CardHover : MonoBehaviour
{
    #region -==- Move -==-

    [Header("Settings")]
    [SerializeField] private float hoverDistance = 0.2f; // На сколько единиц сдвигать вправо
    [SerializeField] private float moveSpeed = 2f;     // Скорость анимации

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    #endregion

    private ITISKIRUHERE.AdvancedOutline outlineComponent;

    [Header("Ouline Settings")]
    [SerializeField] private Color hoverOutlineColor = new Color(0f, 1f, 0f, 1f); // Зеленый цвет с нормальной непрозрачностью (Alpha = 1f)
    [SerializeField, Range(1f, 10f)] private float hoverOutlineWidth = 7f; // Толщина обводки

    private void Start()
    {
        // Запоминаем исходную локальную позицию
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;

        outlineComponent = GetComponent<ITISKIRUHERE.AdvancedOutline>();

        if (outlineComponent != null)
        {
            // Задаем базовые параметры и выключаем компонент обводки при старте
            outlineComponent.OutlineColor = hoverOutlineColor;
            outlineComponent.OutlineWidth = hoverOutlineWidth;
            outlineComponent.enabled = false;
        }
    }

    private void Update()
    {
        // Плавное перемещение к целевой позиции
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    // Срабатывает, когда мышка наводится на 3D-объект (нужен Collider на объекте!)
    private void OnMouseEnter()
    {
        // Сдвигаем по оси X вправо
        targetPosition = originalPosition + new Vector3(hoverDistance, 0f, 0f);

        if (outlineComponent != null)
        {
            // Включаем компонент обводки
            outlineComponent.enabled = true;
        }
    }

    // Срабатывает, когда мышка уходит с объекта
    private void OnMouseExit()
    {
        targetPosition = originalPosition;

        if (outlineComponent != null)
        {
            // Выключаем компонент обводки
            outlineComponent.enabled = false;
        }
    }
}