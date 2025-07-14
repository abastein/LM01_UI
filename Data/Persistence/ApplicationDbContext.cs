using Microsoft.EntityFrameworkCore;
using LM01_UI.Models; // Za dostop do Recipe in RecipeStep modelov

// Opomba: using LM01_UI.Enums; ni več potreben, če ga uporabljate samo v OnModelCreating,
// saj C# samodejno razreši tipe znotraj metod. Lahko ga pustite.

namespace LM01_UI.Data.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        // DbSeti za vaše tabele ostanejo nespremenjeni
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }

        // POPRAVEK: To je edini konstruktor, ki ga potrebujete.
        // Zagotavlja, da mora vsak, ki ustvari DbContext, posredovati konfiguracijske nastavitve.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // POPRAVEK: Odstranili smo prazen konstruktor "public ApplicationDbContext()".
        // S tem preprečimo, da bi kdo po nesreči ustvaril instanco brez nastavitev.

        // POPRAVEK: Odstranili smo celotno metodo "OnConfiguring".
        // S tem zagotovimo, da se ne bo nikoli uporabila trdo kodirana pot "Data Source=recipes.db",
        // ampak vedno tista, ki jo posredujemo preko konstruktorja.

        // Metoda OnModelCreating ostane nespremenjena, saj skrbi za strukturo modela.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Dobra praksa je klicati base metodo.

            // Konfiguracija relacije ena-proti-mnogim med Recipe in RecipeStep
            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.Steps)
                .WithOne(s => s.Recipe)
                .HasForeignKey(s => s.RecipeId);

            // Konfiguracija mapiranja enumov na integer v bazi podatkov
            modelBuilder.Entity<RecipeStep>()
                .Property(s => s.Function)
                .HasConversion<int>();

            modelBuilder.Entity<RecipeStep>()
                .Property(s => s.Direction)
                .HasConversion<int>();

            // Vaši morebitni "seed" podatki ostanejo tukaj, če jih potrebujete.
            /*
            modelBuilder.Entity<Recipe>().HasData(...);
            modelBuilder.Entity<RecipeStep>().HasData(...);
            */
        }
    }
}