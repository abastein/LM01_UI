using Avalonia;
using Avalonia.Controls;
using Avalonia.Input; // Potrebno za GotFocusEventArgs
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LM01_UI.ViewModels;
using System;
using System.Linq;

namespace LM01_UI.Views
{
    public partial class StepEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public StepEditorView()
        {
            InitializeComponent();
            this.AttachedToVisualTree += StepEditorView_AttachedToVisualTree;
        }

        private void StepEditorView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var speedRpmTextBox = this.FindControl<TextBox>("SpeedRpmTextBox");
            var targetXDegTextBox = this.FindControl<TextBox>("TargetXDegTextBox");
            var repeatsTextBox = this.FindControl<TextBox>("RepeatsTextBox");
            var pauseMsTextBox = this.FindControl<TextBox>("PauseMsTextBox");
            var keypad = this.FindControl<NumericKeypad>("Keypad");
            var functionComboBox = this.FindControl<ComboBox>("FunctionComboBox");

            var textBoxes = new[] { speedRpmTextBox, targetXDegTextBox, repeatsTextBox, pauseMsTextBox };
            foreach (var tb in textBoxes.Where(t => t != null))
            {
                // POPRAVEK: Vsa polja se sedaj vežejo na novo, pametnejšo metodo
                tb!.GotFocus += NumericTextBox_GotFocus;
            }
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;

            if (functionComboBox != null)
            {
                functionComboBox.Focus();
                Dispatcher.UIThread.Post(() =>
                {
                    functionComboBox.IsDropDownOpen = true;
                }, DispatcherPriority.Loaded);
            }

            this.AttachedToVisualTree -= StepEditorView_AttachedToVisualTree;
        }

        // POPRAVEK: Nova metoda, ki počisti polje, če je v njem "0"
        private void NumericTextBox_GotFocus(object? sender, GotFocusEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Najprej nastavimo aktivno polje za tipkovnico
                _activeTextBox = textBox;

                // Nato preverimo, ali naj počistimo vsebino
                if (textBox.Text == "0")
                {
                    textBox.Text = string.Empty;
                }
            }
        }

        private void OnKeypadPressed(string key)
        {
            if (_activeTextBox == null) return;
            if (key == "BACKSPACE")
            {
                if (!string.IsNullOrEmpty(_activeTextBox.Text))
                {
                    _activeTextBox.Text = _activeTextBox.Text[..^1];
                }
            }
            else
            {
                _activeTextBox.Text += key;
            }
            _activeTextBox.CaretIndex = _activeTextBox.Text?.Length ?? 0;
        }

        private void OnFunctionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is StepEditorViewModel viewModel)
            {
                // Klic je potreben samo, da se XAML osveži
            }
        }
    }
}