using DrinkitGame.Data;
using NUnit.Framework;
using UnityEditor;

namespace DrinkitGame.Tests.EditMode
{
    public class GameContentIntegrityTests
    {
        private GameContent _content;

        [SetUp]
        public void LoadGameContent()
        {
            var guids = AssetDatabase.FindAssets("t:GameContent");
            Assert.AreEqual(1, guids.Length,
                "Должен быть ровно один GameContent ассет в проекте.");
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _content = AssetDatabase.LoadAssetAtPath<GameContent>(path);
            Assert.IsNotNull(_content, "GameContent не загрузился.");
        }

        [Test]
        public void Has15Products()
        {
            Assert.AreEqual(15, _content.products.Count);
        }

        [Test]
        public void Has3MachineTiers()
        {
            Assert.AreEqual(3, _content.machineTiers.Count);
        }

        [Test]
        public void Has8Recipes()
        {
            Assert.AreEqual(8, _content.recipes.Count);
        }

        [Test]
        public void Has9WheelSectors()
        {
            Assert.AreEqual(9, _content.wheelSectors.Count);
        }

        [Test]
        public void WheelSectorProbabilities_SumToOneHundred()
        {
            int total = 0;
            foreach (var sector in _content.wheelSectors)
            {
                Assert.IsNotNull(sector, "Сектор в списке = null.");
                total += sector.probabilityPercent;
            }
            Assert.AreEqual(100, total,
                "Сумма probabilityPercent всех секторов колеса должна быть 100.");
        }

        [Test]
        public void AllProductReferences_NotNull()
        {
            foreach (var product in _content.products)
            {
                Assert.IsNotNull(product, "Один из продуктов = null.");
                Assert.IsFalse(string.IsNullOrEmpty(product.id),
                    $"У продукта '{product.name}' пустой id.");
            }
        }

        [Test]
        public void AllRecipes_HaveRequiredMachineTier()
        {
            foreach (var recipe in _content.recipes)
            {
                Assert.IsNotNull(recipe, "Рецепт = null.");
                Assert.IsNotNull(recipe.requiredMachineTier,
                    $"У рецепта '{recipe.id}' не указан requiredMachineTier.");
            }
        }

        [Test]
        public void AllRecipes_FixedIngredientsValid()
        {
            foreach (var recipe in _content.recipes)
            {
                foreach (var ing in recipe.fixedIngredients)
                {
                    Assert.IsNotNull(ing.product,
                        $"У рецепта '{recipe.id}' в fixedIngredients продукт = null.");
                    Assert.Greater(ing.amount, 0,
                        $"У рецепта '{recipe.id}' amount должен быть > 0.");
                }
            }
        }

        [Test]
        public void StarterRecipe_IsInRecipesList()
        {
            Assert.IsNotNull(_content.starterRecipe);
            Assert.Contains(_content.starterRecipe, _content.recipes);
        }

        [Test]
        public void StarterMachineTier_IsT1()
        {
            Assert.IsNotNull(_content.starterMachineTier);
            Assert.AreEqual(1, _content.starterMachineTier.tierIndex);
        }
    }
}