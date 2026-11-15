using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResourcePopup : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Image iconImage;
    private float lifetime = 20f; // Время жизни, пока игрок может забрать

    private System.Action onCollected;

    private Vector3 targetPos;
    private Camera mainCamera;

    public void Initialize(string message, Sprite icon, System.Action collectCallback)
    {
        onCollected = collectCallback;

        mainCamera = Camera.main;
        targetPos = transform.position + Vector3.up * 4f;
        transform.position = targetPos;

        if (popupText != null) popupText.text = message;
        if (iconImage != null)
        {
            if (icon != null) { iconImage.gameObject.SetActive(true); iconImage.sprite = icon; }
            else { iconImage.gameObject.SetActive(false); }
        }

        // Автоматически удаляем пузырек, если игрок его не собрал за отведенное время
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Чтобы пузырек всегда был повернут лицом к камере
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    // Этот метод автоматически вызывается Unity при клике мыши по UI-объекту (пузырьку)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[ResourcePopup] Кликнули по пузырьку!");
        onCollected?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log("Пузырек был уничтожен! Прошло секунд: " + Time.time);
    }
}