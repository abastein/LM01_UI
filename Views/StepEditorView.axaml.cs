using Avalonia.Controls;
using Avalonia.Interactivity;
using LM01_UI.Enums;
using LM01_UI.ViewModels;
using System.Linq;

namespace LM01_UI.Views
{
    public partial class StepEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public StepEditorView()
        {
            InitializeComponent();

            // Poiščemo vsa vnosna polja in tipkovnico
            var speedRpmTextBox = this.FindControl<TextBox>("SpeedRpmTextBox");
            var targetXDegTextBox = this.FindControl<TextBox>("TargetXDegTextBox");
            var repeatsTextBox = this.FindControl<TextBox>("RepeatsTextBox");
            var pauseMsTextBox = this.FindControl<TextBox>("PauseMsTextBox");
            var keypad = this.FindControl<NumericKeypad>("Keypad");

            var textBoxes = new[] { speedRpmTextBox, targetXDegTextBox, repeatsTextBox, pauseMsTextBox };

            // Povežemo se na dogodek "GotFocus" za vsa polja
            foreach (var tb in textBoxes.Where(t => t != null))
            {
                tb!.GotFocus += (s, e) => _activeTextBox = s as TextBox;
            }

            // Povežemo se na dogodek "KeyPressed" na tipkovnici
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;
        }

        // Ta metoda se izvede, ko je na tipkovnici pritisnjena tipka
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

        // Ta metoda je potrebna za posodabljanje vidnosti polj
        private void OnFunctionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is StepEditorViewModel viewModel)
            {
                // Pustimo prazno, ker ViewModel že sam posodobi IsEnabled lastnosti.
                // Klic je potreben samo, da se XAML osveži.
            }
        }
    }
}