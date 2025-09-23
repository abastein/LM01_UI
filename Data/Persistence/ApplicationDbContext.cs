using LM01_UI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LM01_UI.Enums;
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


        public async Task EnsureSystemRecipesAsync()
        {
            await EnsureSystemRecipeAsync(
                RecipeSystemKey.NormalWash,
                CreateNormalWashTemplate());

            await EnsureSystemRecipeAsync(
                RecipeSystemKey.IntensiveWash,
                CreateIntensiveWashTemplate());
        }

        private async Task EnsureSystemRecipeAsync(RecipeSystemKey systemKey, Recipe template)
        {
            var recipe = await Recipes
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.SystemKey == systemKey);

            bool modified = false;

            if (recipe == null)
            {
                recipe = await Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == template.Id);

                if (recipe != null)
                {
                    if (recipe.SystemKey != systemKey)
                    {
                        recipe.SystemKey = systemKey;
                        modified = true;
                    }

                    if (string.IsNullOrWhiteSpace(recipe.Name) && !string.IsNullOrWhiteSpace(template.Name))
                    {
                        recipe.Name = template.Name;
                        modified = true;
                    }

                    if (recipe.Description is null && template.Description is not null)
                    {
                        recipe.Description = template.Description;
                        modified = true;
                    }
                }
                else
                {
                    template.SystemKey = systemKey;
                    Recipes.Add(template);
                    recipe = template;
                    modified = true;
                }
            }

            if (recipe.SystemKey != systemKey)
            {
                recipe.SystemKey = systemKey;
                modified = true;
            }

            if (recipe.Steps.Count == 0)
            {
                foreach (var step in CreateStepTemplates(systemKey))
                {
                    recipe.Steps.Add(step);
                }

                modified = true;
            }

            if (modified)
            {
                recipe.LastModifiedDate = DateTime.UtcNow;
                await SaveChangesAsync();
            }
        }

        private static Recipe CreateNormalWashTemplate()
        {
            var recipe = new Recipe
            {
                Id = 998,
                Name = "Normalno pranje",
                Description = "Privzeta receptura za normalno pranje",
                SystemKey = RecipeSystemKey.NormalWash,
                Steps = new List<RecipeStep>()
            };

            foreach (var step in CreateStepTemplates(RecipeSystemKey.NormalWash))
            {
                recipe.Steps.Add(step);
            }

            return recipe;
        }

        private static Recipe CreateIntensiveWashTemplate()
        {
            var recipe = new Recipe
            {
                Id = 997,
                Name = "Intenzivno pranje",
                Description = "Privzeta receptura za intenzivno pranje",
                SystemKey = RecipeSystemKey.IntensiveWash,
                Steps = new List<RecipeStep>()
            };

            foreach (var step in CreateStepTemplates(RecipeSystemKey.IntensiveWash))
            {
                recipe.Steps.Add(step);
            }

            return recipe;
        }

        private static IEnumerable<RecipeStep> CreateStepTemplates(RecipeSystemKey key)
        {
            if (key == RecipeSystemKey.NormalWash)
            {
                return new List<RecipeStep>
                {
                    new RecipeStep
                    {
                        StepNumber = 1,
                        Function = FunctionType.Rotate,
                        SpeedRPM = 20,
                        Direction = DirectionType.CW,
                        TargetXDeg = 360,
                        Repeats = 1,
                        PauseMs = 500
                    },
                    new RecipeStep
                    {
                        StepNumber = 2,
                        Function = FunctionType.Rotate,
                        SpeedRPM = 20,
                        Direction = DirectionType.CCW,
                        TargetXDeg = 360,
                        Repeats = 1,
                        PauseMs = 500
                    },
                    new RecipeStep
                    {
                        StepNumber = 3,
                        Function = FunctionType.Wait,
                        SpeedRPM = 0,
                        Direction = DirectionType.CW,
                        TargetXDeg = 0,
                        Repeats = 1,
                        PauseMs = 1000
                    }
                };
            }

            return new List<RecipeStep>
            {
                new RecipeStep
                {
                    StepNumber = 1,
                    Function = FunctionType.Rotate,
                    SpeedRPM = 25,
                    Direction = DirectionType.CW,
                    TargetXDeg = 540,
                    Repeats = 2,
                    PauseMs = 400
                },
                new RecipeStep
                {
                    StepNumber = 2,
                    Function = FunctionType.Rotate,
                    SpeedRPM = 25,
                    Direction = DirectionType.CCW,
                    TargetXDeg = 540,
                    Repeats = 2,
                    PauseMs = 400
                },
                new RecipeStep
                {
                    StepNumber = 3,
                    Function = FunctionType.Wait,
                    SpeedRPM = 0,
                    Direction = DirectionType.CW,
                    TargetXDeg = 0,
                    Repeats = 1,
                    PauseMs = 1500
                },
                new RecipeStep
                {
                    StepNumber = 4,
                    Function = FunctionType.Rotate,
                    SpeedRPM = 30,
                    Direction = DirectionType.CW,
                    TargetXDeg = 720,
                    Repeats = 1,
                    PauseMs = 600
                }
            };
        }
    }
}