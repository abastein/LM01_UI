using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM01_UI.Models
{
    public class Recipe
    {
        public int Id { get; set; } // Program ID (npr. 001)

        public string Name { get; set; } = string.Empty; // Ime recepture (npr. "Planica")
        public string? Description { get; set; } // Opcijski opis

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigacijska lastnost: Receptura ima lahko veliko korakov
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    }
}
