using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    [SerializeField] private float targetScaleMultiplier = 1.5f; // Size increase on hover
    [SerializeField] private float transitionSpeed = 10f;        // Animation speed

    private Vector3 currentScaleTarget;

    private void Awake()
    {
        originalScale = transform.localScale;
        currentScaleTarget = originalScale;
    }

    private void Update()
    {
        // Smoothly interpolate towards the target scale
        transform.localScale = Vector3.Lerp(transform.localScale, currentScaleTarget, Time.unscaledDeltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentScaleTarget = originalScale * targetScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentScaleTarget = originalScale;
    }
}
