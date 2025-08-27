using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class ManualModeView : UserControl
    {
        public ManualModeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}