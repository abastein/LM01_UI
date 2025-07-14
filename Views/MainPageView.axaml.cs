using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views // Uporabite podčrtaj, ne pomišljaj
{

    public partial class MainPageView : UserControl
    {
        public MainPageView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
