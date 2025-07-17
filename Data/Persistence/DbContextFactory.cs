using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LM01_UI.Data.Persistence
{
    /// <summary>
    /// Ta razred uporablja samo Entity Framework orodje (npr. Add-Migration),
    /// da ve, kako ustvariti DbContext med razvojem.
    /// Ne uporablja se med normalnim delovanjem aplikacije.
    /// </summary>
    public class DbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Uporabimo enako pot do baze kot v App.axaml.cs
            optionsBuilder.UseSqlite("Data Source=lm01.db");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}