using CommunityToolkit.Mvvm.ComponentModel;
using LM01_UI.Enums; // Uvozimo pravilen imenski prostor
using System.ComponentModel.DataAnnotations;

namespace LM01_UI.Models
{
    public partial class RecipeStep : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        [Required]
        public int StepNumber { get; set; }

        // POPRAVEK: Uporaba pravilnih imen tipov iz vašega projekta
        public FunctionType Function { get; set; }
        public int SpeedRPM { get; set; }
        public DirectionType Direction { get; set; }
        public int TargetXDeg { get; set; }
        public int Repeats { get; set; }
        public int PauseMs { get; set; }

        [ObservableProperty]
        private bool _isActive;
    }
}