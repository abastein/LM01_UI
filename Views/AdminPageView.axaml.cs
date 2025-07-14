using Avalonia.Controls;
using Avalonia.Markup.Xaml;

// Pravilen zapis za namespace.
// Pomišljaj '-' ni dovoljen v imenu namespace-a ali razreda v C#.
// Uporabite podčrtaj '_' namesto pomišljaja.
// Ker so to Views, jih damo v namespace LM01_UI.Views
namespace LM01_UI.Views
{
    public partial class AdminPageView : UserControl
    {
        public AdminPageView()
        {
            InitializeComponent(); // Klic metode InitializeComponent()
        }

        private void InitializeComponent()
        {
            // Ta vrstica naloži XAML definicijo za ta UserControl
            AvaloniaXamlLoader.Load(this);
        }
    }
}