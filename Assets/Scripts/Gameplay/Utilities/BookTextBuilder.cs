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

            sb.AppendLine("<align=\"center\">");

            sb.AppendLine($"<size=115%><b><color=#FF9E44>SOL {gm.CurrentSol}</color></b></size>  <color=#8FA3B8>|  {hours}h {minutes:D2}m</color>");
            sb.AppendLine("<color=#4A5568>-----------------------------</color>");
            sb.AppendLine();

            if (gm.CurrentSol == 1)
            {
                sb.AppendLine("<b>WELCOME TO JEZERO CRATER, COMMANDER!</b>");
                sb.AppendLine("Survive <b>7 sols</b> and protect the base from the coming solar storm.");
                sb.AppendLine($"<color=#F6E05E>+{gm.EnergyTotal} Energy Cells</color> generated today.");
                sb.AppendLine();

                sb.AppendLine("<color=#63B3ED><b>CREW</b></color>");
                if (gm.ActiveCrew != null && gm.ActiveCrew.Count > 0)
                {
                    foreach (var crew in gm.ActiveCrew)
                    {
                        sb.AppendLine($"{crew.name} {crew.surname} <color=#A0AEC0>({crew.profession})</color>");
                    }
                }
                else
                {
                    sb.AppendLine("<color=#A0AEC0>No crew assigned</color>");
                }
                sb.AppendLine();

                sb.AppendLine("<color=#63B3ED><b>BASE STATUS & RULES</b></color>");
                sb.AppendLine($"Food: <b>{gm.Food}%</b>  •  Water: <b>{gm.Water}%</b>  •  Rad: <b>{gm.Radiation}%</b>");
                sb.AppendLine("<size=90%><color=#CBD5E0>Don't let Food/Water hit 0% or Radiation reach 100%.</color></size>");
                sb.AppendLine();

                sb.AppendLine("<color=#63B3ED><b>CONTROLS</b></color>");
                sb.AppendLine("<b>Research:</b> Complete 4 rover tasks to win");
                sb.AppendLine("<b>Modules:</b> Spend energy on Food, Water & Shields");
                sb.AppendLine();

                sb.AppendLine("<i><color=#A0AEC0>Your onboard assistant will guide you ahead. Good luck!</color></i>");

                notificationText = $"Welcome to Jezero Crater, Commander! Your solar arrays are online and generated {gm.EnergyTotal} energy cells for today's operations. Allocate your power wisely between life support and rover research before the Martian day ends.";
            }
            else
            {
                if (gm.CurrentScenario != null)
                {
                    sb.AppendLine($"<b><color=#F6AD55>{gm.CurrentScenario.eventTitle.ToUpper()}</color></b>");
                    sb.AppendLine(gm.CurrentScenario.eventDescription);
                    sb.AppendLine();
                    sb.AppendLine($"<color=#F6E05E>Power Grid: +{gm.EnergyTotal} Energy Cells</color>");
                    sb.AppendLine();
                    sb.AppendLine($"<size=90%><i><color=#90CDF4>NASA Data: {gm.CurrentScenario.nasaFact}</color></i></size>");
                    sb.AppendLine();

                    notificationText = $"Sol {gm.CurrentSol} report: {gm.CurrentScenario.eventTitle}. {gm.CurrentScenario.eventDescription} Your power grid generated {gm.EnergyTotal} energy cells—plan your module actions carefully.";
                }
                else
                {
                    sb.AppendLine($"<color=#F6E05E>Power Grid: +{gm.EnergyTotal} Energy Cells</color>");
                    sb.AppendLine();

                    notificationText = $"Sol {gm.CurrentSol} has begun on the Martian surface. Your solar arrays generated {gm.EnergyTotal} energy cells—manage your life support and research priorities wisely.";
                }

                sb.AppendLine("<color=#4A5568>-----------------------------</color>");
                sb.AppendLine($"Food <b>{gm.Food}%</b>  •  Water <b>{gm.Water}%</b>  •  Rad <b>{gm.Radiation}%</b>");
            }

            sb.Append("</align>");

            Debug.Log("BookText and Notification text created.");
            return (sb.ToString(), notificationText);
        }
    }
}