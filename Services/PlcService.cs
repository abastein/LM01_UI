using LM01_UI.Models;
using System;
using System.Linq;
using System.Text;

namespace LM01_UI.Services
{
    public static class PlcService
    {
        // 4-znakovne kode ukazov
        public const string StatusCommand = "1000";
        public const string StartCommand = "1001";
        public const string StopCommand = "1002";
        public const string LoadCommandPrefix = "1003";

        // Dolžina bloka s parametri (256 - 4)
        private const int ParameterLength = 252;
        private const char PaddingChar = '\0';

        /// <summary>
        /// Zgradi samo niz s parametri (brez kode ukaza) in ga podaljša na pravilno dolžino.
        /// </summary>
        public static string BuildLoadParameters(Recipe recipe)
        {
            var paramBuilder = new StringBuilder();
            paramBuilder.Append(String.Format("{0:000}", recipe.Id));
            paramBuilder.Append(String.Format("{0:00}", recipe.Steps.Count));
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                paramBuilder.Append(String.Format("{0:00}", step.StepNumber));
                paramBuilder.Append(String.Format("{0:0}", (int)step.Function));
                paramBuilder.Append(String.Format("{0:000}", step.SpeedRPM));
                paramBuilder.Append(String.Format("{0:0}", (int)step.Direction));
                paramBuilder.Append(String.Format("{0:0000}", step.TargetXDeg));
                paramBuilder.Append(String.Format("{0:00}", step.Repeats));
                paramBuilder.Append(String.Format("{0:00000}", step.PauseMs));
            }

            return paramBuilder.ToString().PadRight(ParameterLength, PaddingChar);
        }
    }
}