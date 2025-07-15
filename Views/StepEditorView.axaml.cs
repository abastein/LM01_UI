using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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

            // Poiščemo vse kontrole, med katerimi se želimo premikati
            var functionComboBox = this.FindControl<ComboBox>("FunctionComboBox");
            var directionComboBox = this.FindControl<ComboBox>("DirectionComboBox");
            var speedRpmTextBox = this.FindControl<TextBox>("SpeedRpmTextBox");
            var targetXDegTextBox = this.FindControl<TextBox>("TargetXDegTextBox");
            var repeatsTextBox = this.FindControl<TextBox>("RepeatsTextBox");
            var pauseMsTextBox = this.FindControl<TextBox>("PauseMsTextBox");
            var keypad = this.FindControl<NumericKeypad>("Keypad");

            // Ustvarimo seznam kontrol v pravilnem vrstnem redu za "Next" funkcionalnost
            _focusableControls = new List<Control>();
            if (functionComboBox != null) _focusableControls.Add(functionComboBox);
            if (directionComboBox != null) _focusableControls.Add(directionComboBox);
            if (speedRpmTextBox != null) _focusableControls.Add(speedRpmTextBox);
            if (targetXDegTextBox != null) _focusableControls.Add(targetXDegTextBox);
            if (repeatsTextBox != null) _focusableControls.Add(repeatsTextBox);
            if (pauseMsTextBox != null) _focusableControls.Add(pauseMsTextBox);

            // Zberemo vse TextBoxe (razen tistega za št. koraka) in jim dodamo dogodek
            var editableTextBoxes = new[] { speedRpmTextBox, targetXDegTextBox, repeatsTextBox, pauseMsTextBox };
            foreach (var tb in editableTextBoxes)
            {
                if (tb != null)
                {
                    tb.GotFocus += (sender, e) => _activeTextBox = sender as TextBox;
                }
            }

            // Povežemo dogodke
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;
            if (functionComboBox != null) functionComboBox.SelectionChanged += OnFunctionComboBoxSelectionChanged;
            if (directionComboBox != null) directionComboBox.SelectionChanged += OnDirectionComboBoxSelectionChanged;

            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            this.AttachedToVisualTree -= OnAttachedToVisualTree;

            var functionComboBox = this.FindControl<ComboBox>("FunctionComboBox");
            if (functionComboBox != null)
            {
                // Uporabimo Dispatcher, da zagotovimo, da je UI pripravljen
                Dispatcher.UIThread.Post(() =>
                {
                    functionComboBox.Focus();
                    functionComboBox.IsDropDownOpen = true;
                }, DispatcherPriority.Loaded);
            }
        }

        private void OnFunctionComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext as StepEditorViewModel;
            if (viewModel?.CurrentStep.Function == FunctionType.Rotate)
            {
                var directionComboBox = this.FindControl<ComboBox>("DirectionComboBox");
                if (directionComboBox != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        directionComboBox.Focus();
                        directionComboBox.IsDropDownOpen = true;
                    }, DispatcherPriority.Input);
                }
            }
            else
            {
                FocusNext(sender as Control);
            }
        }

        private void OnDirectionComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            FocusNext(sender as Control);
        }

        private void OnKeypadPressed(string key)
        {
            if (key == "NEXT")
            {
                // Za premik naprej uporabimo trenutno aktivno vnosno polje
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
            if (currentIndex == -1)
            {
                // Če trenutne kontrole ni na seznamu (npr. je bil fokus na gumbu),
                // poskusimo najti naslednjo od zadnjega aktivnega TextBoxa.
                if (_activeTextBox != null)
                {
                    currentIndex = _focusableControls.IndexOf(_activeTextBox);
                }
                if (currentIndex == -1) return; // Če še vedno ne najdemo, prekinemo.
            }

            int nextIndex = currentIndex;
            int loopGuard = _focusableControls.Count + 1; // Varovalka proti neskončni zanki
            while (loopGuard > 0)
            {
                nextIndex = (nextIndex + 1) % _focusableControls.Count;
                if (_focusableControls[nextIndex].IsEnabled)
                {
                    _focusableControls[nextIndex].Focus();

                    // Če je naslednja kontrola ComboBox, jo tudi odpremo
                    if (_focusableControls[nextIndex] is ComboBox nextComboBox)
                    {
                        Dispatcher.UIThread.Post(() => nextComboBox.IsDropDownOpen = true, DispatcherPriority.Input);
                    }
                    return;
                }
                loopGuard--;
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}