using LM01_UI.Models;
using Microsoft.EntityFrameworkCore;

namespace LM01_UI.Data.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Recipe> Recipes { get; set; }

        // POPRAVEK: Ta vrstica mora biti prisotna.
        public DbSet<RecipeStep> Steps { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    }
}