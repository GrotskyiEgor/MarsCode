using TMPro;
using UnityEngine;

namespace MarsOutpost
{
    public class TaskListUI : MonoBehaviour
    {
        [Header("Счётчик выполненных заданий")]
        [SerializeField] private TMP_Text counterText;

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= RefreshUI;
                GameManager.Instance.OnStateChanged += RefreshUI;
                RefreshUI();
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= RefreshUI;
                GameManager.Instance.OnStateChanged += RefreshUI;
                RefreshUI();
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= RefreshUI;
        }

        public void RefreshUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || counterText == null) return;

            counterText.text = $"{gm.TasksCompleted}/{gm.requiredTasks}";
        }
    }
}