using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LM01_UI.Views
{
    public partial class RecipeEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public RecipeEditorView()
        {
            InitializeComponent();

            // Poiščemo vnosna polja in tipkovnico po njihovih imenih
            var nameTextBox = this.FindControl<TextBox>("NameTextBox");
            var descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            var keypad = this.FindControl<QwertyKeypad>("Keypad");

            // Povežemo se na dogodek "GotFocus", da vemo, katero polje je aktivno
            if (nameTextBox != null) nameTextBox.GotFocus += (s, e) => _activeTextBox = s as TextBox;
            if (descriptionTextBox != null) descriptionTextBox.GotFocus += (s, e) => _activeTextBox = s as TextBox;

            // Povežemo se na dogodek "KeyPressed" na tipkovnici
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;
        }

        // Ta metoda se izvede, ko je na tipkovnici pritisnjena tipka
        private void OnKeypadPressed(string key)
        {
            // Če nobeno polje ni aktivno, ne naredimo nič
            if (_activeTextBox == null) return;

            if (key == "BACKSPACE")
            {
                if (!string.IsNullOrEmpty(_activeTextBox.Text))
                {
                    // Izbrišemo zadnji znak
                    _activeTextBox.Text = _activeTextBox.Text[..^1];
                }
            }
            else
            {
                // Dodamo nov znak
                _activeTextBox.Text += key;
            }
            // Premaknemo kurzor na konec besedila
            _activeTextBox.CaretIndex = _activeTextBox.Text?.Length ?? 0;
        }
    }
}