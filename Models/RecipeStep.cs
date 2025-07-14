// V Models/RecipeStep.cs
using LM01_UI.Enums;
using LM01_UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM01_UI.Models
{
    public class RecipeStep : ViewModelBase
    {
        private int _id;
        public int Id { get => _id; set => SetProperty(ref _id, value); }

        private int _recipeId;
        public int RecipeId { get => _recipeId; set => SetProperty(ref _recipeId, value); }

        private Recipe? _recipe;
        public Recipe? Recipe { get => _recipe; set => SetProperty(ref _recipe, value); }

        private int _stepNumber;
        public int StepNumber { get => _stepNumber; set => SetProperty(ref _stepNumber, value); }

        private FunctionType _function;
        public FunctionType Function { get => _function; set => SetProperty(ref _function, value); }

        private int? _speedRPM;
        public int? SpeedRPM { get => _speedRPM; set => SetProperty(ref _speedRPM, value); }

        private DirectionType _direction;
        public DirectionType Direction { get => _direction; set => SetProperty(ref _direction, value); }

        private int? _targetXDeg;
        public int? TargetXDeg { get => _targetXDeg; set => SetProperty(ref _targetXDeg, value); }

        private int? _repeats;
        public int? Repeats { get => _repeats; set => SetProperty(ref _repeats, value); }

        private int? _pauseMs;
        public int? PauseMs { get => _pauseMs; set => SetProperty(ref _pauseMs, value); }
    }
}
