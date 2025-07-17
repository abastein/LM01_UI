using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LM01_UI.Models
{
    // POPRAVEK: Definiciji sta sedaj v isti datoteki, da preprečimo napake.
    public enum EStepFunction
    {
        Wait = 0,
        Rotate = 1,
        Output = 2,
        Repeat = 3
    }

    public enum EDirection
    {
        CW = 1,  // Clockwise
        CCW = 2, // Counter-Clockwise
    }

    public partial class RecipeStep : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        [Required]
        public int StepNumber { get; set; }

        public EStepFunction Function { get; set; }
        public int SpeedRPM { get; set; }
        public EDirection Direction { get; set; }
        public int TargetXDeg { get; set; }
        public int Repeats { get; set; }
        public int PauseMs { get; set; }

        [ObservableProperty]
        private bool _isActive;
    }
}