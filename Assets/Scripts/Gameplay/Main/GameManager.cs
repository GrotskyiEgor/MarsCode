using TMPro;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace MarsOutpost
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Данные")]
        [SerializeField] private TextAsset masterJson;
        [SerializeField] private string resourcesFile = "mars_outpost_master";
        [SerializeField] private bool autoStart = true;

        [SerializeField] private TMP_FontAsset gameOverFont;

        [Header("Стартовые значения (0–100)")]
        [SerializeField] private int startFood = 100;
        [SerializeField] private int startWater = 90;
        [SerializeField] private int startRadiation = 0;
        [SerializeField] private int maxValue = 100;

        public const int MaxEnergyCells = 3;
        private const int EnergyCellCost = 1;
        public float EnergyPercent => (float)Energy / MaxEnergyCells * 100f;

        [Header("Ежедневный расход экипажа")]
        private const int baseDailyFoodUse = 22;
        private const int baseDailyWaterUse = 22;
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

        [Header("Текстовые поля и кнопки")]
        [SerializeField] private TMP_Text solNumberText;
        [SerializeField] private Button skipSolButton;

        [Header("1. Озвучка: Утренние сводки (7 файлов: Sol 1..7)")]
        [SerializeField] private AudioClip[] solStartVoices = new AudioClip[7];

        [Header("2. Озвучка: Выполнение заданий (6 файлов: Task 1..6)")]
        [SerializeField] private AudioClip[] taskCompleteVoices = new AudioClip[6];

        [Header("3. Озвучка: Обучение и тревоги (6 файлов)")]
        [SerializeField] private AudioClip radarAlertTutorialVoice;
        [SerializeField] private AudioClip panelsDamagedVoice;
        [SerializeField] private AudioClip closedPanelsVoice;
        [SerializeField] private AudioClip firstFoodVoice;
        [SerializeField] private AudioClip firstWaterVoice;
        [SerializeField] private AudioClip decontaminationTutorialVoice;

        [Header("4. Озвучка: Системные и финал (3 файла)")]
        [SerializeField] private AudioClip notEnoughEnergyVoice;
        [SerializeField] private AudioClip victoryVoice;
        [SerializeField] private AudioClip defeatVoice;

        [Header("5. Звуковые эффекты (SFX)")]
        [SerializeField] private AudioSource sfxSource;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float newSolVolume = 0.4f;
        [SerializeField] private AudioClip resourceCollectSfx; // Сбор еды/воды (первый раз)
        [SerializeField] private AudioClip taskMagicSfx;       // Магический звук выполнения задания
        [SerializeField] private AudioClip radiationRepairSfx; // Починка базы от радиации
        [SerializeField] private AudioClip warningSfx;         // Звук предупреждения
        [SerializeField] private AudioClip solarSwitchSfx;     // Переключатель солнечных батарей
        [SerializeField] private AudioClip newSolSfx;          // Звук нового дня
        [SerializeField] private AudioClip buttonClickSfx;     // Звук клика по кнопке

        private AudioClip currentNotificationVoice;

        private int lastSliderFood = -1;
        private int lastSliderWater = -1;
        private int lastSliderRad = -1;
        private int lastSliderEnergy = -1;

        [Header("Конец игры")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [Tooltip("Имя сцены главного меню. Сцена должна быть добавлена в File > Build Settings.")]
        [SerializeField] private string mainMenuSceneName = "FinalScene";
        [SerializeField] private float returnToMenuDelay = 5f;
        private Coroutine returnToMenuRoutine;

        // Энергия / Солнечные панели
        [Header("Солнечные панели")]
        [SerializeField] private GameObject solarPanelOnVisual;
        [SerializeField] private GameObject solarPanelOffVisual;
        private bool solarPanelsUsedToday = false;
        private bool solarPanelsOpen = false;
        private bool solarPanelsDamaged = false;
        private bool panelsJustDamagedToday = false;
        private bool lowPowerFromClosedPanels = false;
        public bool SolarPanelsOpen => solarPanelsOpen;
        public bool SolarPanelsDamaged => solarPanelsDamaged;
        public bool CanOpenSolarPanels => Phase == GamePhase.Planning && !solarPanelsUsedToday;

        // Радар опасности на следующий день
        [Header("Радар опасности (следующий сол)")]
        [SerializeField] private GameObject radarAlertOnVisual;
        [SerializeField] private GameObject radarAlertOffVisual;
        [SerializeField] private int hazardRadiationThreshold = 20;
        public bool IsDangerTomorrow { get; private set; } = false;
        public SolScenario NextScenario =>
            (data != null && solIndex >= 0 && solIndex + 1 < data.sols.Count) ? data.sols[solIndex + 1] : null;

        // Флаги одноразовых обучающих подсказок
        private bool hasShownFoodTutorial = false;
        private bool hasShownWaterTutorial = false;
        private bool hasShownDecontaminationTutorial = false;
        private bool hasShownFirstAlertTutorial = false;

        [Header("Отладка")]
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
        public List<AstronautCard> ActiveCrew { get; private set; } = new List<AstronautCard>();
        public int CurrentSol => CurrentScenario != null ? CurrentScenario.solNumber : 0;
        public int EnergyTotal => CurrentScenario != null ? CurrentScenario.availableEnergy : MaxEnergyCells;
        public bool IsFinalSol => data != null && solIndex == data.sols.Count - 1;
        public SolScenario CurrentScenario =>
            (data != null && solIndex >= 0 && solIndex < data.sols.Count) ? data.sols[solIndex] : null;
        public GameResult FinalResult { get; private set; }

        private MarsOutpostData data;
        private int solIndex = -1;

        [Header("Всплывающие уведомления (Ресурсы)")]
        [SerializeField] private GameObject resourcePopupPrefab;
        [SerializeField] private Transform greenhouseSpawnPoint;
        [SerializeField] private Transform waterPlantSpawnPoint;
        [SerializeField] private Sprite foodIcon;
        [SerializeField] private Sprite waterIcon;

        [Header("Пузырьки заданий на карте")]
        [SerializeField] private int dailyTasksToSpawn = 2;
        [SerializeField] private Sprite taskIcon;
        [SerializeField] private Transform task1SpawnPoint;
        [SerializeField] private Transform task2SpawnPoint;
        [SerializeField] private Transform task3SpawnPoint;
        [SerializeField] private Transform task4SpawnPoint;
        [SerializeField] private Transform task5SpawnPoint;
        [SerializeField] private Transform task6SpawnPoint;

        private GameObject foodPopupObj;
        private GameObject waterPopupObj;
        private readonly List<GameObject> activeTaskPopups = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (skipSolButton != null)
            {
                skipSolButton.onClick.RemoveListener(SkipSol);
                skipSolButton.onClick.AddListener(SkipSol);
            }
        }

        private void Start()
        {
            if (autoStart) StartMission();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlaySfx(AudioClip clip, float volumeOverride = -1f)
        {
            if (clip == null || sfxSource == null) return;
            float vol = volumeOverride >= 0f ? volumeOverride : sfxVolume;
            sfxSource.PlayOneShot(clip, vol);
        }

        public void PlayButtonClick() => PlaySfx(buttonClickSfx);

        public void StartMission()
        {
            StopAllCoroutines();
            returnToMenuRoutine = null;
            if (fallbackGameOverCanvas != null) Destroy(fallbackGameOverCanvas);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            ClearAllPopups();

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

            lastSliderFood = Food;
            lastSliderWater = Water;
            lastSliderRad = Radiation;
            lastSliderEnergy = Energy;

            SelectRandomCrew(3);
            solarPanelsOpen = false;
            solarPanelsDamaged = false;
            panelsJustDamagedToday = false;
            lowPowerFromClosedPanels = false;

            hasShownFoodTutorial = false;
            hasShownWaterTutorial = false;
            hasShownDecontaminationTutorial = false;
            hasShownFirstAlertTutorial = false;

            BeginSol(0);
        }

        public void RestartMission()
        {
            StartMission();
        }

        private bool IsHazardScenario(SolScenario scenario)
        {
            if (scenario == null) return false;
            return scenario.availableEnergy < MaxEnergyCells || scenario.baseRadiationDelta >= hazardRadiationThreshold;
        }

        public void BeginSol(int index)
        {
            solIndex = index;
            panelsJustDamagedToday = false;

            ClearAllPopups();

            // Звук нового дня
            PlaySfx(newSolSfx, newSolVolume);

            if (index > 0 && solarPanelsOpen && IsHazardScenario(CurrentScenario))
            {
                if (!solarPanelsDamaged)
                {
                    solarPanelsDamaged = true;
                    panelsJustDamagedToday = true;
                }
            }

            if (index == 0)
            {
                Energy = Mathf.Clamp(EnergyTotal, 0, MaxEnergyCells);
                lowPowerFromClosedPanels = false;
            }
            else if (solarPanelsOpen)
            {
                int penalty = solarPanelsDamaged ? 1 : 0;
                Energy = Mathf.Clamp(EnergyTotal - penalty, 1, MaxEnergyCells);
                lowPowerFromClosedPanels = false;
            }
            else
            {
                Energy = 1;
                lowPowerFromClosedPanels = true;
            }

            shieldsActiveToday = false;

            solarPanelsUsedToday = false;
            solarPanelsOpen = false;
            UpdateSolarPanelsUI();

            IsDangerTomorrow = IsHazardScenario(NextScenario);
            UpdateRadarAlertUI();

            if (solNumberText != null) solNumberText.text = $"Sol {CurrentSol}";

            UpdateSliders(index == 0);

            Log($"=== BEGIN Sol {CurrentSol} (index {solIndex}/{data.sols.Count - 1}, final={IsFinalSol}) | " +
                $"Energy {Energy}/{MaxEnergyCells} (damaged={solarPanelsDamaged}) | DangerTomorrow={IsDangerTomorrow}");

            if (index > 0)
            {
                SpawnEndSolResourcePopups();
            }
            SpawnDailyTaskPopups();

            Delay(0.5f, () => StartReadingPhase());
        }

        public void StartReadingPhase()
        {
            if (Phase == GamePhase.GameOver) return;
            Phase = GamePhase.Reading;

            var (generatedBook, generatedNotification) = BookTextBuilder.CreateBookText(this);
            bookText = generatedBook;

            currentNotificationVoice = (solStartVoices != null && solIndex >= 0 && solIndex < solStartVoices.Length)
                ? solStartVoices[solIndex]
                : null;

            if (panelsJustDamagedToday)
            {
                currentNotificationText = "WARNING: Solar panels were left deployed during hazardous weather and suffered permanent damage! Energy output is reduced by 1 unit for the rest of the mission.";
                if (panelsDamagedVoice != null) currentNotificationVoice = panelsDamagedVoice;
                PlaySfx(warningSfx);
            }
            else if (lowPowerFromClosedPanels)
            {
                currentNotificationText = "Solar panels were turned off, so only 1 unit of energy was generated today. Deploy the solar panels during the sol to restore power tomorrow!";
                if (closedPanelsVoice != null) currentNotificationVoice = closedPanelsVoice;
                PlaySfx(warningSfx);
            }
            else
            {
                currentNotificationText = generatedNotification;
            }

            if (IsDangerTomorrow)
            {
                PlaySfx(warningSfx);
                if (!hasShownFirstAlertTutorial)
                {
                    hasShownFirstAlertTutorial = true;
                    currentNotificationText += " RADAR ALERT TUTORIAL: The warning indicator is glowing because hazardous weather is forecast for tomorrow! Make sure to retract your solar panels before ending this sol, or the storm will permanently damage them.";
                    if (radarAlertTutorialVoice != null) currentNotificationVoice = radarAlertTutorialVoice;
                }
            }

            if (bookTextTMP != null) bookTextTMP.text = bookText;
            OnSolStarted?.Invoke(CurrentScenario);

            toggleBook(true);
            NotifyChanged();
        }

        private void UpdateRadarAlertUI()
        {
            if (radarAlertOnVisual != null) radarAlertOnVisual.SetActive(IsDangerTomorrow);
            if (radarAlertOffVisual != null) radarAlertOffVisual.SetActive(!IsDangerTomorrow);
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
                Delay(0.5f, () => NotifyPlayer(currentNotificationText, currentNotificationVoice));
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

        // Кнопка пропуска сола (работает как в фазе планирования, так и при открытом журнале)
        public void SkipSol()
        {
            if (Phase == GamePhase.GameOver) return;

            if (Phase == GamePhase.Reading)
            {
                ContinueFromReading();
            }

            if (Phase == GamePhase.Planning)
            {
                EndSol();
            }
        }

        public void EndSol()
        {
            SolTimer.Instance?.StopSol();

            if (Phase != GamePhase.Planning) return;

            NotificationManager.Instance?.HideNotification();

            int actualFoodUse = completedTaskIds.Contains(4) ? baseDailyFoodUse - 5 : baseDailyFoodUse;
            Food = Mathf.Clamp(Food - actualFoodUse, 0, maxValue);
            Water = Mathf.Clamp(Water - baseDailyWaterUse, 0, maxValue);

            int radiationDelta = CurrentScenario != null ? CurrentScenario.baseRadiationDelta : 6;
            if (completedTaskIds.Contains(3))
            {
                radiationDelta = Mathf.Max(1, radiationDelta - 3);
            }
            if (IsFinalSol && completedTaskIds.Contains(hullTaskId))
            {
                int cappedDelta = shieldsActiveToday ? hullReducedFlareDelta : 25;
                radiationDelta = Mathf.Min(radiationDelta, cappedDelta);
            }
            Radiation = Mathf.Clamp(Radiation + radiationDelta, 0, maxValue);

            UpdateSliders();
            Log($"--- END Sol {CurrentSol} | Food {Food} | Water {Water} | Rad {Radiation} (+{radiationDelta}) | " +
                $"Energy left {Energy} | Tasks {TasksCompleted}/{requiredTasks} [{string.Join(",", completedTaskIds)}]");

            if (CheckGameEnd())
            {
                return;
            }

            NotifyPlayer("Sol ended.");
            BeginSol(solIndex + 1);
        }

        private void UpdateSliders(bool instant = false)
        {
            lastSliderFood = Food;
            lastSliderWater = Water;
            lastSliderRad = Radiation;
            lastSliderEnergy = Energy;

            ApplySlider(foodSlider, Food, instant);
            ApplySlider(waterSlider, Water, instant);
            ApplySlider(energySlider, EnergyPercent, instant);
            ApplySlider(radiationSlider, Radiation, instant);
        }

        public void toggleBook(bool action)
        {
            if (bookPanel != null)
            {
                bookPanel.SetActive(action);
                if (CameraMove.Instance != null)
                {
                    CameraMove.Instance.canMove = !action;
                }
            }
        }

        private void SelectRandomCrew(int count)
        {
            ActiveCrew.Clear();
            if (AstronautPresets.DefaultAstronauts == null || AstronautPresets.DefaultAstronauts.Count == 0)
            {
                Debug.LogWarning("[GameManager] список AstronautPresets.DefaultAstronauts пуст");
                return;
            }

            List<AstronautCard> pool = new List<AstronautCard>(AstronautPresets.DefaultAstronauts);
            int targetCount = Mathf.Min(count, pool.Count);

            if (SelectManager.Instance != null)
            {
                int[] ids = SelectManager.Instance.GetIdSelectedCards();
                if (ids != null && ids.Length > 0)
                {
                    for (int i = 0; i < Mathf.Min(targetCount, ids.Length); i++)
                    {
                        int cardIndex = Mathf.Clamp(ids[i], 0, pool.Count - 1);
                        ActiveCrew.Add(pool[cardIndex]);
                    }
                    return;
                }
            }

            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                AstronautCard chosen = pool[randomIndex];
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

            for (int i = 1; i < data.sols.Count; i++)
            {
                if (IsHazardScenario(data.sols[i - 1]) && IsHazardScenario(data.sols[i]))
                {
                    data.sols[i].availableEnergy = MaxEnergyCells;
                    data.sols[i].baseRadiationDelta = Mathf.Min(data.sols[i].baseRadiationDelta, hazardRadiationThreshold - 5);
                }
            }

            if (data.sols.Count != 7)
                Debug.LogWarning($"[GameManager] В JSON {data.sols.Count} солов, а не 7. Результат покажется после сола #{data.sols[data.sols.Count - 1].solNumber}.");

            int taskCount = data.tasks != null ? data.tasks.Count : 0;
            if (taskCount < requiredTasks)
                Debug.LogWarning($"[GameManager] В JSON всего {taskCount} заданий, а для победы нужно {requiredTasks}. Победа невозможна.");

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
            SolTimer.Instance?.StopSol();
            ClearAllPopups();

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

            AudioClip endClip = won ? victoryVoice : defeatVoice;
            if (endClip != null)
            {
                NotificationManager.Instance?.ShowNotification(summaryText, endClip);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverPanel.transform.SetAsLastSibling();
            }
            if (gameOverText != null)
            {
                gameOverText.alignment = TextAlignmentOptions.Center;
            }

            bool panelVisible = gameOverPanel != null && gameOverPanel.activeInHierarchy;
            bool textVisible = gameOverText != null && gameOverText.gameObject.activeInHierarchy;
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

        private IEnumerator ReturnToMenuRoutine(bool won, string summaryText)
        {
            if (gameOverText != null && gameOverFont != null)
            {
                gameOverText.font = gameOverFont;
            }

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
                yield return new WaitForSecondsRealtime(1f);
                remaining -= 1f;
            }

            Time.timeScale = 1f;
            Log("Returning to main menu: " + mainMenuSceneName);
            if (string.IsNullOrEmpty(mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[GameManager] Сцена '{mainMenuSceneName}' не найдена в File > Build Settings.");
                yield break;
            }
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // --- МОДУЛИ БАЗЫ ---

        public void ActivateGreenhouse()
        {
            if (Phase != GamePhase.Planning) return;
            if (!TrySpendEnergy("Not enough energy to run the Greenhouse.")) return;

            bool upgraded = completedTaskIds.Contains(2);
            int foodGain = upgraded ? 40 : 35;
            int waterCost = upgraded ? 5 : 10;

            Food = Mathf.Clamp(Food + foodGain, 0, maxValue);
            Water = Mathf.Clamp(Water - waterCost, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer($"Greenhouse cycle started: +{foodGain}% Food, -{waterCost}% Water.");
        }

        public void ActivateWaterPlant()
        {
            if (Phase != GamePhase.Planning) return;
            if (!TrySpendEnergy("Not enough energy to run the Water Plant.")) return;

            Water = Mathf.Clamp(Water + 40, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 5, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Water Plant started: +40% Water, +5% Radiation.");
        }

        public void ActivateHabitatShields()
        {
            if (Phase != GamePhase.Planning) return;
            if (shieldsActiveToday)
            {
                PlaySfx(warningSfx);
                NotifyPlayer("Habitat Shields are already active today.");
                return;
            }
            if (!TrySpendEnergy("Not enough energy to power Habitat Shields.")) return;

            Radiation = Mathf.Clamp(Radiation - 25, 0, maxValue);
            shieldsActiveToday = true;
            PlaySfx(radiationRepairSfx);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Habitat Shields online: -25% Radiation.");
        }

        public void ActivateDecontamination()
        {
            if (Phase != GamePhase.Planning) return;
            if (Radiation <= 0)
            {
                NotifyPlayer("Radiation level is already at 0%.");
                return;
            }
            if (!TrySpendEnergy("Not enough energy to run Radiation Scrubber.")) return;

            Radiation = Mathf.Clamp(Radiation - 50, 0, maxValue);
            PlaySfx(radiationRepairSfx); // Звук починки базы от радиации
            UpdateSliders();
            NotifyChanged();

            if (!hasShownDecontaminationTutorial)
            {
                hasShownDecontaminationTutorial = true;
                NotifyPlayer(
                    "Radiation Scrubber activated (-50% Rad)! Did you know? Unlike Earth, Mars has no global magnetic field, so active decontamination is essential to protect astronauts from cosmic rays.",
                    decontaminationTutorialVoice
                );
            }
            else
            {
                NotifyPlayer("Radiation Scrubber activated: -50% Radiation.");
            }
        }

        // --- ЗАДАНИЯ (ПУЗЫРЬКИ НА КАРТЕ С УНИКАЛЬНЫМИ БОНУСАМИ И ОЗВУЧКОЙ) ---

        public void ExecuteRoverTask(int taskId) => TryExecuteRoverTask(taskId);

        public bool TryExecuteRoverTask(int taskId)
        {
            if (Phase != GamePhase.Planning) return false;
            if (completedTaskIds.Contains(taskId))
            {
                NotifyPlayer("This research task is already completed.");
                return false;
            }
            if (!TrySpendEnergy("Not enough energy for a Rover expedition.")) return false;

            Food = Mathf.Clamp(Food - 8, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 8, 0, maxValue);
            completedTaskIds.Add(taskId);

            // Магический звук выполнения задания
            PlaySfx(taskMagicSfx);

            string bonusAndLesson = ApplyTaskReward(taskId);

            AudioClip taskVoice = (taskCompleteVoices != null && taskId >= 1 && taskId <= taskCompleteVoices.Length)
                ? taskCompleteVoices[taskId - 1]
                : null;

            UpdateSliders();
            NotifyChanged();
            Log($"Rover task #{taskId} done -> {TasksCompleted}/{requiredTasks} | {bonusAndLesson}");
            NotifyPlayer($"Task #{taskId} complete ({TasksCompleted}/{requiredTasks}): {bonusAndLesson}", taskVoice);
            return true;
        }

        private string ApplyTaskReward(int taskId)
        {
            switch (taskId)
            {
                case 1:
                    Water = Mathf.Clamp(Water + 25, 0, maxValue);
                    return "Subsurface ice found (+25% Water)! Martian regolith hides frozen water just beneath the surface—vital for future colonies.";
                case 2:
                    return "Greenhouse upgraded (+40% Food, -5% Water cost)! Closed-loop hydroponics recycle plant transpiration just like on the ISS.";
                case 3:
                    Radiation = Mathf.Clamp(Radiation - 5, 0, maxValue);
                    return "Sensors calibrated (-5% Rad now, -3% Rad/sol)! Continuous dosimetry warns crews before solar radiation reaches dangerous levels.";
                case 4:
                    return "High-yield crops unlocked (-5 daily Food use)! NASA OSDR experiments show shielded crops grow healthier roots and leaves in space.";
                case 5:
                    return "Hull reinforced (reduces Sol 7 storm damage)! Thick shielding blocks high-energy solar protons during extreme X-class flares.";
                case 6:
                    Energy = Mathf.Clamp(Energy + 1, 0, MaxEnergyCells);
                    Food = Mathf.Clamp(Food + 10, 0, maxValue);
                    Water = Mathf.Clamp(Water + 10, 0, maxValue);
                    return "Earth supply drop (+1 Energy, +10% Food & Water)! Radio signals between Mars and Earth take 4 to 24 minutes each way.";
                default:
                    return "Research data logged to mission archive.";
            }
        }

        private Transform GetTaskSpawnPoint(int taskId)
        {
            switch (taskId)
            {
                case 1: return task1SpawnPoint;
                case 2: return task2SpawnPoint;
                case 3: return task3SpawnPoint;
                case 4: return task4SpawnPoint;
                case 5: return task5SpawnPoint;
                case 6: return task6SpawnPoint;
                default: return null;
            }
        }

        private string GetShortTaskName(int taskId)
        {
            switch (taskId)
            {
                case 1: return "Ice/Soil";
                case 2: return "Greenhouse";
                case 3: return "Sensors";
                case 4: return "Crops";
                case 5: return "Hull";
                case 6: return "Comms";
                default: return "Task";
            }
        }

        public void SpawnDailyTaskPopups()
        {
            if (data == null || data.tasks == null || resourcePopupPrefab == null) return;

            var availableTasks = new List<TaskData>();
            foreach (var task in data.tasks)
            {
                if (!completedTaskIds.Contains(task.id) && GetTaskSpawnPoint(task.id) != null)
                {
                    availableTasks.Add(task);
                }
            }

            int countToSpawn = Mathf.Min(dailyTasksToSpawn, availableTasks.Count);
            for (int i = 0; i < countToSpawn; i++)
            {
                int randIdx = UnityEngine.Random.Range(0, availableTasks.Count);
                TaskData chosen = availableTasks[randIdx];
                availableTasks.RemoveAt(randIdx);

                Transform point = GetTaskSpawnPoint(chosen.id);
                int taskId = chosen.id;
                string label = $"<size=65%>#{chosen.id} {GetShortTaskName(chosen.id)}</size>";

                GameObject popupObj = SpawnCollectiblePopup(label, taskIcon, point, () => TryExecuteRoverTask(taskId));
                if (popupObj != null)
                {
                    activeTaskPopups.Add(popupObj);
                }
            }
        }

        private void ClearAllPopups()
        {
            if (foodPopupObj != null) { Destroy(foodPopupObj); foodPopupObj = null; }
            if (waterPopupObj != null) { Destroy(waterPopupObj); waterPopupObj = null; }

            foreach (var obj in activeTaskPopups)
            {
                if (obj != null) Destroy(obj);
            }
            activeTaskPopups.Clear();
        }

        private bool TrySpendEnergy(string notEnoughMessage)
        {
            if (Energy < EnergyCellCost)
            {
                PlaySfx(warningSfx);
                NotifyPlayer(notEnoughMessage, notEnoughEnergyVoice);
                return false;
            }
            Energy -= EnergyCellCost;
            Log($"Energy spent -> {Energy}/{MaxEnergyCells}");
            return true;
        }

        // --- СОЛНЕЧНЫЕ ПАНЕЛИ ---

        public void OpenSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;

            if (solarPanelsUsedToday)
            {
                PlaySfx(warningSfx);
                NotifyPlayer("Solar panels can only be deployed once per sol.");
                return;
            }

            solarPanelsUsedToday = true;
            solarPanelsOpen = true;

            PlaySfx(solarSwitchSfx); // Звук переключателя солнечных батарей
            UpdateSliders();
            UpdateSolarPanelsUI();
            NotifyChanged();
            Log("Solar panels opened (used for this sol)");
            NotifyPlayer("Solar panels deployed successfully.");
        }

        public void CloseSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;
            if (!solarPanelsOpen) return;

            solarPanelsOpen = false;
            PlaySfx(solarSwitchSfx); // Звук переключателя солнечных батарей
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

        public void SpawnEndSolResourcePopups()
        {
            foodPopupObj = SpawnCollectiblePopup("+22", foodIcon, greenhouseSpawnPoint, () =>
            {
                if (Phase == GamePhase.GameOver) return false;
                if (!TrySpendEnergy("Not enough energy to collect food!")) return false;

                Food = Mathf.Clamp(Food + 22, 0, maxValue);
                UpdateSliders();
                NotifyChanged();

                if (!hasShownFoodTutorial)
                {
                    hasShownFoodTutorial = true;
                    PlaySfx(resourceCollectSfx); // Звук сбора еды (первый раз)
                    NotifyPlayer(
                        "Harvested +22% Food for 1 energy cell! Tip: Resource bubbles appear at the start of a new sol from overnight production—collect them before the sol ends.",
                        firstFoodVoice
                    );
                }
                else
                {
                    NotifyPlayer("Collected food (+22%), spent 1 energy cell.");
                }
                return true;
            });

            waterPopupObj = SpawnCollectiblePopup("+22", waterIcon, waterPlantSpawnPoint, () =>
            {
                if (Phase == GamePhase.GameOver) return false;
                if (!TrySpendEnergy("Not enough energy to collect water!")) return false;

                Water = Mathf.Clamp(Water + 22, 0, maxValue);
                UpdateSliders();
                NotifyChanged();

                if (!hasShownWaterTutorial)
                {
                    hasShownWaterTutorial = true;
                    PlaySfx(resourceCollectSfx); // Звук сбора воды (первый раз)
                    NotifyPlayer(
                        "Collected +22% Water for 1 energy cell! Tip: Atmospheric condensers extract water from thin Martian air overnight, costing 1 energy cell to pump into storage.",
                        firstWaterVoice
                    );
                }
                else
                {
                    NotifyPlayer("Collected water (+22%), spent 1 energy cell.");
                }
                return true;
            });
        }

        private GameObject SpawnCollectiblePopup(string message, Sprite icon, Transform spawnPoint, Func<bool> onCollectAction)
        {
            if (resourcePopupPrefab == null || spawnPoint == null) return null;

            GameObject popupObj = Instantiate(resourcePopupPrefab, spawnPoint.position, Quaternion.identity);
            if (popupObj.TryGetComponent<ResourcePopup>(out var popup))
            {
                popup.Initialize(message, icon, onCollectAction);
            }
            return popupObj;
        }

        private void Log(string msg)
        {
            if (debugLog) Debug.Log("[GameManager] " + msg);
        }

#if UNITY_EDITOR
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