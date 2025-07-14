using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views // PREVERI TO VRSTICO ZELO PREVIDNO!
{
    public partial class WelcomeView : UserControl // PREVERI TUDI TO VRSTICO!
    {
        public WelcomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}