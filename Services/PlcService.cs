using LM01_UI.Models;
using LM01_UI.Enums;
using System;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public class PlcService
    {
        private const int CommandLength = 256;
        private const char PaddingChar = '0';

        // "Spomin" za zadnje veljavne parametre recepture
        private string _parameterPayload = string.Empty;

        /// <summary>
        /// Sequence PLC expects at the end of each command.
        /// </summary>
        private const string Terminator = "$R$L";

        // Recipe ID identifying manual mode in the PLC
        private const string ManualRecipeId = "007";


        // Metoda za sestavljanje sporočila, ki upošteva trenutni payload
        private string BuildPaddedCommand(string commandCode, string? payload = null)
        {
            // Če je podan specifičen payload (npr. za STOP), ga uporabimo.
            // Sicer uporabimo shranjen _parameterPayload.
           // string payloadToUse = payload ?? _parameterPayload;
            string fullCommand = commandCode + payload;
            //if (fullCommand.Length > CommandLength)
            //{
            //    fullCommand = fullCommand.Substring(0, CommandLength);
            //}
            fullCommand = fullCommand.PadRight(CommandLength, PaddingChar);
            //return fullCommand;
           // return commandCode + "$R$L";
            return fullCommand + Terminator;
        }

        // Ukazi so sedaj metode, ker je njihova vsebina odvisna od stanja
        public string GetStartCommand() => "001001" + Terminator;
        public string GetStatusCommand() => "001000" + Terminator;
        //public string GetStatusCommand() => BuildPaddedCommand("001000");

        public string GetStopCommand()
        {
            // STOP ukaz vedno pošlje prazen payload in ponastavi shranjenega
            _parameterPayload = string.Empty;
            //string emptyPayload = new string(PaddingChar, CommandLength - 6);
            return "001002" + Terminator;
        }

        public string GetUnloadCommand() => BuildPaddedCommand("0010030000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        private string BuildManualLoadCommand(int rpm, DirectionType direction, double target, bool isDegrees = true)
        {
            // Convert RPM to pulses per second for the PLC
            var speedPps = (int)Math.Round(rpm * 200.0 / 60.0);

            // Convert direction enum to the PLC format (1+ for CW, 2- for CCW)
            var directionCode = direction == DirectionType.CW ? "1+" : "2-";

            // Convert target distance to pulses (default assumes degrees)
            var targetPulses = isDegrees
                ? (int)Math.Round(target / 1.8)
                : (int)Math.Round(target);

            // Build payload according to PLC protocol
            // Recipe ID placeholder + step count + step number + function rotate code
            var builder = new StringBuilder();
            builder.Append(ManualRecipeId); // Recipe ID for manual mode
            builder.Append("01"); // Step count
            builder.Append("01"); // Step number
            builder.Append("01"); // Function code for rotate
            builder.Append(string.Format("{0:0000}", speedPps)); // Speed in pulses-per-second
            builder.Append(directionCode); // Direction code
            builder.Append(string.Format("{0:000}", targetPulses)); // Target pulses
            builder.Append("0000"); // Pause

            // Pad payload to required length and build final command
            var payload = builder.ToString().PadRight(CommandLength -6, PaddingChar);

            return BuildPaddedCommand("001003", payload);
        }

        public string GetManualLoadCommand(int rpm, DirectionType direction, double target = 360, bool isDegrees = true) =>
            BuildManualLoadCommand(rpm, direction, target, isDegrees);

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