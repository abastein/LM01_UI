using LM01_UI.Models;
using System; // Potrebno za String.Format
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public static class PlcService
    {
        private const int CommandLength = 256;
        private const char PaddingChar = '\0';

        public static string StartCommand => "START".PadRight(CommandLength, PaddingChar);
        public static string StopCommand => "STOP".PadRight(CommandLength, PaddingChar);
        public static string StatusCommand => "STATUS".PadRight(CommandLength, PaddingChar);

        public static string BuildLoadCommand(Recipe recipe)
        {
            var commandBuilder = new StringBuilder();

            commandBuilder.Append("LOAD:");
            // POPRAVEK: Uporaba String.Format za združljivost
            commandBuilder.Append(String.Format("{0:000}", recipe.Id));
            commandBuilder.Append(String.Format("{0:00}", recipe.Steps.Count));
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                commandBuilder.Append(String.Format("{0:00}", step.StepNumber));
                commandBuilder.Append(String.Format("{0:0}", (int)step.Function));
                commandBuilder.Append(String.Format("{0:000}", step.SpeedRPM));
                commandBuilder.Append(String.Format("{0:0}", (int)step.Direction));
                commandBuilder.Append(String.Format("{0:0000}", step.TargetXDeg));
                commandBuilder.Append(String.Format("{0:00}", step.Repeats));
                commandBuilder.Append(String.Format("{0:00000}", step.PauseMs));
            }

            string command = commandBuilder.ToString();
            return command.PadRight(CommandLength, PaddingChar);
        }
    }
}