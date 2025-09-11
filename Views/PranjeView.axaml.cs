using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class PranjeView : UserControl
    {
        public PranjeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}