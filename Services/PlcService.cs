using LM01_UI.Models;
using System;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public class PlcService
    {
        private const int CommandLength = 256;
        private const char PaddingChar = '\0'; // Uporabimo null znak za polnjenje

        // Ukazi so sedaj lastnosti, ki vedno vrnejo niz pravilne dolžine
        public string StartCommand => "1001".PadRight(CommandLength, PaddingChar);
        public string StopCommand => "1002".PadRight(CommandLength, PaddingChar);
        public string StatusCommand => "1000".PadRight(CommandLength, PaddingChar);

        public string BuildLoadCommand(Recipe recipe)
        {
            var commandBuilder = new StringBuilder();

            // Zgradimo jedro ukaza
            commandBuilder.Append("1003"); // Koda za LOAD
            commandBuilder.Append(string.Format("{0:003}", recipe.Id));
            commandBuilder.Append(string.Format("{0:00}", recipe.Steps.Count));
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                commandBuilder.Append(string.Format("{0:00}", step.StepNumber));
                commandBuilder.Append(string.Format("{0:0}", (int)step.Function));
                commandBuilder.Append(string.Format("{0:000}", step.SpeedRPM));
                commandBuilder.Append(string.Format("{0:0}", (int)step.Direction));
                commandBuilder.Append(string.Format("{0:0000}", step.TargetXDeg));
                commandBuilder.Append(string.Format("{0:00}", step.Repeats));
                commandBuilder.Append(string.Format("{0:00000}", step.PauseMs));
            }

            string command = commandBuilder.ToString();

            // Vrnemo niz, dopolnjen do točne dolžine 256
            return command.PadRight(CommandLength, PaddingChar);
        }
    }
}