using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LM01_UI.Views
{
    public partial class NumericKeypad : UserControl
    {
        // Dogodek, ki se sproži, ko je pritisnjena tipka. Pošlje vrednost tipke (npr. "7" ali "BACKSPACE").
        public event Action<string>? KeyPressed;

        public NumericKeypad()
        {
            InitializeComponent();
        }

        private void KeypadButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.Tag is string tag)
            {
                KeyPressed?.Invoke(tag);
            }
        }
    }
}