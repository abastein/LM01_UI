using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views // Uporabite podčrtaj, ne pomišljaj
{

    public partial class ParameterEditorView : UserControl
    {
        public ParameterEditorView()
        {
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}