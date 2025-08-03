using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class DebugLogView : UserControl
    {
        public DebugLogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}