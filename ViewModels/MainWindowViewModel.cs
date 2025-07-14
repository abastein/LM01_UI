using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence; // Za ApplicationDbContext
using LM01_UI.ViewModels;
using System;

namespace LM01_UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext;

        private readonly WelcomeViewModel _welcomeViewModel;
        private readonly MainPageViewModel _mainPageViewModel;
        private readonly AdminPageViewModel _adminPageViewModel;
        private readonly UITestViewModel _uiTestViewModel;

        private object _currentPageViewModel;

        // POPRAVEK: Konstruktor sedaj sprejme DbContext kot parameter
        public MainWindowViewModel(ApplicationDbContext dbContext)
        {
            // ODSTRANJENO: Inicializacija DbContext-a je premaknjena v App.axaml.cs
            // var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // optionsBuilder.UseSqlite("Data Source=recipes.db");
            // _dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // POPRAVEK: DbContext je vbrizgan od zunaj
            _dbContext = dbContext;

            // ODSTRANJENO: Klic za migracije je premaknjen v App.axaml.cs,
            // saj je to del logike ob zagonu aplikacije, ne pa ViewModela.
            // _dbContext.Database.Migrate(); 

            // Ta del ostane enak, saj sedaj posredujemo pravilno konfiguriran DbContext
            _plcClient = new PlcTcpClient();
            _logger = new Logger();

            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _logger);
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate);
            _uiTestViewModel = new UITestViewModel(_logger, Navigate);

            _currentPageViewModel = _welcomeViewModel;

            ExitApplicationCommand = new RelayCommand(ExitApplication);
            NavigateToUITestCommand = new RelayCommand(() => CurrentPageViewModel = _uiTestViewModel);


            _logger.Inform(1, "MainWindowViewModel initialised.");
        }

        public object CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set => SetProperty(ref _currentPageViewModel, value);
        }

        public CommunityToolkit.Mvvm.Input.IRelayCommand ExitApplicationCommand { get; }
        public CommunityToolkit.Mvvm.Input.IRelayCommand NavigateToUITestCommand { get; }

        private void Navigate(object target)
        {
            if (target is "Run")
            {
                // POPRAVEK: Ta vrstica sproži osveževanje podatkov na "Run" pogledu
                // preden se ta dejansko prikaže.
                _mainPageViewModel.LoadRecipesCommand.Execute(null);

                CurrentPageViewModel = _mainPageViewModel;
            }
            else if (target is "Admin")
            {
                // V prihodnosti lahko enako logiko dodate tudi za Admin stran,
                // če boste potrebovali sprotno osveževanje tudi tam.
                CurrentPageViewModel = _adminPageViewModel;
            }
            else if (target is "Welcome")
            {
                CurrentPageViewModel = _welcomeViewModel;
            }
        }

        private void ExitApplication()
        {
            // ODSTRANJENO: Ročno sproščanje DbContexta ni več potrebno,
            // saj njegovo življenjsko dobo upravlja App.axaml.cs
            // _dbContext?.Dispose();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}