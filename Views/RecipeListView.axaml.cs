using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LM01_UI.Views
{
    public partial class RecipeListView : UserControl
    {
        public RecipeListView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

}