using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class QwertyKeypad : UserControl
    {
        public event Action<string>? KeyPressed;

        public QwertyKeypad()
        {
            InitializeComponent();
        }

        // POPRAVEK: Dodana manjkajoča metoda
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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