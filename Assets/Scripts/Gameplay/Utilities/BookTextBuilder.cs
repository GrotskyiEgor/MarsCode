using System.Text;
using UnityEngine;

namespace MarsOutpost
{
    public static class BookTextBuilder
    {
        public static (string bookText, string notificationText) CreateBookText(GameManager gm)
        {
            if (gm == null) return (string.Empty, string.Empty);

            var sb = new StringBuilder();
            string notificationText;

            int totalMinutes = (gm.CurrentSol - 1) * (24 * 60 + 39);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            sb.AppendLine("<margin=4%><line-height=118%><align=\"center\">");

            // --- ШАПКА ЖУРНАЛА ---
            sb.AppendLine($"<size=120%><b><color=#B84A00>SOL {gm.CurrentSol} / 7</color></b></size>  <color=#4A5568>|  {hours}h {minutes:D2}m</color>");
            sb.AppendLine();
            sb.AppendLine();

            if (gm.CurrentSol == 1)
            {
                // --- БРИФИНГ ПЕРВОГО ДНЯ ---
                sb.AppendLine("<size=105%><b><color=#1A202C>WELCOME TO JEZERO CRATER, COMMANDER!</color></b></size>");
                sb.AppendLine("<color=#2D3748>Keep the outpost operational for <b>7 sols</b> and prepare the habitat for the incoming solar storm.</color>");
                sb.AppendLine();

                // --- ЭКИПАЖ ---
                sb.AppendLine("<color=#1E4E8C><b>- ACTIVE CREW -</b></color>");
                if (gm.ActiveCrew != null && gm.ActiveCrew.Count > 0)
                {
                    foreach (var crew in gm.ActiveCrew)
                    {
                        sb.AppendLine($"<color=#1A202C><b>{crew.name} {crew.surname}</b></color> <color=#4A5568>| {crew.profession}</color>");
                    }
                }
                else
                {
                    sb.AppendLine("<color=#4A5568>No crew assigned</color>");
                }
                sb.AppendLine();

                // --- ЦЕЛИ И ПРАВИЛА ---
                sb.AppendLine("<color=#1E4E8C><b>- MISSION DIRECTIVES -</b></color>");
                sb.AppendLine($"<color=#975A16><b>Power Today:</b></color> <color=#1A202C><b>{gm.Energy} Energy Cells</b> (1 action = 1 cell)</color>");
                sb.AppendLine($"<color=#22543D><b>Victory Goal:</b></color> <color=#1A202C>Complete <b>{gm.requiredTasks} Rover Tasks</b> & survive Sol 7</color>");
                sb.AppendLine("<size=92%><color=#4A5568>Keep Food & Water above 0% and Radiation below 100%.</color></size>");
                sb.AppendLine();

                sb.AppendLine();
                sb.AppendLine($"<color=#1A202C>Food <b>{gm.Food}%</b>   |   Water <b>{gm.Water}%</b>   |   Rad <b>{gm.Radiation}%</b></color>");

                notificationText = $"Welcome to Jezero Crater, Commander! Your solar arrays are online and generated {gm.Energy} energy cells for today's operations. Allocate your power wisely between life support and rover research before the Martian day ends.";
            }
            else
            {
                // --- ЕЖЕДНЕВНЫЙ ОТЧЁТ (СОЛЫ 2–7) ---
                if (gm.CurrentScenario != null)
                {
                    sb.AppendLine($"<size=108%><b><color=#9C4221>{gm.CurrentScenario.eventTitle.ToUpper()}</color></b></size>");
                    sb.AppendLine($"<color=#2D3748>{gm.CurrentScenario.eventDescription}</color>");
                    sb.AppendLine();

                    // --- ПРОГНОЗ И ПРОГРЕСС ---
                    sb.AppendLine("<color=#1E4E8C><b>- DAILY TELEMETRY -</b></color>");

                    bool isPowerReduced = gm.Energy < gm.EnergyTotal;
                    string powerDisplay = isPowerReduced
                        ? $"<color=#9B2C2C><b>+{gm.Energy} Cell (Panels Off!)</b></color>"
                        : $"<color=#975A16><b>+{gm.Energy} Cells</b></color>";

                    sb.AppendLine($"<color=#1A202C>Power:</color> {powerDisplay}   <color=#4A5568>|</color>   <color=#1A202C>Rad Forecast:</color> <color=#9B2C2C><b>+{gm.CurrentScenario.baseRadiationDelta}%</b></color>");
                    sb.AppendLine($"<color=#1A202C>Scientific Progress: <b>{gm.TasksCompleted} / {gm.requiredTasks} Tasks</b></color>");
                    sb.AppendLine();

                    // --- СПРАВКА NASA ---
                    sb.AppendLine("<color=#1E4E8C><b>- NASA FIELD NOTE -</b></color>");
                    sb.AppendLine($"<size=92%><i><color=#2C5282>{gm.CurrentScenario.nasaFact}</color></i></size>");
                    sb.AppendLine();

                    notificationText = $"Sol {gm.CurrentSol} report: {gm.CurrentScenario.eventTitle}. {gm.CurrentScenario.eventDescription} Your power grid generated {gm.Energy} energy cells - plan your module actions carefully.";
                }
                else
                {
                    sb.AppendLine($"<color=#975A16><b>Power Grid: +{gm.Energy} Energy Cells</b></color>");
                    sb.AppendLine($"<color=#1A202C>Scientific Progress: <b>{gm.TasksCompleted} / {gm.requiredTasks} Tasks</b></color>");
                    sb.AppendLine();

                    notificationText = $"Sol {gm.CurrentSol} has begun on the Martian surface. Your solar arrays generated {gm.Energy} energy cells - manage your life support and research priorities wisely.";
                }

                // --- НИЖНЯЯ СТРОКА СОСТОЯНИЯ ---
                sb.AppendLine();
                sb.AppendLine($"<color=#1A202C>Food <b>{gm.Food}%</b>   |   Water <b>{gm.Water}%</b>   |   Rad <b>{gm.Radiation}%</b></color>");
            }

            sb.Append("</align></line-height></margin>");

            Debug.Log("BookText and Notification text created.");
            return (sb.ToString(), notificationText);
        }
    }
}