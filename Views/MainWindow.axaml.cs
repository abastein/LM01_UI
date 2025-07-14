using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml; // Potrebno za AvaloniaXamlLoader

namespace LM01_UI.Views
{
    public partial class MainWindow : Window
    {

        // public Popup KeypadPopup => this.FindControl<Popup>("KeypadPopup")!;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}