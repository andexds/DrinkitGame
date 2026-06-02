using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using System;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class OrderServiceTests
    {
        private GameContent _content;
        private GameState _state;
        private InventoryService _inventory;
        private ReputationService _reputation;
        private OrderGenerator _generator;

        [SetUp]
        public void Setup()
        {
            var beans = ScriptableObject.CreateInstance<ProductDefinition>();
            beans.id = "beans"; beans.category = ProductCategory.Beans;

            var t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            t1.tierIndex = 1;

            var espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            espresso.id = "espresso"; espresso.requiredMachineTier = t1;
            espresso.fixedIngredients.Add(new IngredientAmount(beans, 1));

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.products.Add(beans);
            _content.recipes.Add(espresso);

            _state = new GameState();
            _state.unlockedRecipeIds.Add("espresso");

            _inventory = new InventoryService(_state);
            _inventory.Add("beans", 100);

            _reputation = new ReputationService(_state);
            _generator = new OrderGenerator(_state, _content, _inventory, new System.Random(1));
        }

        [Test]
        public void Tick_SpawnsOrder_AfterDelay_InFreeSlot()
        {
            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));
            Order spawned = null;
            service.OrderSpawned += o => spawned = o;

            // Тикаем 20 секунд по 1 сек — должен спавн произойти в этом окне (5-15 сек)
            for (int i = 0; i < 20 && spawned == null; i++)
                service.Tick(1f);

            Assert.IsNotNull(spawned, "За 20 сек заказ должен был появиться");
            Assert.AreEqual("espresso", spawned.recipe.id);
            Assert.AreEqual(0, spawned.slotIndex);
            Assert.AreEqual(OrderService.Patience, spawned.remainingPatience, 0.01f);
            Assert.AreSame(spawned, service.GetSlot(0));
        }

        [Test]
        public void Tick_FillsAllThreeSlots()
        {
            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));
            int spawnCount = 0;
            service.OrderSpawned += _ => spawnCount++;

            // 60 сек тика по 1 сек должно хватить на 3 заказа
            for (int i = 0; i < 60 && spawnCount < 3; i++)
                service.Tick(1f);

            Assert.AreEqual(3, spawnCount);
            for (int i = 0; i < 3; i++)
                Assert.IsNotNull(service.GetSlot(i), $"Слот {i} должен быть заполнен");
        }

        [Test]
        public void Tick_StopsSpawning_WhenAllSlotsFull()
        {
            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));
            for (int i = 0; i < 60; i++) service.Tick(1f);

            int extraSpawn = 0;
            service.OrderSpawned += _ => extraSpawn++;

            // Ещё минута — но слоты заняты, новых не должно быть
            for (int i = 0; i < 60; i++) service.Tick(1f);
            Assert.AreEqual(0, extraSpawn, "При полных слотах новых не должно появляться");
        }

        [Test]
        public void Tick_AbandonsOrder_AfterPatienceExpires()
        {
            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));

            // Захватываем ПЕРВЫЙ заспавненный заказ (внутри Tick(310f) после ухода клиента
            // может появиться новый заказ в тот же слот — нам нужен именно первый).
            Order firstSpawned = null;
            service.OrderSpawned += o => { if (firstSpawned == null) firstSpawned = o; };
            for (int i = 0; i < 20 && firstSpawned == null; i++) service.Tick(1f);
            Assert.IsNotNull(firstSpawned);

            Order abandoned = null;
            service.OrderAbandoned += o => abandoned = o;
            float repBefore = _reputation.Reputation;

            // Тикаем 300+ секунд большими шагами
            service.Tick(310f);

            Assert.IsNotNull(abandoned, "Клиент должен был уйти");
            Assert.AreSame(firstSpawned, abandoned, "Уйти должен именно первый клиент");
            Assert.AreEqual(repBefore - OrderService.ReputationLossOnAbandon,
                _reputation.Reputation, 0.001f, "Репутация должна упасть на 0.1");
            // ВАЖНО: не проверяем что слот пустой — за тот же Tick(310f) мог уже
            // спавниться новый заказ в освободившийся слот, это норм.
        }

        [Test]
        public void TakeFromSlot_ReturnsOrder_AndFreesSlot()
        {
            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));
            for (int i = 0; i < 20 && service.GetSlot(0) == null; i++) service.Tick(1f);
            Assert.IsNotNull(service.GetSlot(0));

            int removedSlot = -1;
            service.OrderRemoved += idx => removedSlot = idx;

            var taken = service.TakeFromSlot(0);
            Assert.IsNotNull(taken);
            Assert.IsNull(service.GetSlot(0));
            Assert.AreEqual(0, removedSlot);
        }

        [Test]
        public void Tick_DoesNotSpawn_WhenInventoryEmpty()
        {
            // Очищаем зерно
            _inventory.TryConsume("beans", 100);

            var service = new OrderService(_generator, _reputation, _state, new System.Random(1));
            int spawnCount = 0;
            service.OrderSpawned += _ => spawnCount++;

            // 60 сек тиков — ничего не должно появиться (Generator вернёт null)
            for (int i = 0; i < 60; i++) service.Tick(1f);

            Assert.AreEqual(0, spawnCount);
        }
    }
}