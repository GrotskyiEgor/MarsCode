using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SmoothSlider : MonoBehaviour
{
    [Header("Настройки плавности")]
    [SerializeField] private float changeSpeed = 35f;

    private Slider slider;
    private float targetValue;

    private Slider SliderComponent
    {
        get
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = 100f;
                slider.wholeNumbers = false;
                slider.interactable = false;
            }
            return slider;
        }
    }

    private void Awake()
    {
        targetValue = SliderComponent.value;
    }

    private void Update()
    {
        if (!Mathf.Approximately(SliderComponent.value, targetValue))
        {
            SliderComponent.value = Mathf.MoveTowards(SliderComponent.value, targetValue, changeSpeed * Time.deltaTime);
        }
    }

    public void SetTargetValue(float newValue)
    {
        targetValue = Mathf.Clamp(newValue, SliderComponent.minValue, SliderComponent.maxValue);
    }

    public void DecreaseValue(float amount)
    {
        SetTargetValue(targetValue - amount);
    }

    public void SetValueInstant(float newValue)
    {
        targetValue = Mathf.Clamp(newValue, SliderComponent.minValue, SliderComponent.maxValue);
        SliderComponent.value = targetValue;
    }
}