using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Linq;

namespace LM01_UI.Views
{
    public partial class RecipeEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public RecipeEditorView()
        {
            InitializeComponent();

            var nameTextBox = this.FindControl<TextBox>("NameTextBox");
            var descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            var qwertyKeypad = this.FindControl<QwertyKeypad>("QwertyKeypad");

            if (nameTextBox != null) nameTextBox.GotFocus += OnTextBoxGotFocus;
            if (descriptionTextBox != null) descriptionTextBox.GotFocus += OnTextBoxGotFocus;

            if (qwertyKeypad != null)
            {
                qwertyKeypad.KeyPressed += OnQwertyKeyPressed;
            }
        }

        private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
        {
            _activeTextBox = sender as TextBox;
        }

        private void OnQwertyKeyPressed(string key)
        {
            if (_activeTextBox == null) return;
            if (key == "BACKSPACE")
            {
                if (!string.IsNullOrEmpty(_activeTextBox.Text))
                {
                    _activeTextBox.Text = _activeTextBox.Text.Substring(0, _activeTextBox.Text.Length - 1);
                }
            }
            else
            {
                _activeTextBox.Text += key;
            }
            _activeTextBox.CaretIndex = _activeTextBox.Text?.Length ?? 0;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}