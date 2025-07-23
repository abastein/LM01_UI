using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace LM01_UI.Views
{
    public partial class RecipeEditorView : UserControl
    {
        private TextBox? _activeTextBox;

        public RecipeEditorView()
        {
            InitializeComponent();
            this.AttachedToVisualTree += RecipeEditorView_AttachedToVisualTree;
        }

        private void RecipeEditorView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // POPRAVEK: Namesto FindControl, dostopamo do kontrol neposredno.
            // Prevajalnik je samodejno ustvaril polja 'NameTextBox', 'DescriptionTextBox' in 'Keypad'
            // na podlagi x:Name oznak v XAML datoteki.

            // Preverimo, ali je prevajalnik pravilno ustvaril polja.
            if (this.NameTextBox == null) Debug.WriteLine("NAPAKA: Polje NameTextBox je null!");
            else this.NameTextBox.GotFocus += (s, ev) => _activeTextBox = s as TextBox;

            if (this.DescriptionTextBox == null) Debug.WriteLine("NAPAKA: Polje DescriptionTextBox je null!");
            else this.DescriptionTextBox.GotFocus += (s, ev) => _activeTextBox = s as TextBox;

            if (this.Keypad == null)
            {
                Debug.WriteLine("KRITIČNA NAPAKA: Polje Keypad je null!");
            }
            else
            {
                Debug.WriteLine("USPEH: Polje Keypad najdeno. Povezujem dogodek KeyPressed.");
                this.Keypad.KeyPressed += OnKeypadPressed;
            }

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