using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input; // Potrebno za IRelayCommand
using LM01_UI; // Za Logger
using LM01_UI.ViewModels; // Za ViewModelBase

namespace LM01_UI.ViewModels
{
    public class UITestViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private readonly Action<object>? _navigateAction; // Akcija za navigacijo

        // Testne lastnosti za ComboBox in ListBox
        private ObservableCollection<string> _comboBoxItems = new() { "Item A", "Item B", "Item C" };
        private string? _selectedComboBoxItem;
        private ObservableCollection<string> _listBoxItems = new() { "List Item 1", "List Item 2", "List Item 3", "List Item 4" };
        private string? _selectedListBoxItem;

        // Lastnosti za DatePicker in TimePicker
        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        private TimeSpan _selectedTime = DateTimeOffset.Now.TimeOfDay;

        public UITestViewModel(Logger logger, Action<object> navigateAction)
        {
            _logger = logger;
            _navigateAction = navigateAction;

            NavigateBackCommand = new RelayCommand(NavigateBack);

            _logger.Inform(1, "UITestViewModel initialised.");
        }

        // Bindable Properties
        public ObservableCollection<string> ComboBoxItems => _comboBoxItems;
        public string? SelectedComboBoxItem
        {
            get => _selectedComboBoxItem;
            set => SetProperty(ref _selectedComboBoxItem, value);
        }

        public ObservableCollection<string> ListBoxItems => _listBoxItems;
        public string? SelectedListBoxItem
        {
            get => _selectedListBoxItem;
            set => SetProperty(ref _selectedListBoxItem, value);
        }

        public DateTimeOffset SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public TimeSpan SelectedTime
        {
            get => _selectedTime;
            set => SetProperty(ref _selectedTime, value);
        }

        // Commands
        public IRelayCommand NavigateBackCommand { get; }

        private void NavigateBack()
        {
            _navigateAction?.Invoke("Welcome"); // Navigacija nazaj na WelcomeView
        }
    }
}