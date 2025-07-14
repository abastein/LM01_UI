using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using LM01_UI; // Za PlcTcpClient in Logger
using LM01_UI.Data.Persistence; // Dodano za ApplicationDbContext
// ... ostali usingi ...

namespace LM01_UI.ViewModels
{
    public class ParameterEditorViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext; // DODANO

        public ParameterEditorViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext) // DODANO dbContext
        {
            _plcClient = plcClient;
            _logger = logger;
            _dbContext = dbContext; // DODANO

            _logger.Inform(1, "ParameterEditorViewModel initialised.");
        }
        // ...
    }
}