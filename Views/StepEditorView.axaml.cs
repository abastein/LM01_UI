using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading; // Potrebno za Dispatcher
using Avalonia.VisualTree; // Potrebno za VisualTreeAttachmentEventArgs
using LM01_UI.Enums;
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
                tb!.GotFocus += (s, ev) => _activeTextBox = s as TextBox;
            }
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;

            // POPRAVEK: Postavimo fokus na ComboBox in ga odpremo
            if (functionComboBox != null)
            {
                functionComboBox.Focus();
                // Akcijo za odprtje pošljemo v čakalno vrsto, da se izvede, ko bo UI pripravljen
                Dispatcher.UIThread.Post(() =>
                {
                    functionComboBox.IsDropDownOpen = true;
                }, DispatcherPriority.Loaded);
            }

            this.AttachedToVisualTree -= StepEditorView_AttachedToVisualTree;
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
                // Pustimo prazno, ker ViewModel že sam posodobi IsEnabled lastnosti
            }
        }
    }
}