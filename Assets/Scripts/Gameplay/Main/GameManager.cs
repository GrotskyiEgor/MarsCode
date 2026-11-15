using TMPro;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MarsOutpost
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Данные")]
        [SerializeField] private TextAsset masterJson;
        [SerializeField] private string resourcesFile = "mars_outpost_master";
        [SerializeField] private bool autoStart = true;

        [Header("Стартовые значения (0–100)")]
        [SerializeField] private int startFood = 100;
        [SerializeField] private int startWater = 80;
        [SerializeField] private int startRadiation = 0;
        [SerializeField] private int maxValue = 100;

        // ЭНЕРГИЯ: всегда 3 ячейки. Каждое действие тратит 1 ячейку.
        // В начале КАЖДОГО сола энергия восстанавливается до 3.
        public const int MaxEnergyCells = 3;
        private const int EnergyCellCost = 1;
        public float EnergyPercent => (float)Energy / MaxEnergyCells * 100f;

        [Header("Ежедневный расход экипажа")]
        [SerializeField] private int dailyFoodUse = 10;
        [SerializeField] private int dailyWaterUse = 10;
        [SerializeField] public int requiredTasks = 5;
        [SerializeField] private int hullTaskId = 5;
        [SerializeField] private int hullReducedFlareDelta = 20;
        private readonly HashSet<int> completedTaskIds = new HashSet<int>();
        private bool shieldsActiveToday = false;
        public int TasksCompleted => completedTaskIds.Count;
        public bool IsTaskCompleted(int taskId) => completedTaskIds.Contains(taskId);

        [Header("Шкалы ресурсов")]
        [SerializeField] private SmoothSlider foodSlider;
        [SerializeField] private SmoothSlider waterSlider;
        [SerializeField] private SmoothSlider radiationSlider;
        [SerializeField] private SmoothSlider energySlider;

        [Header("Текстовые поля")]
        [SerializeField] private TMP_Text solNumberText;

        [Header("Озвучка уведов")]
        [SerializeField] private AudioClip solStartVoice;

        [Header("Конец игры")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [Tooltip("Имя сцены главного меню. Сцена должна быть добавлена в File > Build Settings.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private float returnToMenuDelay = 5f;
        private Coroutine returnToMenuRoutine;

        // Энергия / Солнечные панели
        [Header("Солнечные панели")]
        [SerializeField] private GameObject solarPanelOnVisual;  // Объект/картинка "Включено"
        [SerializeField] private GameObject solarPanelOffVisual; // Объект/картинка "Выключено"
        private bool solarPanelsUsedToday = false; // Панели уже открывали в этом соле (можно только 1 раз за сол)
        private bool solarPanelsOpen = false;      // Открыты ли панели прямо сейчас (для картинки)
        public bool SolarPanelsOpen => solarPanelsOpen;
        public bool CanOpenSolarPanels => Phase == GamePhase.Planning && !solarPanelsUsedToday;

        [Header("Отладка")]
        [Tooltip("Писать в Console подробный лог энергии и заданий по каждому солу.")]
        [SerializeField] private bool debugLog = true;

        public event Action<SolScenario> OnSolStarted;
        public event Action OnStateChanged;
        public event Action<GameResult> OnGameOver;

        public GamePhase Phase { get; private set; } = GamePhase.NotStarted;
        public int Food { get; private set; }
        public int Water { get; private set; }
        public int Radiation { get; private set; }
        public int Energy { get; private set; }

        [Header("Бортовой журнал")]
        public string bookText = "";
        private string currentNotificationText = "";
        [SerializeField] private TMP_Text bookTextTMP;
        [SerializeField] private GameObject bookPanel;
        public IReadOnlyList<TaskData> Tasks => data != null ? data.tasks : null;
        public List<AstronautCard> ActiveCrew {get; private set;} = new List<AstronautCard>();
        public int CurrentSol => CurrentScenario != null ? CurrentScenario.solNumber : 0;
        public int EnergyTotal => MaxEnergyCells;
        public bool IsFinalSol => data != null && solIndex == data.sols.Count - 1;
        public SolScenario CurrentScenario =>
            (data != null && solIndex >= 0 && solIndex < data.sols.Count) ? data.sols[solIndex] : null;
        public GameResult FinalResult { get; private set; }

        private MarsOutpostData data;
        private int solIndex = -1;

        [Header("Всплывающие уведомления (Пузырьки)")]
        [SerializeField] private GameObject resourcePopupPrefab;
        [SerializeField] private Transform greenhouseSpawnPoint;
        [SerializeField] private Transform waterPlantSpawnPoint;

        [Header("Иконки для пузырьков")]
        [SerializeField] private Sprite foodIcon;
        [SerializeField] private Sprite waterIcon;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoStart) StartMission();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartMission()
        {
            // Останавливаем всё, что могло остаться от прошлой партии (таймер возврата в меню, задержки).
            StopAllCoroutines();
            returnToMenuRoutine = null;
            if (fallbackGameOverCanvas != null) Destroy(fallbackGameOverCanvas);

            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            if (!LoadData())
            {
                Phase = GamePhase.Error;
                NotifyChanged();
                return;
            }

            completedTaskIds.Clear();
            FinalResult = null;

            Food = Mathf.Clamp(startFood, 0, maxValue);
            Water = Mathf.Clamp(startWater, 0, maxValue);
            Energy = MaxEnergyCells;
            Radiation = Mathf.Clamp(startRadiation, 0, maxValue);

            SelectRandomCrew(3);
            BeginSol(0);
        }

        // Кнопка «Restart» после поражения: просто начинает всё заново.
        public void RestartMission() => StartMission();

        // Сол: Reading Phase -> Doing Phase -> EndSol
        public void BeginSol(int index)
        {
            solIndex = index;

            // Каждый сол — ровно 3 ячейки энергии (раньше бралось availableEnergy из JSON:
            // если там 0 или поле отсутствует — энергии было 0).
            Energy = MaxEnergyCells;
            shieldsActiveToday = false;

            // Сбрасываем статус солнечных панелей на новый сол
            solarPanelsUsedToday = false;
            solarPanelsOpen = false;
            UpdateSolarPanelsUI();

            if (solNumberText != null) solNumberText.text = $"Sol {CurrentSol}";
            UpdateSliders();
            Log($"=== BEGIN Sol {CurrentSol} (index {solIndex}/{data.sols.Count - 1}, final={IsFinalSol}) | " +
                $"Energy {Energy}/{MaxEnergyCells} | Food {Food} | Water {Water} | Rad {Radiation} | Tasks {TasksCompleted}/{requiredTasks}");

            Delay(0.5f, () => StartReadingPhase());
        }

        public void StartReadingPhase()
        {
            if (Phase == GamePhase.GameOver) return;
            Phase = GamePhase.Reading;

            var (generatedBook, generatedNotification) = BookTextBuilder.CreateBookText(this);
            bookText = generatedBook;
            currentNotificationText = generatedNotification;

            if (bookTextTMP != null) bookTextTMP.text = bookText;
            OnSolStarted?.Invoke(CurrentScenario);

            toggleBook(true);
            NotifyChanged();
        }

        public void ContinueFromReading()
        {
            if (Phase != GamePhase.Reading) return;

            toggleBook(false);
            UpdateSliders();
            StartDoingPhase();
        }

        public void StartDoingPhase()
        {
            Phase = GamePhase.Planning;
            SolTimer.Instance?.StartSol();

            if (!string.IsNullOrEmpty(currentNotificationText))
            {
                Delay(0.5f, () => NotifyPlayer(currentNotificationText, solStartVoice));
            }

            NotifyChanged();
        }

        private void NotifyPlayer(string text, AudioClip voice = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (Phase == GamePhase.GameOver) return;
            currentNotificationText = text;
            NotificationManager.Instance?.ShowNotification(currentNotificationText, voice);
        }


        public void EndSol()
        {
            if (Phase != GamePhase.Planning) return;

            NotificationManager.Instance?.HideNotification();

            // 1. Базовый расход еды и воды экипажем
            Food = Mathf.Clamp(Food - dailyFoodUse, 0, maxValue);
            Water = Mathf.Clamp(Water - dailyWaterUse, 0, maxValue);

            // 2. Расчет радиации
            int radiationDelta = CurrentScenario != null ? CurrentScenario.baseRadiationDelta : 5;
            if (IsFinalSol && completedTaskIds.Contains(hullTaskId) && shieldsActiveToday)
            {
                radiationDelta = Mathf.Min(radiationDelta, hullReducedFlareDelta);
            }
            Radiation = Mathf.Clamp(Radiation + radiationDelta, 0, maxValue);

            UpdateSliders();
            Log($"--- END Sol {CurrentSol} | Food {Food} | Water {Water} | Rad {Radiation} (+{radiationDelta}) | " +
                $"Energy left {Energy} | Tasks {TasksCompleted}/{requiredTasks} [{string.Join(",", completedTaskIds)}]");

            // 3. Проверка победы или поражения (ДО пузырьков, чтобы они не появлялись поверх экрана результата)
            if (CheckGameEnd())
            {
                return;
            }

            // Спавним пузырьки в конце сола
            SpawnEndSolResourcePopups();

            NotifyPlayer("Sol ended.");
            BeginSol(solIndex + 1);
        }

        private void UpdateSliders(bool instant = false)
        {
            ApplySlider(foodSlider, Food, instant);
            ApplySlider(waterSlider, Water, instant);
            ApplySlider(energySlider, EnergyPercent, instant);
            ApplySlider(radiationSlider, Radiation, instant);
        }

        public void toggleBook(bool action)
        {
            if (bookPanel != null) bookPanel.SetActive(action);
        }

        private void SelectRandomCrew(int count)
        {
            ActiveCrew.Clear();
            if(AstronautPresets.DefaultAstronauts == null || AstronautPresets.DefaultAstronauts.Count == 0)
            {
                Debug.LogWarning("[GameManager] список AstronautPresets.DefaultAstronauts пуст");
                return;
            }

            List<AstronautCard> pool = new List<AstronautCard>(AstronautPresets.DefaultAstronauts);
            int targetCount = Mathf.Min(count, pool.Count);

            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                AstronautCard chosen = (pool[randomIndex]);
                ActiveCrew.Add(chosen);
                pool.RemoveAt(randomIndex);
            }
        }

        private bool CheckGameEnd()
        {
            var reasons = new List<string>();

            if (Food <= 0)
                reasons.Add("Food supplies ran out.");
            if (Water <= 0)
                reasons.Add("Water reserves depleted.");
            if (Radiation >= maxValue)
                reasons.Add("Radiation reached critical levels (100%).");

            if (reasons.Count > 0)
            {
                ShowGameOver(false, "Defeat! " + string.Join(" ", reasons));
                return true;
            }

            if (!IsFinalSol)
            {
                return false;
            }

            // Финальный сол пережит — проверяем количество выполненных научных заданий.
            Log($"FINAL CHECK: tasks {TasksCompleted} / required {requiredTasks}");
            if (TasksCompleted < requiredTasks)
            {
                ShowGameOver(false, $"Defeat! You survived the solar storm, but completed only {TasksCompleted} of {requiredTasks} required scientific tasks.");
                return true;
            }

            ShowGameOver(true, $"Victory! The outpost survived the solar storm and completed {TasksCompleted} scientific tasks.");
            return true;
        }

        private bool LoadData()
        {
            TextAsset asset = masterJson != null ? masterJson : Resources.Load<TextAsset>(resourcesFile);
            if (asset == null)
            {
                Debug.LogError("[GameManager] Missing " + resourcesFile + ".json in Assets/Resources.");
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<MarsOutpostData>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameManager] JSON parse error: " + e.Message);
                return false;
            }

            if (data == null || data.sols == null || data.sols.Count == 0)
            {
                Debug.LogError("[GameManager] JSON has no sols.");
                return false;
            }

            data.sols.Sort((a, b) => a.solNumber.CompareTo(b.solNumber));

            // Проверки, без которых игру нельзя пройти/протестировать как задумано.
            if (data.sols.Count != 7)
                Debug.LogWarning($"[GameManager] В JSON {data.sols.Count} солов, а не 7. Результат покажется после сола #{data.sols[data.sols.Count - 1].solNumber}.");

            int taskCount = data.tasks != null ? data.tasks.Count : 0;
            if (taskCount < requiredTasks)
                Debug.LogWarning($"[GameManager] В JSON всего {taskCount} заданий, а для победы нужно {requiredTasks}. Победа невозможна.");

            int maxActions = data.sols.Count * MaxEnergyCells;
            if (maxActions < requiredTasks)
                Debug.LogWarning($"[GameManager] За игру доступно максимум {maxActions} действий, а нужно {requiredTasks} заданий.");

            return true;
        }

        private void Delay(float seconds, Action action)
        {
            StartCoroutine(DelayRoutine(seconds, action));
        }

        private IEnumerator DelayRoutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        private void NotifyChanged() => OnStateChanged?.Invoke();

        private void ShowGameOver(bool won, string summaryText)
        {
            Phase = GamePhase.GameOver;
            FinalResult = new GameResult
            {
                won = won,
                solsSurvived = CurrentSol,
                tasksCompleted = TasksCompleted,
                summary = summaryText
            };

            NotificationManager.Instance?.HideNotification();
            toggleBook(false);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverPanel.transform.SetAsLastSibling(); // поверх остального UI на Canvas
            }
            if (gameOverText != null)
            {
                gameOverText.alignment = TextAlignmentOptions.Center;
            }

            // Диагностика: почему экран результата может быть не виден.
            bool panelVisible = gameOverPanel != null && gameOverPanel.activeInHierarchy;
            bool textVisible = gameOverText != null && gameOverText.gameObject.activeInHierarchy;
            if (gameOverPanel == null) Debug.LogWarning("[GameManager] Game Over Panel НЕ назначен в инспекторе.");
            else if (!panelVisible) Debug.LogWarning("[GameManager] Game Over Panel включён, но его родитель выключен — панель не видна.");
            if (gameOverText == null) Debug.LogWarning("[GameManager] Game Over Text НЕ назначен в инспекторе.");

            // Если своей панели/текста не видно — рисуем запасной экран результата поверх всего.
            if (!panelVisible || !textVisible)
            {
                gameOverText = CreateFallbackGameOverScreen();
            }

            Debug.Log($"[GameManager] GAME OVER ({(won ? "Victory" : "Defeat")}): {summaryText}");
            OnGameOver?.Invoke(FinalResult);
            NotifyChanged();

            if (returnToMenuRoutine != null) StopCoroutine(returnToMenuRoutine);
            returnToMenuRoutine = StartCoroutine(ReturnToMenuRoutine(won, summaryText));
        }

        private GameObject fallbackGameOverCanvas;

        // Запасной экран результата: отдельный Canvas поверх всего, тёмный фон, текст по центру.
        private TMP_Text CreateFallbackGameOverScreen()
        {
            if (fallbackGameOverCanvas != null) Destroy(fallbackGameOverCanvas);

            fallbackGameOverCanvas = new GameObject("GameOver_Fallback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = fallbackGameOverCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = fallbackGameOverCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(fallbackGameOverCanvas.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var textGo = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(fallbackGameOverCanvas.transform, false);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.5f); tRect.anchorMax = new Vector2(0.5f, 0.5f);
            tRect.pivot = new Vector2(0.5f, 0.5f);
            tRect.anchoredPosition = Vector2.zero;
            tRect.sizeDelta = new Vector2(1400, 700);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        // Показывает WIN/LOSE по центру с обратным отсчётом и через returnToMenuDelay секунд грузит главное меню.
        private IEnumerator ReturnToMenuRoutine(bool won, string summaryText)
        {
            float remaining = returnToMenuDelay;
            while (remaining > 0f)
            {
                if (gameOverText != null)
                {
                    gameOverText.text =
                        $"<size=200%><b>{(won ? "WIN" : "LOSE")}</b></size>\n\n" +
                        $"{summaryText}\n\n" +
                        $"<size=70%>Main menu in {Mathf.CeilToInt(remaining)}...</size>";
                }
                // Realtime — чтобы отсчёт работал, даже если Time.timeScale = 0.
                yield return new WaitForSecondsRealtime(1f);
                remaining -= 1f;
            }

            Time.timeScale = 1f;
            Log("Returning to main menu: " + mainMenuSceneName);
            if (string.IsNullOrEmpty(mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[GameManager] Сцена главного меню '{mainMenuSceneName}' не найдена. Добавьте её в File > Build Settings и проверьте имя в инспекторе GameManager.");
                yield break;
            }
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // Кнопка 1: Теплица (+15 Еды, -10 Воды, -1 ячейка энергии)
        public void ActivateGreenhouse()
        {
            if (Phase != GamePhase.Planning) return;
            if (!TrySpendEnergy("Not enough energy to run the Greenhouse.")) return;

            Food = Mathf.Clamp(Food + 15, 0, maxValue);
            Water = Mathf.Clamp(Water - 10, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Greenhouse cycle started: +15% Food, -10% Water.");
        }

        // Кнопка 2: Водоочиститель (+20 Воды, +5 Радиации, -1 ячейка энергии)
        // БЫЛО: код был скопирован из теплицы и давал +15 Еды / -10 Воды.
        public void ActivateWaterPlant()
        {
            if (Phase != GamePhase.Planning) return;
            if (!TrySpendEnergy("Not enough energy to run the Water Plant.")) return;

            Water = Mathf.Clamp(Water + 20, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 5, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Water Plant started: +20% Water, +5% Radiation.");
        }

        // Кнопка 3: Щиты жилого модуля (-15 Радиации, -1 ячейка энергии)
        public void ActivateHabitatShields()
        {
            if (Phase != GamePhase.Planning) return;
            if (shieldsActiveToday) { NotifyPlayer("Habitat Shields are already active today."); return; }
            if (!TrySpendEnergy("Not enough energy to power Habitat Shields.")) return;

            Radiation = Mathf.Clamp(Radiation - 15, 0, maxValue);
            shieldsActiveToday = true;
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Habitat Shields online: -15% Radiation.");
        }

        // Кнопка 4: Отправить ровер на задание
        public void ExecuteRoverTask(int taskId)
        {
            if (Phase != GamePhase.Planning) return;
            if (completedTaskIds.Contains(taskId)) { NotifyPlayer("This research task is already completed."); return; }
            if (!TrySpendEnergy("Not enough energy for a Rover expedition.")) return;

            Food = Mathf.Clamp(Food - 10, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 10, 0, maxValue);
            completedTaskIds.Add(taskId);
            UpdateSliders();
            NotifyChanged();
            Log($"Rover task #{taskId} done -> {TasksCompleted}/{requiredTasks}");
            NotifyPlayer($"Rover completed task #{taskId} ({TasksCompleted}/{requiredTasks} required).");
        }

        // Единая точка списания энергии: 1 ячейка из 3.
        private bool TrySpendEnergy(string notEnoughMessage)
        {
            if (Energy < EnergyCellCost)
            {
                NotifyPlayer(notEnoughMessage);
                return false;
            }
            Energy -= EnergyCellCost;
            Log($"Energy spent -> {Energy}/{MaxEnergyCells}");
            return true;
        }

        // --- УПРАВЛЕНИЕ СОЛНЕЧНЫМИ ПАНЕЛЯМИ ---

        public void OpenSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;

            // Открыть можно только ОДИН раз за сол. Закрытие НЕ сбрасывает этот запрет.
            if (solarPanelsUsedToday)
            {
                NotifyPlayer("Solar panels can only be deployed once per sol.");
                return;
            }

            solarPanelsUsedToday = true;
            solarPanelsOpen = true;

            UpdateSliders();
            UpdateSolarPanelsUI();
            NotifyChanged();
            Log("Solar panels opened (used for this sol)");
            // ВНИМАНИЕ: энергия здесь НЕ тратится и не добавляется (как и в исходном коде).
            NotifyPlayer("Solar panels deployed successfully.");
        }

        public void CloseSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;
            if (!solarPanelsOpen) return;

            solarPanelsOpen = false; // solarPanelsUsedToday остаётся true до следующего сола
            UpdateSolarPanelsUI();
            NotifyChanged();
            NotifyPlayer("Solar panels retracted. They can be deployed again next sol.");
        }

        private void UpdateSolarPanelsUI()
        {
            if (solarPanelOnVisual != null) solarPanelOnVisual.SetActive(solarPanelsOpen);
            if (solarPanelOffVisual != null) solarPanelOffVisual.SetActive(!solarPanelsOpen);
        }

        private static void ApplySlider(SmoothSlider slider, float value, bool instant)
        {
            if (slider == null) return;

            if (instant) slider.SetValueInstant(value);
            else slider.SetTargetValue(value);
        }

        // Пузырьки еды и воды в конце сола. Сбор тратит 1 ячейку энергии УЖЕ СЛЕДУЮЩЕГО сола
        // (энергия к этому моменту восстановлена до 3 в BeginSol).
        public void SpawnEndSolResourcePopups()
        {
            SpawnCollectiblePopup("+10", foodIcon, greenhouseSpawnPoint, () =>
            {
                if (Phase == GamePhase.GameOver) return;
                if (!TrySpendEnergy("Not enough energy to collect resources!")) return;

                Food = Mathf.Clamp(Food + 10, 0, maxValue);
                UpdateSliders();
                NotifyChanged();
                NotifyPlayer("Collected food (+10), spent energy.");
            });

            SpawnCollectiblePopup("+10", waterIcon, waterPlantSpawnPoint, () =>
            {
                if (Phase == GamePhase.GameOver) return;
                if (!TrySpendEnergy("Not enough energy to collect resources!")) return;

                Water = Mathf.Clamp(Water + 10, 0, maxValue);
                UpdateSliders();
                NotifyChanged();
                NotifyPlayer("Collected water (+10), spent energy.");
            });
        }

        private void SpawnCollectiblePopup(string message, Sprite icon, Transform spawnPoint, System.Action onCollectAction)
        {
            if (resourcePopupPrefab == null || spawnPoint == null) return;

            GameObject popupObj = Instantiate(resourcePopupPrefab, spawnPoint.position, Quaternion.identity);
            if (popupObj.TryGetComponent<ResourcePopup>(out var popup))
            {
                popup.Initialize(message, icon, onCollectAction);
            }
        }

        private void Log(string msg)
        {
            if (debugLog) Debug.Log("[GameManager] " + msg);
        }

#if UNITY_EDITOR
        // ПКМ по компоненту GameManager в инспекторе (в Play Mode) → быстрые тесты без прохождения вручную.
        [ContextMenu("TEST: Complete all required tasks")]
        private void DebugCompleteTasks()
        {
            for (int i = 1; i <= requiredTasks; i++) completedTaskIds.Add(i);
            Log($"DEBUG: tasks set to {TasksCompleted}/{requiredTasks}");
            NotifyChanged();
        }

        [ContextMenu("TEST: Skip to final sol")]
        private void DebugSkipToFinal()
        {
            if (data == null) return;
            StopAllCoroutines();
            Phase = GamePhase.NotStarted;
            BeginSol(data.sols.Count - 1);
        }

        [ContextMenu("TEST: End current sol now")]
        private void DebugEndSol()
        {
            if (Phase == GamePhase.Reading) ContinueFromReading();
            EndSol();
        }
#endif
    }
}