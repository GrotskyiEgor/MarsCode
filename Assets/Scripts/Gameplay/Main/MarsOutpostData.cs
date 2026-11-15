using System;
using System.Collections.Generic;

namespace MarsOutpost
{
    //TODO: Ваня
    // =====================================================================
    //  Модели для mars_outpost_master.json (имена полей = имена в JSON,
    //  иначе JsonUtility их не заполнит. Не переименовывать!)
    // =====================================================================

    /// <summary>
    /// Коренной контейнер для инициализации списков и строк:
    /// 
    /// </summary>
    [Serializable]
    public class MarsOutpostData
    {
        public string missionTitle;
        public string attributionBadge;
        public List<SolScenario> sols = new List<SolScenario>();
        public List<TaskData> tasks = new List<TaskData>();
        public List<AstronautCard> kidFriendlyCards = new List<AstronautCard>();
        public List<GlossaryEntry> glossary = new List<GlossaryEntry>();
    }

    /// <summary>
    /// Берёт на себя ОДИН СОЛ:
    /// сколько энергии,
    /// какая радиация,
    /// какой случайный ивент,
    /// какая справка NASA.
    /// </summary>
    [Serializable]
    public class SolScenario
    {
        public int solNumber;
        public int availableEnergy;
        public int baseRadiationDelta;
        public string eventTitle;
        public string eventDescription;
        public RadlabMeasurement radlabMeasurement;
        public string nasaFact;
    }

    /// <summary>
    /// Данные с радиационной станции RadLab:
    /// номер сола,
    /// место измерения,
    /// реальное время измерения,
    /// доза за час,
    /// доза за день,
    /// идентификатор прибора.
    /// </summary>
    [Serializable]
    public class RadlabMeasurement
    {
        public int solNumber;
        public string location;
        public string instrumentId;
        public string realTimestamp;
        public float doseRate_uGy_hour;
        public float dailyDose_uGy_day;
        public int gameBaseRadiationDelta;
    }


    /// <summary>
    /// Одной из 6 задач для марсохода:
    /// айди,
    /// заголовок,
    /// ссылка на справку NASA.
    /// </summary>
    [Serializable]
    public class TaskData
    {
        public int id;
        public string title;
        public string nasaRef;
    }


    /// <summary>
    /// Карточка для детей з базы NASA OSDR:
    /// айди связанного исследования,
    /// модуль, в котором выполняется задание,
    /// номер задания,
    /// заголовок карточки,
    /// текст объяснения.
    /// </summary>
    [Serializable]
    public class AstronautCard
    {
        public int id;
        public string module;
        public int taskNumber;
        public string name;
        public string surname;
        public string profession;
        public string sex;
    }

    /// <summary>
    /// одна пара - термин и его определение, для глосария.
    /// </summary>
    [Serializable]
    public class GlossaryEntry
    {
        public string term;
        public string definition;
    }

    // =====================================================================
    //  Игровые типы (не из JSON)
    // =====================================================================

    /// <summary>
    /// Описывает состояния игры:
    /// игра не запущена,
    /// игрок кликает по модулях и тратит енергию,
    /// Открытие отчёта после заврешния сола,
    /// финал мисиии,
    /// в случае ненахождения файла вывод ошибки.
    /// </summary>
    public enum GamePhase { NotStarted, Reading, Planning, SolReport, GameOver, Error }

    /// <summary>
    /// Для действий с модулями:
    /// теплица,
    /// водоочиститель,
    /// щиты жилого модуля,
    /// ровер выполняет задание из списка.
    /// </summary>
    public enum ModuleType
    {
        Greenhouse,     // теплица
        WaterPlant,     // водоочиститель
        HabitatShield,  // щиты жилого модуля
        RoverTask       // ровер выполняет задание из списка
    }

    /// <summary>
    /// все ресурсы, которые нужно отслеживать в игре:
    /// </summary>
    public enum ResourceType { Food, Water, Energy, Radiation }

    /// <summary>
    /// Флажки для причин окончания игры:
    /// </summary>
    [Flags]
    public enum GameOverReason
    {
        None = 0,
        Starved = 1,          // еда 0
        Dehydrated = 2,       // вода 0
        Irradiated = 4,       // заражение 100
        NotEnoughTasks = 8    // выжили, но заданий меньше минимума
    }

    /// <summary>Один пункт в очереди действий на текущий сол.</summary>
    public struct PlannedAction
    {
        public ModuleType module;
        public int taskId; // только для RoverTask, иначе 0

        public PlannedAction(ModuleType module, int taskId = 0)
        {
            this.module = module;
            this.taskId = taskId;
        }
    }

    /// <summary>Одно изменение ресурса: для зелёных (+) и красных (-) цифр в UI.</summary>
    public struct ResourceChange
    {
        public string source;        // «Екіпаж», «Теплиця», «Сонячна буря»...
        public ResourceType resource;
        public int amount;           // + или -; для Radiation плюс = ПЛОХО
    }

    public class SolReport
    {
        public int sol;
        public string eventTitle;
        public int foodBefore, foodAfter;
        public int waterBefore, waterAfter;
        public int radiationBefore, radiationAfter;
        public List<ResourceChange> changes = new List<ResourceChange>();
        public List<TaskData> completedTasks = new List<TaskData>();
        public List<AstronautCard> unlockedCards = new List<AstronautCard>();
        public bool isGameOver;
        public GameResult result; // заполнено, только если isGameOver
    }

    public class GameResult
    {
        public bool won;
        public GameOverReason reasons;
        public int solsSurvived;
        public int tasksCompleted;
        public string summary; // объяснение для детей: какое решение к чему привело
    }

    public static class AstronautPresets
    {
        public static List<AstronautCard> DefaultAstronauts = new List<AstronautCard>
        {
            new AstronautCard { id = 1, module = null, taskNumber = 0, name = "Alex", surname = "Morgan", profession = "Commander", sex = "Male" },
            new AstronautCard { id = 2, module = null, taskNumber = 0, name = "Sarah", surname = "Williams", profession = "Pilot", sex = "Female" },
            new AstronautCard { id = 3, module = null, taskNumber = 0, name = "Daniel", surname = "Anderson", profession = "Engineer", sex = "Male" },
            new AstronautCard { id = 4, module = null, taskNumber = 0, name = "Emily", surname = "Carter", profession = "Scientist", sex = "Female" },
            new AstronautCard { id = 5, module = null, taskNumber = 0, name = "Michael", surname = "Bennett", profession = "Doctor", sex = "Male" },
            new AstronautCard { id = 6, module = null, taskNumber = 0, name = "Olivia", surname = "Parker", profession = "Biologist", sex = "Female" },
            new AstronautCard { id = 7, module = null, taskNumber = 0, name = "James", surname = "Wilson", profession = "Mechanic", sex = "Male" },
            new AstronautCard { id = 8, module = null, taskNumber = 0, name = "Sophia", surname = "Turner", profession = "Geologist", sex = "Female" },
            new AstronautCard { id = 9, module = null, taskNumber = 0, name = "William", surname = "Harris", profession = "Technician", sex = "Male" },
            new AstronautCard { id = 10, module = null, taskNumber = 0, name = "Isabella", surname = "Scott", profession = "Astronomer", sex = "Female" },
            new AstronautCard { id = 11, module = null, taskNumber = 0, name = "Ethan", surname = "Brooks", profession = "Engineer", sex = "Male" },
            new AstronautCard { id = 12, module = null, taskNumber = 0, name = "Mia", surname = "Collins", profession = "Doctor", sex = "Female" },
            new AstronautCard { id = 13, module = null, taskNumber = 0, name = "Noah", surname = "Reed", profession = "Pilot", sex = "Male" },
            new AstronautCard { id = 14, module = null, taskNumber = 0, name = "Ava", surname = "Mitchell", profession = "Botanist", sex = "Female" },
            new AstronautCard { id = 15, module = null, taskNumber = 0, name = "Lucas", surname = "Foster", profession = "Physicist", sex = "Male" },
            new AstronautCard { id = 16, module = null, taskNumber = 0, name = "Charlotte", surname = "Hayes", profession = "Psychologist", sex = "Female" },
            new AstronautCard { id = 17, module = null, taskNumber = 0, name = "Benjamin", surname = "Cooper", profession = "Mechanic", sex = "Male" },
            new AstronautCard { id = 18, module = null, taskNumber = 0, name = "Amelia", surname = "Ward", profession = "Chemist", sex = "Female" },
            new AstronautCard { id = 19, module = null, taskNumber = 0, name = "Henry", surname = "Peterson", profession = "Geologist", sex = "Male" },
            new AstronautCard { id = 20, module = null, taskNumber = 0, name = "Grace", surname = "Murphy", profession = "Biologist", sex = "Female" },
            new AstronautCard { id = 21, module = null, taskNumber = 0, name = "Jack", surname = "Richardson", profession = "Technician", sex = "Male" },
            new AstronautCard { id = 22, module = null, taskNumber = 0, name = "Ella", surname = "Bailey", profession = "Scientist", sex = "Female" },
            new AstronautCard { id = 23, module = null, taskNumber = 0, name = "Ryan", surname = "Wood", profession = "Commander", sex = "Male" },
            new AstronautCard { id = 24, module = null, taskNumber = 0, name = "Lily", surname = "Bennett", profession = "Doctor", sex = "Female" },
            new AstronautCard { id = 25, module = null, taskNumber = 0, name = "David", surname = "Ross", profession = "Engineer", sex = "Male" }
        };
    }
}
