using System.Collections;
using UnityEngine;

namespace MarsOutpost
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Настройки для старта ракеты")]
        [SerializeField] private float defaultDuration = 1.5f; // По умолчанию подлиннее для старта
        [SerializeField] private float defaultMagnitude = 1.2f; // Очень большая сила тряски

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Вызов со стандартными настройками
        public void Shake()
        {
            StartCoroutine(ShakeRoutine(defaultDuration, defaultMagnitude));
        }

        // Публичный метод для вызова с кастомными значениями
        public void Shake(float duration, float magnitude)
        {
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            Vector3 originalPosition = transform.localPosition;
            Quaternion originalRotation = transform.localRotation;
            
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // Вычисляем коэффициент затухания: тряска сильная в начале и плавно затухает к концу
                float currentPercent = 1f - (elapsed / duration);

                // Огромное случайное смещение, умноженное на затухание
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * currentPercent;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * currentPercent;
                float z = UnityEngine.Random.Range(-1f, 1f) * (magnitude * 0.5f) * currentPercent;

                transform.localPosition = originalPosition + new Vector3(x, y, z);

                // Жесткий дикий наклон (ротация) камеры для ощущения невесомости/ударов
                float tiltX = UnityEngine.Random.Range(-1f, 1f) * (magnitude * 3f) * currentPercent;
                float tiltY = UnityEngine.Random.Range(-1f, 1f) * (magnitude * 3f) * currentPercent;
                float tiltZ = UnityEngine.Random.Range(-1f, 1f) * (magnitude * 8f) * currentPercent;
                
                transform.localRotation = originalRotation * Quaternion.Euler(tiltX, tiltY, tiltZ);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Возвращаем камеру на точное исходное место и поворот
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }
    }
}