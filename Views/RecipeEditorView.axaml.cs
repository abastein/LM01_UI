using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree; // Potrebno za VisualTreeAttachmentEventArgs

namespace LM01_UI.Views
{
    public partial class RecipeEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public RecipeEditorView()
        {
            InitializeComponent();

            // Povežemo se na dogodek, ki se zgodi, ko je kontrola pripravljena
            this.AttachedToVisualTree += RecipeEditorView_AttachedToVisualTree;
        }

        private void RecipeEditorView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // Ko je kontrola pripravljena, izvedemo povezovanje
            var nameTextBox = this.FindControl<TextBox>("NameTextBox");
            var descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            var keypad = this.FindControl<QwertyKeypad>("Keypad");

            if (nameTextBox != null) nameTextBox.GotFocus += (s, ev) => _activeTextBox = s as TextBox;
            if (descriptionTextBox != null) descriptionTextBox.GotFocus += (s, ev) => _activeTextBox = s as TextBox;
            if (keypad != null) keypad.KeyPressed += OnKeypadPressed;

            // Odjavimo se od dogodka, da se ne izvede večkrat
            this.AttachedToVisualTree -= RecipeEditorView_AttachedToVisualTree;
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
    }
}