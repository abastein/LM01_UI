using LM01_UI.Models;
using System;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public class PlcService
    {
        private const int CommandLength = 256;
        private const char PaddingChar = '\0';

        // "Spomin" za zadnje veljavne parametre recepture
        private string _parameterPayload = string.Empty;

        // Metoda za sestavljanje sporočila, ki upošteva trenutni payload
        private string BuildPaddedCommand(string commandCode, string? payload = null)
        {
            // Če je podan specifičen payload (npr. za STOP), ga uporabimo.
            // Sicer uporabimo shranjen _parameterPayload.
            string payloadToUse = payload ?? _parameterPayload;
            string fullCommand = commandCode + payloadToUse;
            if (fullCommand.Length > CommandLength)
            {
                fullCommand = fullCommand.Substring(0, CommandLength);
            }
            return fullCommand.PadRight(CommandLength, PaddingChar);
        }

        // Ukazi so sedaj metode, ker je njihova vsebina odvisna od stanja
        public string GetStartCommand() => BuildPaddedCommand("001001");
      //  public string GetStatusCommand() => BuildPaddedCommand("001000", string.Empty);
        public string GetStatusCommand() => BuildPaddedCommand("001000");

        public string GetStopCommand()
        {
            // STOP ukaz vedno pošlje prazen payload in ponastavi shranjenega
            _parameterPayload = string.Empty;
            string emptyPayload = new string(PaddingChar, CommandLength - 6);
            return BuildPaddedCommand("001002", emptyPayload);
        }

        public string GetUnloadCommand() => BuildPaddedCommand("001004");

        public string BuildLoadCommand(Recipe recipe)
        {
            var parameterBuilder = new StringBuilder();
            parameterBuilder.Append(string.Format("{0:000}", recipe.Id));
            parameterBuilder.Append(string.Format("{0:00}", recipe.Steps.Count));
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                parameterBuilder.Append(string.Format("{0:00}", step.StepNumber));
                parameterBuilder.Append(string.Format("{0:00}", (int)step.Function));
                parameterBuilder.Append(string.Format("{0:0000}", step.SpeedRPM));
                parameterBuilder.Append(string.Format("{0:0}", (int)step.Direction));
                parameterBuilder.Append(string.Format("{0:0000}", step.TargetXDeg));
               // parameterBuilder.Append(string.Format("{0:00}", step.Repeats));
                parameterBuilder.Append(string.Format("{0:0000}", step.PauseMs));
            }

            // Shranimo nov payload v "spomin"
            _parameterPayload = parameterBuilder.ToString();

            // Sestavimo celoten LOAD ukaz
            return BuildPaddedCommand("001003");
        }
    }
}