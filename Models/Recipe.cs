using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;
using LM01_UI.Enums;

namespace LM01_UI.Models
{
    public partial class Recipe : ObservableObject
    {
        public int Id { get; set; } // Program ID (npr. 001)

        public string Name { get; set; } = string.Empty; // Ime recepture (npr. "Planica")
        public string? Description { get; set; } // Opcijski opis

        public RecipeSystemKey? SystemKey { get; set; }


        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigacijska lastnost: Receptura ima lahko veliko korakov
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();

        [NotMapped]
        [ObservableProperty]
        private bool _isActive;

        [NotMapped]
        public bool IsSystem => SystemKey.HasValue;
    }
}
