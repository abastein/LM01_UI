using Avalonia.Controls;
using Avalonia.Interactivity;
using LM01_UI.Models; // POPRAVEK: Dodana using direktiva
using LM01_UI.ViewModels;

namespace LM01_UI.Views
{
    // POPRAVEK: Izbrisana je bila celotna definicija za "public enum FunctionType", ker ni več potrebna.

    public partial class StepEditorView : UserControl
    {
        public StepEditorView()
        {
            InitializeComponent();
        }

        private void OnFunctionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is StepEditorViewModel viewModel && sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is FunctionViewModel selectedFunction)
                {
                    viewModel.SelectedFunction = selectedFunction;
                    UpdateParameterVisibility(viewModel);
                }
            }
        }

        private void UpdateParameterVisibility(StepEditorViewModel viewModel)
        {
            // Prikrijemo vse parametre na začetku
            var speedPanel = this.FindControl<StackPanel>("SpeedPanel");
            var directionPanel = this.FindControl<StackPanel>("DirectionPanel");
            var targetPanel = this.FindControl<StackPanel>("TargetPanel");
            var repeatsPanel = this.FindControl<StackPanel>("RepeatsPanel");
            var pausePanel = this.FindControl<StackPanel>("PausePanel");

            if (speedPanel != null) speedPanel.IsVisible = false;
            if (directionPanel != null) directionPanel.IsVisible = false;
            if (targetPanel != null) targetPanel.IsVisible = false;
            if (repeatsPanel != null) repeatsPanel.IsVisible = false;
            if (pausePanel != null) pausePanel.IsVisible = false;

            // Prikažemo samo relevantne parametre glede na izbrano funkcijo
            // POPRAVEK: Primerjava sedaj uporablja pravilen Enum 'EStepFunction'
            if (viewModel.SelectedFunction?.Function == EStepFunction.Rotate)
            {
                if (speedPanel != null) speedPanel.IsVisible = true;
                if (directionPanel != null) directionPanel.IsVisible = true;
                if (targetPanel != null) targetPanel.IsVisible = true;
            }
            else if (viewModel.SelectedFunction?.Function == EStepFunction.Wait)
            {
                if (pausePanel != null) pausePanel.IsVisible = true;
            }
            else if (viewModel.SelectedFunction?.Function == EStepFunction.Repeat)
            {
                if (repeatsPanel != null) repeatsPanel.IsVisible = true;
            }
        }
    }
}