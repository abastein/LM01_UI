using LM01_UI.Enums;
using LM01_UI.Models;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public class PlcService
    {
        // Ukazi so definirani kot konstante za boljšo preglednost
        public const string StatusCommand = "1000";
        public const string StartCommand = "1001";
        public const string StopCommand = "1002";
        private const string LoadCommandId = "1003";

        /// <summary>
        /// Sestavi ukaz LOAD iz podane recepture po definiranem protokolu.
        /// </summary>
        /// <param name="recipe">Receptura za nalaganje.</param>
        /// <returns>String, pripravljen za pošiljanje na PLC.</returns>
        public string BuildLoadCommand(Recipe recipe)
        {
            // Uporabimo StringBuilder za učinkovito sestavljanje dolgih nizov
            var commandBuilder = new StringBuilder();

            // 1. Glava ukaza
            commandBuilder.Append(LoadCommandId);
            commandBuilder.Append(recipe.Id.ToString("D3")); // D3 formatira število na 3 mesta z vodilnimi ničlami (npr. 2 -> "002")
            commandBuilder.Append(recipe.Steps.Count.ToString("D2")); // D2 formatira na 2 mesti

            // 2. Iteracija skozi korake
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                commandBuilder.Append(step.StepNumber.ToString("D2"));
                commandBuilder.Append(GetFunctionCode(step.Function));

                // Dodajanje parametrov glede na funkcijo
                commandBuilder.Append((step.Function == FunctionType.Rotate ? step.SpeedRPM ?? 0 : 0).ToString("D4"));
                commandBuilder.Append(step.Function == FunctionType.Rotate ? GetDirectionCode(step.Direction) : "0");
                commandBuilder.Append((step.Function == FunctionType.Rotate ? step.TargetXDeg ?? 0 : 0).ToString("D4"));
                commandBuilder.Append((step.Function == FunctionType.Wait ? step.PauseMs ?? 0 : 0).ToString("D4"));
                // Za Repeat bi lahko dodali še en parameter, če bi bil potreben
            }

            return commandBuilder.ToString();
        }

        // Pomožne metode za pretvorbo enumov v kode
        private string GetFunctionCode(FunctionType function)
        {
            return function switch
            {
                FunctionType.Rotate => "01",
                FunctionType.Wait => "02",
                FunctionType.Repeat => "03",
                _ => "00" // Privzeta vrednost
            };
        }

        private string GetDirectionCode(DirectionType direction)
        {
            return direction switch
            {
                DirectionType.CW => "1",
                DirectionType.CCW => "2",
                _ => "0"
            };
        }
    }
}