using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class OrderGeneratorTests
    {
        private GameContent _content;
        private GameState _state;
        private InventoryService _inventory;
        private ProductDefinition _beans, _milkCow, _milkOat, _cream, _syrupVanilla, _cinnamon, _cupTakeaway;
        private RecipeDefinition _espresso, _americano, _cappuccino;
        private MachineTierDefinition _t1, _t2;

        [SetUp]
        public void Setup()
        {
            // Products
            _beans = MakeProduct("beans", ProductCategory.Beans);
            _milkCow = MakeProduct("milk_cow", ProductCategory.Milk);
            _milkOat = MakeProduct("milk_oat", ProductCategory.Milk);
            _cream = MakeProduct("cream", ProductCategory.Cream);
            _syrupVanilla = MakeProduct("syrup_vanilla", ProductCategory.Syrup);
            _cinnamon = MakeProduct("topping_cinnamon", ProductCategory.Topping);
            _cupTakeaway = MakeProduct("cup_takeaway", ProductCategory.Cup);

            // Machines
            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;
            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;

            // Recipes
            _espresso = MakeRecipe("espresso", _t1, beans: true);
            _espresso.canHaveSyrup = true;
            _espresso.compatibleToppings.Add(_cinnamon);

            _americano = MakeRecipe("americano", _t1, beans: true);
            _americano.canHaveSyrup = true;

            _cappuccino = MakeRecipe("cappuccino", _t2, beans: true);
            _cappuccino.needsMilk = true;
            _cappuccino.canHaveSyrup = true;
            _cappuccino.compatibleToppings.Add(_cinnamon);

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.products.AddRange(new[] { _beans, _milkCow, _milkOat, _cream, _syrupVanilla, _cinnamon, _cupTakeaway });
            _content.recipes.AddRange(new[] { _espresso, _americano, _cappuccino });

            _state = new GameState();
            _inventory = new InventoryService(_state);
        }

        private ProductDefinition MakeProduct(string id, ProductCategory cat)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.id = id;
            p.category = cat;
            return p;
        }

        private RecipeDefinition MakeRecipe(string id, MachineTierDefinition tier, bool beans)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.id = id;
            r.requiredMachineTier = tier;
            r.canBeToGo = true;
            if (beans)
                r.fixedIngredients.Add(new IngredientAmount(_beans, 1));
            return r;
        }

        [Test]
        public void Generate_ReturnsNull_WhenNoRecipesUnlocked()
        {
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(1));
            Assert.IsNull(gen.Generate(0));
        }

        [Test]
        public void Generate_ReturnsNull_WhenNoBeansInStock()
        {
            _state.unlockedRecipeIds.Add("espresso");
            // Никаких ингредиентов в инвентаре
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(1));
            Assert.IsNull(gen.Generate(0));
        }

        [Test]
        public void Generate_ReturnsEspresso_WhenOnlyEspressoUnlocked_BeansAvailable()
        {
            _state.unlockedRecipeIds.Add("espresso");
            _inventory.Add("beans", 10);
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(1));
            var order = gen.Generate(0);
            Assert.IsNotNull(order);
            Assert.AreEqual("espresso", order.recipe.id);
            Assert.IsNull(order.milk);
            Assert.AreEqual(0, order.slotIndex);
        }

        [Test]
        public void Generate_Cappuccino_PicksMilkFromInStock()
        {
            _state.unlockedRecipeIds.AddRange(new[] { "espresso", "cappuccino" });
            _inventory.Add("beans", 10);
            _inventory.Add("milk_oat", 5);
            // milk_cow НЕТ — генератор должен выбрать только овсяное
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(42));

            // 5 попыток — если хотя бы раз попало на капучино, молоко должно быть овсяным
            bool foundCappuccino = false;
            for (int i = 0; i < 20; i++)
            {
                var order = gen.Generate(0);
                if (order != null && order.recipe.id == "cappuccino")
                {
                    foundCappuccino = true;
                    Assert.AreEqual("milk_oat", order.milk.id, "Должно быть овсяное (других в стоке нет)");
                }
            }
            Assert.IsTrue(foundCappuccino, "За 20 попыток ни разу не выпало капучино");
        }

        [Test]
        public void Generate_DoesNotSpawnCappuccino_WhenNoMilk()
        {
            _state.unlockedRecipeIds.AddRange(new[] { "espresso", "cappuccino" });
            _inventory.Add("beans", 10);
            // Молока нет вообще
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(42));

            for (int i = 0; i < 30; i++)
            {
                var order = gen.Generate(0);
                Assert.IsNotNull(order, "Эспрессо должен быть доступен");
                Assert.AreNotEqual("cappuccino", order.recipe.id, "Капучино без молока — недопустимо");
            }
        }

        [Test]
        public void Generate_WeightsFavorNewestRecipe()
        {
            _state.unlockedRecipeIds.AddRange(new[] { "espresso", "americano" });
            _inventory.Add("beans", 100);
            var gen = new OrderGenerator(_state, _content, _inventory, new System.Random(7));

            int americanoCount = 0, espressoCount = 0;
            for (int i = 0; i < 200; i++)
            {
                var order = gen.Generate(0);
                if (order.recipe.id == "americano") americanoCount++;
                else espressoCount++;
            }

            // Американо открыто последним → вес 4 vs 1. Ожидаем ~80% / 20%.
            // С шумом — americano должен сильно лидировать.
            Assert.Greater(americanoCount, espressoCount * 1.5,
                $"Ожидали что американо заметно лидирует (4:2), фактически americano={americanoCount}, espresso={espressoCount}");
        }
    }
}