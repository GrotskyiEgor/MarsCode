using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarsOutpost
{
    public class ResourcePopup : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text popupText;
        [SerializeField] private Image iconImage;
        [SerializeField] private float yOffset = 1.5f;
        private float lifetime = 89f;

        private Func<bool> onTryCollect;
        private Camera mainCamera;

        public ResourcePopup Initialize(string message, Sprite icon, Func<bool> tryCollectCallback)
        {
            onTryCollect = tryCollectCallback;
            mainCamera = Camera.main;

            transform.position += Vector3.up * yOffset;

            if (popupText != null) popupText.text = message;
            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = icon;
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            return this;
            // Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.Instance != null && GameManager.Instance.Phase != GamePhase.Planning)
                return;

            // Удаляем пузырёк только если сбор реально прошёл (хватило энергии)
            if (onTryCollect != null && onTryCollect.Invoke())
            {
                Destroy(gameObject);
            }
        }
    }
}