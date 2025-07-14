using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading; // <-- DODAJTE TA USING
using Avalonia.VisualTree;
using LM01_UI.Enums;
using LM01_UI.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace LM01_UI.Views
{
    public partial class StepEditorView : UserControl
    {
        private TextBox? _activeTextBox;
        private readonly List<Control> _focusableControls;

        public StepEditorView()
        {
            InitializeComponent();

            var functionComboBox = this.FindControl<ComboBox>("FunctionComboBox");
            var directionComboBox = this.FindControl<ComboBox>("DirectionComboBox");
            var speedRpmTextBox = this.FindControl<TextBox>("SpeedRpmTextBox");
            var targetXDegTextBox = this.FindControl<TextBox>("TargetXDegTextBox");
            var repeatsTextBox = this.FindControl<TextBox>("RepeatsTextBox");
            var pauseMsTextBox = this.FindControl<TextBox>("PauseMsTextBox");
            var keypad = this.FindControl<NumericKeypad>("Keypad");

            _focusableControls = new List<Control>();
            if (functionComboBox != null) _focusableControls.Add(functionComboBox);
            if (directionComboBox != null) _focusableControls.Add(directionComboBox);
            if (speedRpmTextBox != null) _focusableControls.Add(speedRpmTextBox);
            if (targetXDegTextBox != null) _focusableControls.Add(targetXDegTextBox);
            if (repeatsTextBox != null) _focusableControls.Add(repeatsTextBox);
            if (pauseMsTextBox != null) _focusableControls.Add(pauseMsTextBox);

            var textBoxes = _focusableControls.OfType<TextBox>().ToList();
            textBoxes.Add(this.FindControl<TextBox>("StepNumberTextBox")); // Tudi tega moramo obravnavati

            foreach (var tb in textBoxes)
            {
                if (tb != null)
                {
                    tb.GotFocus += (sender, e) => _activeTextBox = sender as TextBox;
                }
            }

            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;
            if (functionComboBox != null) functionComboBox.SelectionChanged += FunctionComboBox_SelectionChanged;
            if (directionComboBox != null) directionComboBox.SelectionChanged += DirectionComboBox_SelectionChanged;

            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            this.AttachedToVisualTree -= OnAttachedToVisualTree;

            var functionComboBox = this.FindControl<ComboBox>("FunctionComboBox");
            if (functionComboBox != null)
            {
                functionComboBox.Focus();

                // POPRAVEK: Akcijo za odprtje pošljemo v čakalno vrsto, da se izvede, ko bo UI pripravljen.
                Dispatcher.UIThread.Post(() =>
                {
                    functionComboBox.IsDropDownOpen = true;
                }, DispatcherPriority.Loaded); // Prioriteta 'Loaded' je idealna za to.
            }
        }

        private void FunctionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext as StepEditorViewModel;
            if (viewModel?.CurrentStep.Function == FunctionType.Rotate)
            {
                var directionComboBox = this.FindControl<ComboBox>("DirectionComboBox");
                if (directionComboBox != null)
                {
                    directionComboBox.Focus();
                    Dispatcher.UIThread.Post(() => directionComboBox.IsDropDownOpen = true, DispatcherPriority.Input);
                }
            }
            else
            {
                FocusNext(sender as Control);
            }
        }

        private void DirectionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            FocusNext(sender as Control);
        }

        private void OnKeypadPressed(string key)
        {
            if (key == "NEXT")
            {
                FocusNext(_activeTextBox);
                return;
            }

            if (_activeTextBox == null) return;

            if (key == "BACKSPACE")
            {
                if (!string.IsNullOrEmpty(_activeTextBox.Text)) { _activeTextBox.Text = _activeTextBox.Text[..^1]; }
            }
            else
            {
                if (key == "." && _activeTextBox.Text?.Contains('.') == true) return;
                _activeTextBox.Text += key;
            }
            _activeTextBox.CaretIndex = _activeTextBox.Text?.Length ?? 0;
        }

        private void FocusNext(Control? currentControl)
        {
            if (currentControl == null) return;
            int currentIndex = _focusableControls.IndexOf(currentControl);
            if (currentIndex == -1) return;

            int nextIndex = currentIndex;
            int loopGuard = _focusableControls.Count;
            while (loopGuard > 0)
            {
                nextIndex = (nextIndex + 1) % _focusableControls.Count;
                if (_focusableControls[nextIndex].IsEnabled)
                {
                    _focusableControls[nextIndex].Focus();
                    return;
                }
                loopGuard--;
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}