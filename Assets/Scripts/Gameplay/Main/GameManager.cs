using TMPro;
using System;
using UnityEngine;
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
        [SerializeField] private int startEnergy = 100;
        [SerializeField] private int startRadiation = 0;
        [SerializeField] private int maxValue = 100;
        private const int MaxEnergyCells = 4;
        private const int EnergyCellPercent = 25; // 1 ячейка = 25%, 3 ячейки = 75%, 4 ячейки = 100%

        [Header("Ежедневный расход экипажа")]
        [SerializeField] private int dailyFoodUse = 10;
        [SerializeField] private int dailyWaterUse = 10;
        [SerializeField] private int requiredTasks = 4;
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

        // Энергия / Солнечные панели
        [Header("Солнечные панели")]
        [SerializeField] private GameObject solarPanelOnVisual;  // Объект/картинка "Включено"
        [SerializeField] private GameObject solarPanelOffVisual; // Объект/картинка "Выключено"
        private bool solarPanelsOpenedToday = false; // Флаг: открывались ли панели в этом соле

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
        public int EnergyTotal => CurrentScenario != null ? CurrentScenario.availableEnergy : 0;
        public bool IsFinalSol => data != null && solIndex == data.sols.Count - 1;
        public SolScenario CurrentScenario =>
            (data != null && solIndex >= 0 && solIndex < data.sols.Count) ? data.sols[solIndex] : null;
        public GameResult FinalResult { get; private set; }

        private MarsOutpostData data;
        private int solIndex = -1;

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
            Energy = Mathf.Clamp(startEnergy, 0, maxValue);
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

            float energyRatio = (float)EnergyTotal / MaxEnergyCells;
            Energy = Mathf.Clamp(Mathf.RoundToInt(energyRatio * maxValue), 0, maxValue);
            shieldsActiveToday = false;

            // Сбрасываем статус солнечных панелей на новый сол
            solarPanelsOpenedToday = false;
            UpdateSolarPanelsUI();

            if (solNumberText != null) solNumberText.text = $"Sol {CurrentSol}";
            UpdateSliders();

            Delay(0.5f, () => StartReadingPhase());
        }

        public void StartReadingPhase()
        {
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
            Debug.Log($"[Sol {CurrentSol} Ended] Food: {Food}%, Water: {Water}%, Energy: {Energy}%, Radiation: {Radiation}% (+{radiationDelta}%)");

            // 3. Проверка победы или поражения
            if (CheckGameEnd())
            {
                return;
            }

            // 4. Переход на следующий сол
            BeginSol(solIndex + 1);
        }

        private void UpdateSliders(bool instant = false)
        {
            ApplySlider(foodSlider, Food, instant);
            ApplySlider(waterSlider, Water, instant);
            ApplySlider(energySlider, Energy, instant);
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

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverText != null) gameOverText.text = summaryText;

            Debug.Log($"[GameManager] GAME OVER ({(won ? "Victory" : "Defeat")}): {summaryText}");
            OnGameOver?.Invoke(FinalResult);
            NotifyChanged();
        }

        // Кнопка 1: Теплица (+15 Еды, -10 Воды, -1 ячейка / 25% Энергии)
        public void ActivateGreenhouse()
        {
            if (Phase != GamePhase.Planning) return;
            if (Energy < EnergyCellPercent) { NotifyPlayer("Not enough energy to run the Greenhouse."); return; }

            Energy -= EnergyCellPercent;
            Food = Mathf.Clamp(Food + 15, 0, maxValue);
            Water = Mathf.Clamp(Water - 10, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Greenhouse cycle started: +15% Food, -10% Water.");
        }

        // Кнопка 2: Водоочиститель (+20 Воды, +5 Радиации, -1 ячейка / 25% Энергии)
        public void ActivateWaterPlant()
        {
            if (Phase != GamePhase.Planning) return;
            if (Energy < EnergyCellPercent) { NotifyPlayer("Not enough energy to run the Water Plant."); return; }

            Energy -= EnergyCellPercent;
            Water = Mathf.Clamp(Water + 20, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 5, 0, maxValue);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer("Water Plant active: +20% Water, +5% Radiation.");
        }

        // Кнопка 3: Щиты жилого модуля (-15 Радиации, -1 ячейка / 25% Энергии)
        public void ActivateHabitatShields()
        {
            if (Phase != GamePhase.Planning) return;
            if (shieldsActiveToday) { NotifyPlayer("Habitat Shields are already active today."); return; }
            if (Energy < EnergyCellPercent) { NotifyPlayer("Not enough energy to power Habitat Shields."); return; }

            Energy -= EnergyCellPercent;
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
            if (Energy < EnergyCellPercent) { NotifyPlayer("Not enough energy for a Rover expedition."); return; }

            Energy -= EnergyCellPercent;
            Food = Mathf.Clamp(Food - 10, 0, maxValue);
            Radiation = Mathf.Clamp(Radiation + 10, 0, maxValue);
            completedTaskIds.Add(taskId);
            UpdateSliders();
            NotifyChanged();
            NotifyPlayer($"Rover completed task #{taskId} ({TasksCompleted}/{requiredTasks} required).");
        }

        // --- УПРАВЛЕНИЕ СОЛНЕЧНЫМИ ПАНЕЛЯМИ ---

        public void OpenSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;

            // Если панели уже открывались сегодня — блокируем повторное открытие
            if (solarPanelsOpenedToday)
            {
                NotifyPlayer("Solar panels have already been deployed today.");
                return;
            }

            // Проверяем наличие энергии (25%)
            if (Energy < EnergyCellPercent) 
            { 
                NotifyPlayer("Not enough energy to deploy solar panels."); 
                return; 
            }

            // Списываем 25% энергии и фиксируем открытие
            Energy -= EnergyCellPercent;
            solarPanelsOpenedToday = true;

            UpdateSliders();
            UpdateSolarPanelsUI();
            NotifyChanged();
            NotifyPlayer("Solar panels deployed successfully (-25% Energy).");
        }

        public void CloseSolarPanels()
        {
            if (Phase != GamePhase.Planning) return;
            
            solarPanelsOpenedToday = false;
            UpdateSolarPanelsUI(); // Исправлено имя метода
            NotifyChanged();
            NotifyPlayer("Solar panels retracted.");
        }

        private void UpdateSolarPanelsUI()
        {
            if (solarPanelOnVisual != null) solarPanelOnVisual.SetActive(solarPanelsOpenedToday);
            if (solarPanelOffVisual != null) solarPanelOffVisual.SetActive(!solarPanelsOpenedToday);
        }

        private static void ApplySlider(SmoothSlider slider, float value, bool instant)
        {
            if (slider == null) return;

            if (instant) slider.SetValueInstant(value);
            else slider.SetTargetValue(value);
        }
    }
}