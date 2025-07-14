using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class UITestView : UserControl
    {
        public UITestView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}