using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class OrderServicePersistenceTests
    {
        private GameContent _content;
        private GameState _state;
        private InventoryService _inventory;
        private ReputationService _reputation;
        private OrderGenerator _generator;
        private RecipeDefinition _espresso;
        private ProductDefinition _beans, _milkOat;

        [SetUp]
        public void Setup()
        {
            _beans = MakeProduct("beans", ProductCategory.Beans);
            _milkOat = MakeProduct("milk_oat", ProductCategory.Milk);

            var t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            t1.tierIndex = 1;

            _espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            _espresso.id = "espresso";
            _espresso.requiredMachineTier = t1;
            _espresso.fixedIngredients.Add(new IngredientAmount(_beans, 1));

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.products.AddRange(new[] { _beans, _milkOat });
            _content.recipes.Add(_espresso);

            _state = new GameState();
            _state.unlockedRecipeIds.Add("espresso");

            _inventory = new InventoryService(_state);
            _inventory.Add("beans", 100);

            _reputation = new ReputationService(_state);
            _generator = new OrderGenerator(_state, _content, _inventory, new System.Random(1));
        }

        private ProductDefinition MakeProduct(string id, ProductCategory cat)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.id = id; p.category = cat;
            return p;
        }

        [Test]
        public void SerializeRestore_RoundTripsOneOrder()
        {
            var s1 = new OrderService(_generator, _reputation, _state, new System.Random(1));
            for (int i = 0; i < 30 && s1.GetSlot(0) == null; i++) s1.Tick(1f);
            Assert.IsNotNull(s1.GetSlot(0));

            // Запомним детали
            var original = s1.GetSlot(0);
            float patience = original.remainingPatience;

            // Сохраняем в state
            s1.SerializeToState(_state);
            Assert.AreEqual(1, _state.persistedOrders.Count);

            // Восстанавливаем в новый сервис
            var s2 = new OrderService(_generator, _reputation, _state, new System.Random(1));
            s2.RestoreFromState(_state, _content);

            var restored = s2.GetSlot(0);
            Assert.IsNotNull(restored);
            Assert.AreEqual(original.recipe.id, restored.recipe.id);
            Assert.AreEqual(patience, restored.remainingPatience, 0.001f);
        }

        [Test]
        public void Restore_SkipsOrderWithUnknownRecipe()
        {
            _state.persistedOrders.Add(new PersistedOrder
            {
                recipeId = "unknown_drink",
                slotIndex = 0,
                remainingPatience = 100f
            });

            var s = new OrderService(_generator, _reputation, _state, new System.Random(1));
            s.RestoreFromState(_state, _content);
            Assert.IsNull(s.GetSlot(0));
        }

        [Test]
        public void SerializeRestore_PreservesModifiers()
        {
            _inventory.Add("milk_oat", 5);
            // Создадим вручную заказ с молоком
            var order = new Order
            {
                recipe = _espresso,
                milk = _milkOat,
                isToGo = true,
                remainingPatience = 250f,
                slotIndex = 1
            };
            _state.persistedOrders.Add(new PersistedOrder
            {
                recipeId = order.recipe.id,
                milkId = order.milk.id,
                isToGo = order.isToGo,
                remainingPatience = order.remainingPatience,
                slotIndex = order.slotIndex
            });

            var s = new OrderService(_generator, _reputation, _state, new System.Random(1));
            s.RestoreFromState(_state, _content);

            var restored = s.GetSlot(1);
            Assert.IsNotNull(restored);
            Assert.AreEqual("milk_oat", restored.milk.id);
            Assert.IsTrue(restored.isToGo);
            Assert.AreEqual(250f, restored.remainingPatience, 0.001f);
        }
    }
}