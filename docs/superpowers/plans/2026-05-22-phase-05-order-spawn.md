# Phase 5 — Order Spawn Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Сделать так, чтобы в 3 слотах главного экрана сами появлялись заказы (рандомный рецепт из открытых + рандомные модификаторы из доступных по складу). У каждого заказа — таймер терпения 300 сек. По истечении — клиент уходит (-0.1 репутации). Клик по слоту — пока что просто лог в Console (`Order tapped: ...`). Полный цикл готовки — Phase 6.

**Architecture:**
- `Order` — POCO с ссылками на `RecipeDefinition` + `ProductDefinition` модификаторов + `remainingPatience` + `slotIndex`.
- `OrderGenerator` — pure C# класс. На входе `GameState`/`GameContent`/`InventoryService`, на выходе `Order` или `null`. Веса: последний открытый рецепт = 4, предпоследний = 2, остальные = 1.
- `OrderService` — pure C# класс. Хранит массив из 3 слотов. Метод `Tick(dt)` обновляет терпение и спавнит новые заказы (с задержкой 5–15 сек когда слот свободен).
- `OrderServiceTicker` — MonoBehaviour-обёртка, вызывает `Tick(Time.deltaTime)` каждый кадр.
- `OrderSlotView` — UI компонент на слоте, рендерит данные текущего заказа (название напитка + строка модификаторов + таймер); Button делает Click.
- `OrderSlotsController` — координирует 3 view'хи и подписывается на события `OrderService`.

**Tech Stack:** C# 9 · Unity 2022.3 · uGUI · TMPro

**Конец фазы:** Запустил Play → подождал 5–15 сек → в одном из трёх слотов появилась карточка вида "Эспрессо / Без сиропа / Тут / 4:55". Через ещё 5–15 сек — следующая. Каждую секунду таймер уменьшается. Через 5 минут (можно подкрутить на тест) — клиент ушёл, репутация упала. Тап по слоту — лог в Console.

---

## Task 1: `Order` POCO + `OrderState` enum

**Files:**
- Create: `Assets/Scripts/Core/Order.cs`

- [ ] **Step 1: Создать `Order.cs`**

В `Assets/Scripts/Core/`:

```csharp
using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Одна единица заказа клиента: рецепт + конкретные модификаторы + таймер терпения.
    /// Создаётся OrderGenerator'ом, лежит в OrderService слоте, отображается OrderSlotView.
    [Serializable]
    public class Order
    {
        public string id;                          // GUID для отладки/логов
        public RecipeDefinition recipe;            // что готовим
        public ProductDefinition milk;             // null если рецепт не требует молока
        public ProductDefinition cream;            // null если не раф
        public ProductDefinition syrup;            // null если нет сиропа
        public ProductDefinition topping;          // null если нет топпинга
        public bool isToGo;                        // тут (false) или с собой (true)
        public float remainingPatience;            // сек, тикает вниз; 300 при спавне
        public int slotIndex;                      // 0, 1 или 2

        public Order()
        {
            id = Guid.NewGuid().ToString();
        }
    }

    /// Состояние слота заказа.
    public enum OrderState
    {
        Empty,
        Waiting,      // ждёт игрока
        InProgress,   // игрок начал готовить (Phase 6)
    }
}
```

- [ ] **Step 2: Дождаться компиляции (Console чистая)**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/Order.cs Assets/Scripts/Core/Order.cs.meta && git commit -m "feat(core): Order POCO with recipe + modifiers + patience"
```

---

## Task 2: `OrderGenerator` + тесты

**Files:**
- Create: `Assets/Scripts/Core/OrderGenerator.cs`
- Create: `Assets/Tests/EditMode/OrderGeneratorTests.cs`

Pure C# класс. Принимает зависимости через конструктор. Использует переданный `System.Random` для детерминированности в тестах.

- [ ] **Step 1: Создать `OrderGenerator.cs`**

```csharp
using System;
using System.Collections.Generic;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Чистая логика генерации одного заказа: выбор рецепта по весам + модификаторы из доступных по складу.
    public class OrderGenerator
    {
        // Шансы накатить модификатор (если применим)
        private const double SyrupChance = 0.4;
        private const double ToppingChance = 0.3;
        private const double ToGoChance = 0.5;

        // Веса для приоритета "новых" рецептов в выдаче
        private const int WeightNewest = 4;
        private const int WeightSecondNewest = 2;
        private const int WeightOther = 1;

        // Сколько раз повторять попытку, если выбранный рецепт нельзя выполнить (нет ингредиентов)
        private const int MaxRetries = 12;

        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly InventoryService _inventory;
        private readonly Random _rng;

        public OrderGenerator(
            GameState state,
            GameContent content,
            InventoryService inventory,
            Random rng = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _rng = rng ?? new System.Random();
        }

        /// Сгенерировать заказ для указанного слота. Возвращает null если ничего нельзя приготовить
        /// (нет ингредиентов для всех открытых рецептов).
        public Order Generate(int slotIndex)
        {
            var unlockedRecipes = CollectUnlockedRecipes();
            if (unlockedRecipes.Count == 0) return null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                var recipe = PickRecipeByWeight(unlockedRecipes);
                var order = TryBuildOrder(recipe, slotIndex);
                if (order != null) return order;
            }
            return null;
        }

        private List<RecipeDefinition> CollectUnlockedRecipes()
        {
            var list = new List<RecipeDefinition>();
            foreach (var recipe in _content.recipes)
                if (_state.unlockedRecipeIds.Contains(recipe.id))
                    list.Add(recipe);
            return list;
        }

        private RecipeDefinition PickRecipeByWeight(List<RecipeDefinition> recipes)
        {
            // Считаем веса: последний открытый = 4, предпоследний = 2, остальные = 1
            int lastIndex = -1, prevIndex = -1;
            for (int i = _state.unlockedRecipeIds.Count - 1; i >= 0 && (lastIndex < 0 || prevIndex < 0); i--)
            {
                var id = _state.unlockedRecipeIds[i];
                for (int j = 0; j < recipes.Count; j++)
                {
                    if (recipes[j].id == id)
                    {
                        if (lastIndex < 0) lastIndex = j;
                        else if (prevIndex < 0) { prevIndex = j; }
                        break;
                    }
                }
            }

            int totalWeight = 0;
            var weights = new int[recipes.Count];
            for (int i = 0; i < recipes.Count; i++)
            {
                weights[i] = i == lastIndex ? WeightNewest
                           : i == prevIndex ? WeightSecondNewest
                           : WeightOther;
                totalWeight += weights[i];
            }

            int pick = _rng.Next(totalWeight);
            for (int i = 0; i < recipes.Count; i++)
            {
                if (pick < weights[i]) return recipes[i];
                pick -= weights[i];
            }
            return recipes[recipes.Count - 1]; // fallback (не должен срабатывать)
        }

        private Order TryBuildOrder(RecipeDefinition recipe, int slotIndex)
        {
            // 1. Фиксированные ингредиенты — должны быть в стоке
            foreach (var ing in recipe.fixedIngredients)
            {
                if (ing.product == null) continue;
                if (!_inventory.HasEnough(ing.product.id, ing.amount)) return null;
            }

            var order = new Order { recipe = recipe, slotIndex = slotIndex };

            // 2. Молоко — обязательное если needsMilk
            if (recipe.needsMilk)
            {
                var milkOptions = CollectInStock(ProductCategory.Milk);
                if (milkOptions.Count == 0) return null;
                order.milk = milkOptions[_rng.Next(milkOptions.Count)];
            }

            // 3. Сливки — обязательное для рафа
            if (recipe.needsCream)
            {
                var creamOptions = CollectInStock(ProductCategory.Cream);
                if (creamOptions.Count == 0) return null;
                order.cream = creamOptions[_rng.Next(creamOptions.Count)];
            }

            // 4. Сироп — опционально, 40% шанс если применим
            if (recipe.canHaveSyrup && _rng.NextDouble() < SyrupChance)
            {
                var syrupOptions = CollectInStock(ProductCategory.Syrup);
                if (syrupOptions.Count > 0)
                    order.syrup = syrupOptions[_rng.Next(syrupOptions.Count)];
            }

            // 5. Топпинг — опционально, 30% шанс из compatibleToppings
            if (recipe.compatibleToppings != null && recipe.compatibleToppings.Count > 0
                && _rng.NextDouble() < ToppingChance)
            {
                var toppingOptions = new List<ProductDefinition>();
                foreach (var t in recipe.compatibleToppings)
                    if (t != null && _inventory.HasEnough(t.id, 1))
                        toppingOptions.Add(t);
                if (toppingOptions.Count > 0)
                    order.topping = toppingOptions[_rng.Next(toppingOptions.Count)];
            }

            // 6. Тара — 50% to-go если рецепт это разрешает И есть стаканы
            if (recipe.canBeToGo && _rng.NextDouble() < ToGoChance)
            {
                if (HasTakeawayCupInStock()) order.isToGo = true;
            }

            return order;
        }

        private List<ProductDefinition> CollectInStock(ProductCategory category)
        {
            var result = new List<ProductDefinition>();
            foreach (var p in _content.products)
                if (p.category == category && _inventory.HasEnough(p.id, 1))
                    result.Add(p);
            return result;
        }

        private bool HasTakeawayCupInStock()
        {
            foreach (var p in _content.products)
                if (p.category == ProductCategory.Cup && _inventory.HasEnough(p.id, 1))
                    return true;
            return false;
        }
    }
}
```

- [ ] **Step 2: Создать тесты `OrderGeneratorTests.cs`**

В `Assets/Tests/EditMode/`:

```csharp
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

            // Американо открыто последним (вес 4), эспрессо — предпоследним (вес 2).
            // Ожидаемое соотношение 4:2 ≈ 67% / 33%. С шумом проверяем что americano > espresso × 1.5.
            Assert.Greater(americanoCount, espressoCount * 1.5,
                $"Ожидали что американо заметно лидирует (4:2), фактически americano={americanoCount}, espresso={espressoCount}");
        }
    }
}
```

- [ ] **Step 3: Запустить тесты — все зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): OrderGenerator with weighted picks and stock filtering"
```

---

## Task 3: `OrderService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/OrderService.cs`
- Create: `Assets/Tests/EditMode/OrderServiceTests.cs`

- [ ] **Step 1: Создать `OrderService.cs`**

```csharp
using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Управляет 3 слотами заказов: спавн через 5–15 сек когда свободен слот, тик терпения, уход клиентов.
    public class OrderService
    {
        public const int SlotCount = 3;
        public const float SpawnDelayMin = 5f;
        public const float SpawnDelayMax = 15f;
        public const float Patience = 300f;        // 5 минут
        public const float ReputationLossOnAbandon = 0.1f;

        private readonly Order[] _slots = new Order[SlotCount];
        private readonly OrderGenerator _generator;
        private readonly ReputationService _reputation;
        private readonly Random _rng;

        private float _spawnTimer;
        private bool _spawnTimerActive;

        public event Action<Order> OrderSpawned;
        public event Action<Order> OrderAbandoned;
        public event Action<int> OrderRemoved;    // slot освобождён по любой причине
        public event Action<int, float> SlotPatienceTick;

        public OrderService(
            OrderGenerator generator,
            ReputationService reputation,
            Random rng = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
            _rng = rng ?? new System.Random();
        }

        public Order GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return null;
            return _slots[index];
        }

        /// Снять заказ со слота (например когда игрок начал готовить или мы отменили).
        public Order TakeFromSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return null;
            var order = _slots[index];
            if (order != null)
            {
                _slots[index] = null;
                OrderRemoved?.Invoke(index);
            }
            return order;
        }

        /// Tick — вызывается каждый кадр из MonoBehaviour-обёртки.
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0) return;

            // 1. Тик терпения для всех непустых слотов
            for (int i = 0; i < SlotCount; i++)
            {
                var order = _slots[i];
                if (order == null) continue;
                order.remainingPatience -= deltaTime;
                if (order.remainingPatience <= 0)
                {
                    _slots[i] = null;
                    _reputation.Adjust(-ReputationLossOnAbandon);
                    OrderAbandoned?.Invoke(order);
                    OrderRemoved?.Invoke(i);
                }
                else
                {
                    SlotPatienceTick?.Invoke(i, order.remainingPatience);
                }
            }

            // 2. Спавн нового, если есть свободный слот
            int freeIndex = FindFreeSlot();
            if (freeIndex < 0)
            {
                _spawnTimerActive = false;
                return;
            }

            if (!_spawnTimerActive)
            {
                _spawnTimer = NextSpawnDelay();
                _spawnTimerActive = true;
                return;
            }

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0) return;

            var newOrder = _generator.Generate(freeIndex);
            if (newOrder != null)
            {
                newOrder.remainingPatience = Patience;
                _slots[freeIndex] = newOrder;
                OrderSpawned?.Invoke(newOrder);
            }

            // Сбрасываем таймер вне зависимости от того, удалось ли сгенерить.
            _spawnTimer = NextSpawnDelay();
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < SlotCount; i++)
                if (_slots[i] == null) return i;
            return -1;
        }

        private float NextSpawnDelay()
        {
            return (float)(_rng.NextDouble() * (SpawnDelayMax - SpawnDelayMin) + SpawnDelayMin);
        }
    }
}
```

- [ ] **Step 2: Создать тесты `OrderServiceTests.cs`**

```csharp
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
            var service = new OrderService(_generator, _reputation, new System.Random(1));
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
            var service = new OrderService(_generator, _reputation, new System.Random(1));
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
            var service = new OrderService(_generator, _reputation, new System.Random(1));
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
            var service = new OrderService(_generator, _reputation, new System.Random(1));

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
            var service = new OrderService(_generator, _reputation, new System.Random(1));
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

            var service = new OrderService(_generator, _reputation, new System.Random(1));
            int spawnCount = 0;
            service.OrderSpawned += _ => spawnCount++;

            // 60 сек тиков — ничего не должно появиться (Generator вернёт null)
            for (int i = 0; i < 60; i++) service.Tick(1f);

            Assert.AreEqual(0, spawnCount);
        }
    }
}
```

- [ ] **Step 3: Запустить тесты — все зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): OrderService with 3 slots, patience tick, abandonment"
```

---

## Task 4: Подключить `OrderService` к `GameStateManager`

**Files:**
- Modify: `Assets/Scripts/Core/GameStateManager.cs`

- [ ] **Step 1: Добавить публичные свойства**

В `GameStateManager.cs` найди блок свойств (после `public SaveService Save { get; private set; }`) и добавь:

```csharp
        public OrderGenerator OrderGenerator { get; private set; }
        public OrderService Orders { get; private set; }
```

- [ ] **Step 2: Создать сервисы в `Awake`**

Найди в `Awake` строку с созданием `GoalTracker = new GoalTrackerService(...)`. Сразу **после** неё добавь:

```csharp
            OrderGenerator = new OrderGenerator(State, content, Inventory);
            Orders = new OrderService(OrderGenerator, Reputation);
```

- [ ] **Step 3: Подписать сейв на OrderSpawned (чтобы заказ переживал перезапуск)**

В блоке "Подписываем сохранение на любые изменения" добавь:

```csharp
            Orders.OrderSpawned += _ => Save.Save(State);
            Orders.OrderAbandoned += _ => Save.Save(State);
```

(Пока заказы не персистятся в `GameState`, но дальше — да. Подписки заложены.)

- [ ] **Step 4: Compile, Console чистая, Commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs && git commit -m "feat(core): wire OrderGenerator + OrderService into GameStateManager"
```

---

## Task 5: `OrderServiceTicker` — MonoBehaviour, который тикает

**Files:**
- Create: `Assets/Scripts/Core/OrderServiceTicker.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `OrderServiceTicker.cs`**

В `Assets/Scripts/Core/`:

```csharp
using UnityEngine;

namespace DrinkitGame.Core
{
    /// MonoBehaviour-обёртка вокруг OrderService.Tick() — гонит таймер каждый кадр.
    /// Висит на GameRoot рядом с GameStateManager.
    public class OrderServiceTicker : MonoBehaviour
    {
        private void Update()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.Orders == null) return;
            gsm.Orders.Tick(Time.deltaTime);
        }
    }
}
```

- [ ] **Step 2: Прицепить к `GameRoot`**

В Hierarchy → `GameRoot` → Inspector → `Add Component` → `Order Service Ticker`.

- [ ] **Step 3: Save сцены**

Cmd+S.

- [ ] **Step 4: Запусти Play**

В Console каждые 5–15 сек должны идти автосохранения (от OrderSpawned). Видимо? Только если повесить debug-лог. Можно временно добавить в `GameStateManager.Awake`:

```csharp
            Orders.OrderSpawned += o => Debug.Log($"[Orders] Spawned: {o.recipe.id} in slot {o.slotIndex}");
            Orders.OrderAbandoned += o => Debug.Log($"[Orders] ABANDONED: {o.recipe.id}");
```

(Это не обязательно — потом UI покажет. Если хочешь убедиться что тик работает — добавь, посмотри логи, потом убери или оставь.)

Запусти Play, подожди 20 секунд — в Console должна появиться строка `[Orders] Spawned: espresso in slot 0`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core Assets/Scenes/Main.unity && git commit -m "feat(core): OrderServiceTicker MonoBehaviour drives OrderService.Tick"
```

---

## Task 6: `OrderSlotView` — UI для одного слота

**Files:**
- Create: `Assets/Scripts/UI/OrderSlotView.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `OrderSlotView.cs`**

В `Assets/Scripts/UI/`:

```csharp
using System.Text;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// UI одного слота заказа. Показывает напиток + модификаторы + таймер.
    /// Button делает Click → событие, которое ловит OrderSlotsController.
    public class OrderSlotView : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Индекс слота 0..2")]
        public int slotIndex;

        [Header("UI parts")]
        public GameObject contentRoot;       // показываем когда заказ есть
        public GameObject emptyRoot;         // показываем когда заказ пустой
        public TMP_Text recipeNameLabel;
        public TMP_Text modifiersLabel;
        public TMP_Text timerLabel;
        public Button clickButton;

        /// Событие — игрок тапнул по слоту. Передаётся индекс.
        public event System.Action<int> Tapped;

        private Order _current;

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(() =>
                {
                    if (_current != null) Tapped?.Invoke(slotIndex);
                });
        }

        /// Поставить заказ в слот (заменяет существующий) или null чтобы очистить.
        public void Bind(Order order)
        {
            _current = order;
            if (contentRoot != null) contentRoot.SetActive(order != null);
            if (emptyRoot != null) emptyRoot.SetActive(order == null);
            if (order == null) return;

            if (recipeNameLabel != null)
                recipeNameLabel.text = order.recipe.displayName;
            if (modifiersLabel != null)
                modifiersLabel.text = BuildModifiersString(order);
            UpdateTimer(order.remainingPatience);
        }

        /// Обновить только таймер (вызывается из контроллера на тик).
        public void UpdateTimer(float remainingSeconds)
        {
            if (timerLabel == null) return;
            int t = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            int m = t / 60;
            int s = t % 60;
            timerLabel.text = $"{m}:{s:00}";
        }

        private static string BuildModifiersString(Order order)
        {
            var sb = new StringBuilder();
            if (order.milk != null) sb.Append(order.milk.displayName).Append(" · ");
            if (order.cream != null) sb.Append("сливки · ");
            if (order.syrup != null) sb.Append("сироп · ");
            if (order.topping != null) sb.Append(order.topping.displayName).Append(" · ");
            sb.Append(order.isToGo ? "с собой" : "тут");
            return sb.ToString();
        }
    }
}
```

- [ ] **Step 2: Доработать первый слот в сцене**

Открой сцену Main, в Hierarchy зайди в `MainScreenPanel/OrdersSection/SlotsRow/Slot_1`.

Сейчас там просто `Image` + `Status` (TMP "Пусто"). Нужна более детальная разметка.

**Внутри `Slot_1` создай дополнительные дети:**

1. Правый клик `Slot_1` → `Create Empty` → переименуй в `EmptyState`. Перетащи существующий `Status` (TMP "Пусто") внутрь `EmptyState`. RectTransform `EmptyState`: stretch на весь родитель.

2. Правый клик `Slot_1` → `Create Empty` → переименуй в `ContentState`. RectTransform: stretch на весь родитель.

3. Внутри `ContentState` добавь по очереди:
   - `UI → Text - TextMeshPro`, переименуй в `RecipeName`. RectTransform: Top stretch (якорь верхний центр + растянуть по ширине), Height = 28, Top = 4.
     - Text: `Капучино` (плейсхолдер)
     - Font Size: 13
     - Color: белый
     - Alignment: Center + Middle
     - Wrapping: Disabled
   - `UI → Text - TextMeshPro`, переименуй в `Modifiers`. RectTransform: между RecipeName и Timer (Top=36, Height=28).
     - Text: `на овсяном · тут`
     - Font Size: 10
     - Color: белый
     - Alignment: Center + Middle
     - Wrapping: Enabled
   - `UI → Text - TextMeshPro`, переименуй в `Timer`. RectTransform: Bottom anchor, Height=24, Bottom=4.
     - Text: `4:55`
     - Font Size: 14
     - Color: белый
     - Alignment: Center + Middle

4. На сам `Slot_1`:
   - Поменяй цвет `Image` с серого на синий: HEX `5A8DDC` (для теста "карточка заполнена"). Позже OrderSlotView будет переключать между серым и синим через ContentState/EmptyState.
   - Чтобы Slot был кликабельным — `Add Component` → `Button`. В Button.Image поле должно подставиться существующий Image. Можно убрать transition анимации или оставить дефолтные.

5. Теперь `Add Component` → `Order Slot View`. В инспекторе:
   - Slot Index: `0`
   - Content Root: перетащи `ContentState`
   - Empty Root: перетащи `EmptyState`
   - Recipe Name Label: перетащи `ContentState/RecipeName`
   - Modifiers Label: перетащи `ContentState/Modifiers`
   - Timer Label: перетащи `ContentState/Timer`
   - Click Button: перетащи сам `Slot_1` (там Button-компонент)

- [ ] **Step 3: Превратить Slot_1 в Prefab (чтобы 2 и 3 переиспользовать)**

В Project панели зайди в `Assets/Prefabs/`. Перетащи `Slot_1` из Hierarchy в эту папку. Появится prefab. Переименуй prefab в `OrderSlotCard`.

В Hierarchy теперь `Slot_1` — синий (prefab instance).

- [ ] **Step 4: Удалить Slot_2 и Slot_3, заменить на prefab**

В Hierarchy выдели `Slot_2` и `Slot_3` → удали (Delete).

Перетащи 2 раза `OrderSlotCard` prefab из `Assets/Prefabs/` в `SlotsRow` в Hierarchy. Должно получиться 3 слота: `Slot_1`, `OrderSlotCard`, `OrderSlotCard (1)`.

Переименуй копии в `Slot_2` и `Slot_3`. На каждом в `Order Slot View` компоненте поменяй `Slot Index` соответственно на `1` и `2`.

- [ ] **Step 5: Сохрани сцену (Cmd+S)**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity Assets/Prefabs && git commit -m "feat(ui): OrderSlotView with content/empty states + slot card prefab"
```

---

## Task 7: `OrderSlotsController` — координирует 3 слота с сервисом

**Files:**
- Create: `Assets/Scripts/UI/OrderSlotsController.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `OrderSlotsController.cs`**

```csharp
using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Координирует 3 OrderSlotView и подписывается на события OrderService.
    /// Висит на родительском объекте OrdersSection.
    public class OrderSlotsController : MonoBehaviour
    {
        [Tooltip("Три OrderSlotView в порядке 0, 1, 2.")]
        public OrderSlotView[] slots = new OrderSlotView[3];

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            // Подписки на сервис
            _gsm.Orders.OrderSpawned += OnOrderSpawned;
            _gsm.Orders.OrderRemoved += OnOrderRemoved;
            _gsm.Orders.SlotPatienceTick += OnPatienceTick;

            // Обработчики тапов
            foreach (var slot in slots)
                if (slot != null) slot.Tapped += OnSlotTapped;

            // Изначальное состояние (на случай восстановления из сейва или просто пустое)
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].Bind(_gsm.Orders.GetSlot(i));
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Orders.OrderSpawned -= OnOrderSpawned;
            _gsm.Orders.OrderRemoved -= OnOrderRemoved;
            _gsm.Orders.SlotPatienceTick -= OnPatienceTick;
            foreach (var slot in slots)
                if (slot != null) slot.Tapped -= OnSlotTapped;
        }

        private void OnOrderSpawned(Order order)
        {
            if (order.slotIndex >= 0 && order.slotIndex < slots.Length && slots[order.slotIndex] != null)
                slots[order.slotIndex].Bind(order);
        }

        private void OnOrderRemoved(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null)
                slots[slotIndex].Bind(null);
        }

        private void OnPatienceTick(int slotIndex, float remaining)
        {
            if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null)
                slots[slotIndex].UpdateTimer(remaining);
        }

        private void OnSlotTapped(int slotIndex)
        {
            var order = _gsm.Orders.GetSlot(slotIndex);
            if (order == null) return;

            // На Phase 5 — просто лог; в Phase 6 откроем Cooking screen
            Debug.Log($"[Order tapped] slot={slotIndex} recipe={order.recipe.id} " +
                      $"milk={order.milk?.id ?? "-"} syrup={order.syrup?.id ?? "-"} " +
                      $"topping={order.topping?.id ?? "-"} togo={order.isToGo}");

            // Снимаем заказ со слота, чтобы освободить
            _gsm.Orders.TakeFromSlot(slotIndex);
        }
    }
}
```

- [ ] **Step 2: Прицепить контроллер**

В Hierarchy → `MainScreenPanel/OrdersSection` → `Add Component` → `Order Slots Controller`.

В инспекторе компонента:
- Slots: 3 элемента (нажми "+" три раза или сразу укажи Size 3)
  - Element 0: перетащи `Slot_1`
  - Element 1: перетащи `Slot_2`
  - Element 2: перетащи `Slot_3`

- [ ] **Step 3: Сохрани сцену и запусти Play**

Через 5–15 сек должен появиться первый заказ — в слоте `Slot_1` или другом. Увидишь:
- Название напитка ("Эспрессо")
- Модификаторы ("тут" — без сиропа/топпинга поскольку онбординг даёт только зерно)
- Таймер `5:00` тикает вниз

В пустых слотах — серая заглушка "Пусто".

Тапни по заказу — слот опустеет, в Console:
```
[Order tapped] slot=0 recipe=espresso milk=- syrup=- topping=- togo=False
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity && git commit -m "feat(ui): OrderSlotsController syncs 3 slots with OrderService events"
```

---

## Task 8: Финальная сверка Phase 5

- [ ] **Step 1: Все тесты зелёные**

Test Runner → EditMode → Run All. Должно быть ~70 зелёных (Phase 1-4 + новые: OrderGenerator + OrderService).

- [ ] **Step 2: Лайв-проверка в Play**

Запусти Play. Что должно происходить:

1. **0–15 сек:** появляется первый заказ в `Slot_1` (или другом свободном).
2. **Каждые 5–15 сек:** в свободные слоты доспавниваются заказы.
3. **Когда все 3 заняты:** новые не появляются.
4. **Тапни по заказу:** слот опустеет, в Console лог.
5. **Если оставить заказ на 5 минут:** слот опустеет, в Console `[Orders] ABANDONED: ...` (если оставил Debug.Log из Task 5), рейтинг в топ-баре упадёт на 0.1.

(Чтобы быстро проверить уход клиента, можешь временно поменять `Patience = 300f` на `Patience = 15f` в OrderService.cs — но **верни обратно** перед коммитом!)

- [ ] **Step 3: Чистый Console при Play (только наши Debug.Log)**

- [ ] **Step 4: git log проверка**

```bash
git log --oneline | head -10
```

Должно быть 7 коммитов Phase 5.

---

## Self-Review

После прохождения:
1. ✅ `Order` POCO + `OrderGenerator` (взвешенный выбор + фильтр out-of-stock) + тесты
2. ✅ `OrderService` (3 слота, спавн 5–15 сек, таймер 300 сек, потеря репутации) + тесты
3. ✅ `OrderServiceTicker` MonoBehaviour висит на GameRoot
4. ✅ `OrderSlotView` UI рендерит заказ (название, модификаторы, таймер)
5. ✅ Prefab `OrderSlotCard` использован для 3 слотов
6. ✅ `OrderSlotsController` синхронизирует UI с сервисом
7. ✅ Тап по слоту вызывает Debug.Log с деталями заказа

**Готово → пиши `Phase 5 done`. Дальше Phase 6: Mock Cooking — простой экран готовки с кнопкой "Выдать", которая платит деньги и расходует ингредиенты. Это закроет базовый игровой цикл.**

---

## Что НЕ делаем в этой фазе (anti-scope)

- ❌ Реальная готовка / мини-игры — Phase 8
- ❌ Cooking screen — Phase 6 (там добавим простой экран с "Выдать")
- ❌ Сохранение заказов в `GameState` между сессиями — пока заказы в памяти, рестарт игры = новые заказы
- ❌ Пауза таймеров когда игрок вне TG — упрощённо: тикаем всегда (доделаем в Phase 11)
- ❌ Звуки спавна и ухода клиента — отдельный шаг полировки
- ❌ Анимации появления карточек — отдельный шаг полировки
