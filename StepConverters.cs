using Avalonia.Data.Converters;
using System;
using System.Globalization;
using LM01_UI.Enums;

namespace LM01_UI
{
    public class DegreesToPulsesConverter : IValueConverter
    {
        private const double DegreesPerStep = 1.8;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var deg))
            {
                var pulses = deg / DegreesPerStep;
                return System.Convert.ChangeType(Math.Round(pulses), targetType, culture);
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var pulses))
            {
                var degrees = pulses * DegreesPerStep;
                return System.Convert.ChangeType(Math.Round(degrees), targetType, culture);
            }
            return 0;
        }
    }

    public class RpmToPulsesPerSecondConverter : IValueConverter
    {
        private const double PulsesPerRevolution = 200.0;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rpm))
            {
                var pps = rpm * PulsesPerRevolution / 60.0;
                return System.Convert.ChangeType(Math.Round(pps), targetType, culture);
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var pps))
            {
                var rpm = pps * 60.0 / PulsesPerRevolution;
                return System.Convert.ChangeType(Math.Round(rpm), targetType, culture);
            }
            return 0;
        }
    }

    public class DirectionToSymbolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DirectionType dir)
            {
                return dir == DirectionType.CW ? "+" : "-";
            }
            if (value is int intVal)
            {
                return intVal == 0 ? "+" : "-";
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Trim();
                if (str == "+") return DirectionType.CW;
                if (str == "-") return DirectionType.CCW;
            }
            return DirectionType.CW;
        }
    }
}
