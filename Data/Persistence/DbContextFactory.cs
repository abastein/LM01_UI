using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LM01_UI.Data.Persistence
{
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