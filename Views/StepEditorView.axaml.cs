using Avalonia.Controls;
using Avalonia.Interactivity;
using LM01_UI.Enums; // Uvozimo pravilen imenski prostor
using LM01_UI.ViewModels;
using System;

namespace LM01_UI.Views
{
    public partial class StepEditorView : UserControl
    {
        public StepEditorView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is StepEditorViewModel viewModel)
            {
                UpdateParameterVisibility(viewModel);
            }
        }

        private void OnFunctionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is StepEditorViewModel viewModel)
            {
                UpdateParameterVisibility(viewModel);
            }
        }

        private void UpdateParameterVisibility(StepEditorViewModel viewModel)
        {
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

            if (viewModel.SelectedFunction == null) return;

            // POPRAVEK: Uporabimo pravilno ime 'FunctionType'
            switch (viewModel.SelectedFunction.Function)
            {
                case FunctionType.Rotate:
                    if (speedPanel != null) speedPanel.IsVisible = true;
                    if (directionPanel != null) directionPanel.IsVisible = true;
                    if (targetPanel != null) targetPanel.IsVisible = true;
                    break;
                case FunctionType.Wait:
                    if (pausePanel != null) pausePanel.IsVisible = true;
                    break;
                case FunctionType.Repeat:
                    if (repeatsPanel != null) repeatsPanel.IsVisible = true;
                    break;
            }
        }
    }
}