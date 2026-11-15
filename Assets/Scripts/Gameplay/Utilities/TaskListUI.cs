using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsOutpost
{
    public class TaskListUI : MonoBehaviour
    {
        [Header("Шапка и сворачивание")]
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleArrowText; // опционально: "▼" / "▲"
        [SerializeField] private GameObject listContainer;
        [SerializeField] private bool startExpanded = false;

        [Header("Список заданий")]
        [SerializeField] private Transform itemsParent;
        [SerializeField] private GameObject taskItemPrefab;

        private readonly List<(TaskData data, Button button, TMP_Text label)> spawnedItems = new();
        private bool isBuilt = false;

        private void Awake()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleList);

            SetExpanded(startExpanded);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += RefreshUI;
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= RefreshUI;
        }

        public void ToggleList()
        {
            if (listContainer == null) return;
            SetExpanded(!listContainer.activeSelf);
        }

        public void SetExpanded(bool expanded)
        {
            if (listContainer != null)
                listContainer.SetActive(expanded);

            if (toggleArrowText != null)
                toggleArrowText.text = expanded ? "▲" : "▼";
        }

        private void BuildListIfNeeded()
        {
            var gm = GameManager.Instance;
            if (isBuilt || gm == null || gm.Tasks == null || taskItemPrefab == null || itemsParent == null)
                return;

            foreach (Transform child in itemsParent)
                Destroy(child.gameObject);

            spawnedItems.Clear();

            foreach (var task in gm.Tasks)
            {
                GameObject row = Instantiate(taskItemPrefab, itemsParent);
                row.SetActive(true);

                Button btn = row.GetComponent<Button>();
                TMP_Text label = row.GetComponentInChildren<TMP_Text>();

                int taskId = task.id;
                if (btn != null)
                {
                    btn.onClick.AddListener(() => GameManager.Instance?.ExecuteRoverTask(taskId));
                }

                spawnedItems.Add((task, btn, label));
            }

            isBuilt = true;
        }

        public void RefreshUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            BuildListIfNeeded();

            // 1. Обновляем счетчик (например: "Tasks: 2/4")
            if (counterText != null)
            {
                counterText.text = $"Tasks: {gm.TasksCompleted}/{gm.RequiredTasks}";
            }

            // 2. Обновляем статус каждой строки в списке
            foreach (var (task, btn, label) in spawnedItems)
            {
                bool done = gm.IsTaskCompleted(task.id);

                if (btn != null)
                    btn.interactable = !done;

                if (label != null)
                {
                    label.text = done
                        ? $"<color=#68D391><s>[✓] {task.id}. {task.title}</s></color>"
                        : $"[  ] {task.id}. {task.title}";
                }
            }
        }
    }
}