using LM01_UI.Models;
using LM01_UI.Enums;
using System;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public class PlcService
    {
        //private const int CommandLength = 256;
        //private const char PaddingChar = '\0';

        // "Spomin" za zadnje veljavne parametre recepture
        private string _parameterPayload = string.Empty;

        /// <summary>
        /// Sequence PLC expects at the end of each command.
        /// </summary>
        private const string Terminator = "$R$L";

        // Metoda za sestavljanje sporočila, ki upošteva trenutni payload
        private string BuildPaddedCommand(string commandCode, string? payload = null)
        {
            // Če je podan specifičen payload (npr. za STOP), ga uporabimo.
            // Sicer uporabimo shranjen _parameterPayload.
            string payloadToUse = payload ?? _parameterPayload;
            string fullCommand = commandCode + payloadToUse;
            //if (fullCommand.Length > CommandLength)
            //{
            //    fullCommand = fullCommand.Substring(0, CommandLength);
            //}
            //return fullCommand.PadRight(CommandLength, PaddingChar);
            //return fullCommand;
           // return commandCode + "$R$L";
            return fullCommand + Terminator;
        }

        // Ukazi so sedaj metode, ker je njihova vsebina odvisna od stanja
        public string GetStartCommand() => BuildPaddedCommand("001001");
        public string GetStatusCommand() => BuildPaddedCommand("001000");
        //public string GetStatusCommand() => BuildPaddedCommand("001000");

        public string GetStopCommand()
        {
            // STOP ukaz vedno pošlje prazen payload in ponastavi shranjenega
            _parameterPayload = string.Empty;
            //string emptyPayload = new string(PaddingChar, CommandLength - 6);
            return BuildPaddedCommand("001002");
        }

        public string GetUnloadCommand() => BuildPaddedCommand("00100300000");

        public string BuildLoadCommand(Recipe recipe)
        {
            var parameterBuilder = new StringBuilder();
            parameterBuilder.Append(string.Format("{0:000}", recipe.Id));
            parameterBuilder.Append(string.Format("{0:00}", recipe.Steps.Count));
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                parameterBuilder.Append(string.Format("{0:00}", step.StepNumber));
                parameterBuilder.Append(string.Format("{0:00}", (int)step.Function));
                // Convert speed from RPM to pulses per second for the PLC
                var speedPps = (int)Math.Round(step.SpeedRPM * 200.0 / 60.0);
                parameterBuilder.Append(string.Format("{0:0000}", speedPps));

                // Convert direction enum to "+" or "-" expected by the PLC
                parameterBuilder.Append(step.Direction == DirectionType.CW ? "1+" : "2-");

                // Convert target degrees to pulses
                var targetPulses = (int)Math.Round(step.TargetXDeg / 1.8);
                parameterBuilder.Append(string.Format("{0:000}", targetPulses));
                // parameterBuilder.Append(string.Format("{0:00}", step.Repeats));
                parameterBuilder.Append(string.Format("{0:0000}", step.PauseMs));
                //parameterBuilder.Append("$R$L"); 
            }

            // Shranimo nov payload v "spomin"
            _parameterPayload = parameterBuilder.ToString();


            // Sestavimo celoten LOAD ukaz
            //return BuildPaddedCommand("001003");
            //return BuildPaddedCommand("001003" + _parameterPayload);
            return BuildPaddedCommand("001003", _parameterPayload);
        }
    }
}