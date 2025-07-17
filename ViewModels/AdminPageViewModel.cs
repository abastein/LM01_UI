using CommunityToolkit.Mvvm.ComponentModel;
using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;

namespace LM01_UI.ViewModels
{
    public partial class AdminPageViewModel : ViewModelBase
    {
        // Ta ViewModel sedaj samo gosti RecipeListViewModel
        [ObservableProperty]
        private RecipeListViewModel _recipeList;

        public AdminPageViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext, Action<string> navigate)
        {
            // Ustvarimo RecipeListViewModel in mu posredujemo vse, kar potrebuje
            _recipeList = new RecipeListViewModel(dbContext, logger);
        }
    }
}