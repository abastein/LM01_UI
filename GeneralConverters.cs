using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LM01_UI
{
    // Namesto BoolToBrushConverter.cs, uporabimo GeneralConverters.cs
    // v njej pa lahko zberemo več pretvornikov
    public class BoolToBrushConverter : IValueConverter
    {
        public IBrush TrueBrush { get; set; } = Brushes.Green;
        public IBrush FalseBrush { get; set; } = Brushes.Red;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // NOV PRETVORNIK
    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null; // Vrni true, če vrednost ni null
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}