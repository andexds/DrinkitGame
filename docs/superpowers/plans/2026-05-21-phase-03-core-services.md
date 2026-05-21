# Phase 3 — Core Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Описать всю логику игры (баланс, инвентарь, репутация, квесты, рецепты, машина, goal-tracker, сохранение) как **чистые C#-классы** — без MonoBehaviour и без UI. Покрыть тестами в Edit Mode. Затем оформить один MonoBehaviour-корень `GameStateManager`, который собирает все сервисы и кладёт их на `GameRoot` в сцене.

**Architecture:**
- `GameState` — POCO `[Serializable]` со всем мутабельным состоянием игры. Один объект, передаётся в сервисы.
- Каждый "сервис" (Economy, Inventory, Reputation, …) — обычный C#-класс. Конструктор принимает `GameState` (+ `GameContent` где нужно). Методы мутируют состояние и стреляют событиями (`event Action<…>`).
- `SaveService` — сериализует `GameState` в JSON через `JsonUtility` и сохраняет в `PlayerPrefs`. Восстанавливает на старте.
- `GameStateManager` — единственный MonoBehaviour, висит на `GameRoot`. Создаёт сервисы, подписывает `SaveService` на их события, прокидывает ссылки в UI (UI напишем в Phase 4).

**Tech Stack:** C# 9 · Unity 2022.3 · NUnit (Edit Mode) · `JsonUtility` для сериализации · `PlayerPrefs` для персистенции.

**Конец фазы:** Все сервисы написаны и покрыты тестами (~30 зелёных). `GameStateManager` висит на `GameRoot`, при Play в Console логируется стартовое состояние ("Balance: 0, Recipes: [espresso], Machine: T1"). Состояние сохраняется в PlayerPrefs и подгружается при следующем Play.

---

## Task 1: `GameState` — POCO для всего состояния игры

**Files:**
- Create: `Assets/Scripts/Core/GameState.cs`

`GameState` — это `[Serializable]` класс со всеми мутабельными полями. Один экземпляр на игру. Сервисы держат на него ссылку и меняют его.

`JsonUtility` Unity не умеет сериализовать `Dictionary`, поэтому для инвентаря и счётчиков квестов используем `List<>` из пар "ключ-значение" и оборачиваем доступ в методы.

- [ ] **Step 1: Создать `GameState.cs`**

В `Assets/Scripts/Core/` (это папка, созданная в Phase 1; **не путать** с `Assets/Scripts/Data/`) → правый клик → `Create → C# Script` → имя `GameState`. Содержимое:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Вся мутабельная информация одной игровой сессии.
    /// Это POCO — никакого Unity-API внутри. Сервисы мутируют поля напрямую.
    /// Сериализуется в JSON через JsonUtility и сохраняется в PlayerPrefs.
    [Serializable]
    public class GameState
    {
        [Tooltip("Текущий баланс в рублях.")]
        public int balance;

        [Tooltip("Репутация 0.0–5.0 (информативно).")]
        public float reputation = 5f;

        [Tooltip("Индекс текущего тира кофемашины (1, 2 или 3).")]
        public int currentMachineTierIndex = 1;

        [Tooltip("ID рецептов, которые уже куплены/открыты.")]
        public List<string> unlockedRecipeIds = new();

        [Tooltip("Слоты инвентаря (1 запись на каждый купленный продукт).")]
        public List<InventorySlot> inventory = new();

        [Tooltip("Счётчики проданных напитков для квестов.")]
        public List<RecipeSoldCount> recipeSoldCounts = new();

        [Tooltip("Сколько жетонов колеса удачи накоплено.")]
        public int wheelTokens;

        [Tooltip("Есть ли активный ваучер скидки -50% на след. рецепт.")]
        public bool hasDiscountVoucher;

        [Tooltip("Активен ли буст 'следующий заказ ×2'.")]
        public bool hasDoubleNextOrderBuff;

        [Tooltip("Прошёл ли игрок онбординг (хотя бы один раз).")]
        public bool onboardingCompleted;
    }

    /// Пара "продукт → остаток" в инвентаре.
    [Serializable]
    public class InventorySlot
    {
        public string productId;
        public int count;

        public InventorySlot() { }
        public InventorySlot(string productId, int count)
        {
            this.productId = productId;
            this.count = count;
        }
    }

    /// Пара "рецепт → сколько продано" для квестов.
    [Serializable]
    public class RecipeSoldCount
    {
        public string recipeId;
        public int count;

        public RecipeSoldCount() { }
        public RecipeSoldCount(string recipeId, int count)
        {
            this.recipeId = recipeId;
            this.count = count;
        }
    }
}
```

- [ ] **Step 2: Дождаться компиляции (Console чистая)**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core && git commit -m "feat(core): GameState POCO + inventory/sold-count slots"
```

---

## Task 2: `EconomyService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/EconomyService.cs`
- Create: `Assets/Tests/EditMode/EconomyServiceTests.cs`

- [ ] **Step 1: Создать `EconomyService.cs`**

В `Assets/Scripts/Core/`:

```csharp
using System;

namespace DrinkitGame.Core
{
    /// Управляет балансом игрока. Все транзакции с деньгами идут через этот сервис.
    public class EconomyService
    {
        private readonly GameState _state;

        /// Стреляет после каждого изменения баланса. Параметр — новый баланс.
        public event Action<int> BalanceChanged;

        public EconomyService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public int Balance => _state.balance;

        /// Зачислить N₽ на баланс. amount должен быть > 0.
        public void Earn(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            _state.balance += amount;
            BalanceChanged?.Invoke(_state.balance);
        }

        /// Списать N₽. Возвращает true если хватило денег, иначе false (баланс не меняется).
        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            if (_state.balance < amount) return false;
            _state.balance -= amount;
            BalanceChanged?.Invoke(_state.balance);
            return true;
        }
    }
}
```

- [ ] **Step 2: Создать тесты `EconomyServiceTests.cs`**

В `Assets/Tests/EditMode/`:

```csharp
using DrinkitGame.Core;
using NUnit.Framework;
using System;

namespace DrinkitGame.Tests.EditMode
{
    public class EconomyServiceTests
    {
        [Test]
        public void Earn_AddsToBalance()
        {
            var state = new GameState { balance = 100 };
            var service = new EconomyService(state);
            service.Earn(50);
            Assert.AreEqual(150, service.Balance);
        }

        [Test]
        public void Earn_NegativeOrZero_Throws()
        {
            var service = new EconomyService(new GameState());
            Assert.Throws<ArgumentException>(() => service.Earn(0));
            Assert.Throws<ArgumentException>(() => service.Earn(-10));
        }

        [Test]
        public void TrySpend_Succeeds_WhenEnough()
        {
            var state = new GameState { balance = 100 };
            var service = new EconomyService(state);
            Assert.IsTrue(service.TrySpend(60));
            Assert.AreEqual(40, service.Balance);
        }

        [Test]
        public void TrySpend_Fails_WhenInsufficient()
        {
            var state = new GameState { balance = 30 };
            var service = new EconomyService(state);
            Assert.IsFalse(service.TrySpend(50));
            Assert.AreEqual(30, service.Balance);
        }

        [Test]
        public void Earn_FiresBalanceChangedEvent()
        {
            var service = new EconomyService(new GameState { balance = 0 });
            int notified = -1;
            service.BalanceChanged += b => notified = b;
            service.Earn(100);
            Assert.AreEqual(100, notified);
        }

        [Test]
        public void TrySpend_DoesNotFireEvent_WhenInsufficient()
        {
            var service = new EconomyService(new GameState { balance = 10 });
            bool fired = false;
            service.BalanceChanged += _ => fired = true;
            service.TrySpend(100);
            Assert.IsFalse(fired);
        }
    }
}
```

- [ ] **Step 3: Запустить тесты**

Test Runner → EditMode → Run All. Должно стать +6 зелёных (всего 17 с учётом фазы 2).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): EconomyService with Earn/TrySpend and event"
```

---

## Task 3: `InventoryService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/InventoryService.cs`
- Create: `Assets/Tests/EditMode/InventoryServiceTests.cs`

- [ ] **Step 1: Создать `InventoryService.cs`**

```csharp
using System;

namespace DrinkitGame.Core
{
    /// Управляет остатками продуктов на складе.
    /// Хранение в виде List<InventorySlot>; поиск по productId — линейный (15 продуктов = норм).
    public class InventoryService
    {
        private readonly GameState _state;

        /// Стреляет после любого изменения остатка. Параметры: productId, новый остаток.
        public event Action<string, int> StockChanged;

        public InventoryService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// Текущий остаток продукта (0 если не в инвентаре).
        public int GetStock(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("productId is empty", nameof(productId));
            foreach (var slot in _state.inventory)
                if (slot.productId == productId) return slot.count;
            return 0;
        }

        /// Прибавить N единиц продукта.
        public void Add(string productId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            var slot = FindOrCreateSlot(productId);
            slot.count += amount;
            StockChanged?.Invoke(productId, slot.count);
        }

        /// Попытка списать N единиц. true если хватило.
        public bool TryConsume(string productId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            var slot = FindSlot(productId);
            if (slot == null || slot.count < amount) return false;
            slot.count -= amount;
            StockChanged?.Invoke(productId, slot.count);
            return true;
        }

        /// Достаточно ли единиц на складе для конкретной операции.
        public bool HasEnough(string productId, int amount)
        {
            return GetStock(productId) >= amount;
        }

        private InventorySlot FindSlot(string productId)
        {
            foreach (var slot in _state.inventory)
                if (slot.productId == productId) return slot;
            return null;
        }

        private InventorySlot FindOrCreateSlot(string productId)
        {
            var slot = FindSlot(productId);
            if (slot != null) return slot;
            slot = new InventorySlot(productId, 0);
            _state.inventory.Add(slot);
            return slot;
        }
    }
}
```

- [ ] **Step 2: Создать тесты `InventoryServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using NUnit.Framework;
using System;

namespace DrinkitGame.Tests.EditMode
{
    public class InventoryServiceTests
    {
        [Test]
        public void GetStock_ReturnsZero_WhenProductNotInInventory()
        {
            var service = new InventoryService(new GameState());
            Assert.AreEqual(0, service.GetStock("beans"));
        }

        [Test]
        public void Add_IncreasesStock()
        {
            var service = new InventoryService(new GameState());
            service.Add("beans", 5);
            Assert.AreEqual(5, service.GetStock("beans"));
            service.Add("beans", 3);
            Assert.AreEqual(8, service.GetStock("beans"));
        }

        [Test]
        public void TryConsume_Succeeds_WhenEnough()
        {
            var service = new InventoryService(new GameState());
            service.Add("milk_cow", 10);
            Assert.IsTrue(service.TryConsume("milk_cow", 3));
            Assert.AreEqual(7, service.GetStock("milk_cow"));
        }

        [Test]
        public void TryConsume_Fails_WhenInsufficient()
        {
            var service = new InventoryService(new GameState());
            service.Add("syrup_vanilla", 2);
            Assert.IsFalse(service.TryConsume("syrup_vanilla", 5));
            Assert.AreEqual(2, service.GetStock("syrup_vanilla"));
        }

        [Test]
        public void TryConsume_ProductNotInInventory_ReturnsFalse()
        {
            var service = new InventoryService(new GameState());
            Assert.IsFalse(service.TryConsume("matcha_powder", 1));
        }

        [Test]
        public void HasEnough_ReturnsCorrectAnswer()
        {
            var service = new InventoryService(new GameState());
            service.Add("beans", 5);
            Assert.IsTrue(service.HasEnough("beans", 5));
            Assert.IsTrue(service.HasEnough("beans", 1));
            Assert.IsFalse(service.HasEnough("beans", 6));
            Assert.IsFalse(service.HasEnough("cream", 1));
        }

        [Test]
        public void Add_FiresStockChanged()
        {
            var service = new InventoryService(new GameState());
            string changedId = null;
            int changedCount = -1;
            service.StockChanged += (id, n) => { changedId = id; changedCount = n; };
            service.Add("beans", 7);
            Assert.AreEqual("beans", changedId);
            Assert.AreEqual(7, changedCount);
        }

        [Test]
        public void Add_NegativeOrZero_Throws()
        {
            var service = new InventoryService(new GameState());
            Assert.Throws<ArgumentException>(() => service.Add("beans", 0));
            Assert.Throws<ArgumentException>(() => service.Add("beans", -1));
        }
    }
}
```

- [ ] **Step 3: Запустить тесты — все зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): InventoryService with Add/TryConsume/HasEnough"
```

---

## Task 4: `ReputationService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/ReputationService.cs`
- Create: `Assets/Tests/EditMode/ReputationServiceTests.cs`

- [ ] **Step 1: Создать `ReputationService.cs`**

```csharp
using System;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Управляет репутацией (float 0.0–5.0). Информативная — ни на что в прототипе не влияет.
    public class ReputationService
    {
        public const float Min = 0f;
        public const float Max = 5f;

        private readonly GameState _state;

        /// Стреляет после любого изменения. Параметр — новое значение.
        public event Action<float> ReputationChanged;

        public ReputationService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public float Reputation => _state.reputation;

        /// Изменить репутацию на дельту (может быть отрицательной). Зажимается в [Min, Max].
        public void Adjust(float delta)
        {
            float next = Mathf.Clamp(_state.reputation + delta, Min, Max);
            if (Mathf.Approximately(next, _state.reputation)) return;
            _state.reputation = next;
            ReputationChanged?.Invoke(_state.reputation);
        }
    }
}
```

- [ ] **Step 2: Создать тесты `ReputationServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class ReputationServiceTests
    {
        [Test]
        public void DefaultReputation_Is5()
        {
            var service = new ReputationService(new GameState());
            Assert.AreEqual(5f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_DecreasesReputation()
        {
            var service = new ReputationService(new GameState());
            service.Adjust(-0.1f);
            Assert.AreEqual(4.9f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_ClampedAtZero()
        {
            var service = new ReputationService(new GameState { reputation = 0.05f });
            service.Adjust(-1f);
            Assert.AreEqual(0f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_ClampedAtFive()
        {
            var service = new ReputationService(new GameState { reputation = 4.95f });
            service.Adjust(1f);
            Assert.AreEqual(5f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_FiresChangedEvent()
        {
            var service = new ReputationService(new GameState());
            float notified = -1f;
            service.ReputationChanged += r => notified = r;
            service.Adjust(-0.1f);
            Assert.AreEqual(4.9f, notified, 0.0001f);
        }

        [Test]
        public void Adjust_NoChange_DoesNotFireEvent()
        {
            var service = new ReputationService(new GameState { reputation = 5f });
            bool fired = false;
            service.ReputationChanged += _ => fired = true;
            service.Adjust(1f); // уже на максимуме
            Assert.IsFalse(fired);
        }
    }
}
```

- [ ] **Step 3: Run all tests — зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): ReputationService with clamped Adjust"
```

---

## Task 5: `QuestService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/QuestService.cs`
- Create: `Assets/Tests/EditMode/QuestServiceTests.cs`

Отслеживает "сколько каких напитков продано" — используется для условий открытия рецептов и прокачки машины.

- [ ] **Step 1: Создать `QuestService.cs`**

```csharp
using System;

namespace DrinkitGame.Core
{
    /// Считает сколько каких рецептов было успешно продано (для квестов на разблокировку).
    public class QuestService
    {
        private readonly GameState _state;

        /// Стреляет после увеличения счётчика. Параметры: recipeId, новое значение.
        public event Action<string, int> CountChanged;

        public QuestService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// Сколько раз продан указанный рецепт (0 если ни разу).
        public int GetSoldCount(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                throw new ArgumentException("recipeId is empty", nameof(recipeId));
            foreach (var entry in _state.recipeSoldCounts)
                if (entry.recipeId == recipeId) return entry.count;
            return 0;
        }

        /// Увеличить счётчик продаж для рецепта на 1.
        public void RecordSale(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                throw new ArgumentException("recipeId is empty", nameof(recipeId));
            var entry = FindOrCreate(recipeId);
            entry.count += 1;
            CountChanged?.Invoke(recipeId, entry.count);
        }

        private RecipeSoldCount FindOrCreate(string recipeId)
        {
            foreach (var entry in _state.recipeSoldCounts)
                if (entry.recipeId == recipeId) return entry;
            var fresh = new RecipeSoldCount(recipeId, 0);
            _state.recipeSoldCounts.Add(fresh);
            return fresh;
        }
    }
}
```

- [ ] **Step 2: Создать тесты `QuestServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class QuestServiceTests
    {
        [Test]
        public void GetSoldCount_ReturnsZero_ForUnseenRecipe()
        {
            var service = new QuestService(new GameState());
            Assert.AreEqual(0, service.GetSoldCount("americano"));
        }

        [Test]
        public void RecordSale_IncrementsCount()
        {
            var service = new QuestService(new GameState());
            service.RecordSale("americano");
            service.RecordSale("americano");
            service.RecordSale("espresso");
            Assert.AreEqual(2, service.GetSoldCount("americano"));
            Assert.AreEqual(1, service.GetSoldCount("espresso"));
        }

        [Test]
        public void RecordSale_FiresCountChanged()
        {
            var service = new QuestService(new GameState());
            string id = null;
            int n = -1;
            service.CountChanged += (i, c) => { id = i; n = c; };
            service.RecordSale("latte");
            Assert.AreEqual("latte", id);
            Assert.AreEqual(1, n);
        }
    }
}
```

- [ ] **Step 3: Run all tests — зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): QuestService with sold counts per recipe"
```

---

## Task 6: `RecipeService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/RecipeService.cs`
- Create: `Assets/Tests/EditMode/RecipeServiceTests.cs`

Каталог рецептов: какие открыты, можно ли купить, попытка покупки.

- [ ] **Step 1: Создать `RecipeService.cs`**

```csharp
using System;
using System.Collections.Generic;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Управляет состоянием каталога рецептов: какие открыты, можно ли купить.
    public class RecipeService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;

        /// Стреляет когда новый рецепт был открыт. Параметр — RecipeDefinition.
        public event Action<RecipeDefinition> RecipeUnlocked;

        public RecipeService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
        }

        public bool IsUnlocked(string recipeId) => _state.unlockedRecipeIds.Contains(recipeId);

        /// Можно ли сейчас купить (выполнены все условия + хватает денег)?
        public PurchaseAvailability GetAvailability(RecipeDefinition recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (IsUnlocked(recipe.id)) return PurchaseAvailability.AlreadyOwned;
            if (recipe.requiredMachineTier != null
                && _state.currentMachineTierIndex < recipe.requiredMachineTier.tierIndex)
                return PurchaseAvailability.NeedsHigherMachine;
            if (recipe.unlockQuestTargetRecipe != null
                && _quests.GetSoldCount(recipe.unlockQuestTargetRecipe.id) < recipe.unlockQuestTargetCount)
                return PurchaseAvailability.NeedsMoreSales;
            int price = ApplyDiscountIfAny(recipe.recipePurchasePrice);
            if (_economy.Balance < price) return PurchaseAvailability.NotEnoughMoney;
            return PurchaseAvailability.Available;
        }

        /// Попытка купить рецепт. Возвращает true если успешно (деньги списаны, рецепт открыт).
        public bool TryPurchase(RecipeDefinition recipe)
        {
            if (GetAvailability(recipe) != PurchaseAvailability.Available) return false;
            int price = ApplyDiscountIfAny(recipe.recipePurchasePrice);
            if (!_economy.TrySpend(price)) return false; // защита от race
            if (_state.hasDiscountVoucher) _state.hasDiscountVoucher = false;
            _state.unlockedRecipeIds.Add(recipe.id);
            RecipeUnlocked?.Invoke(recipe);
            return true;
        }

        /// Стартовый набор рецептов: добавляет starterRecipe если ещё не открыт.
        public void EnsureStarterUnlocked()
        {
            if (_content.starterRecipe == null) return;
            if (!IsUnlocked(_content.starterRecipe.id))
                _state.unlockedRecipeIds.Add(_content.starterRecipe.id);
        }

        /// Список всех открытых рецептов как объекты.
        public IEnumerable<RecipeDefinition> EnumerateUnlocked()
        {
            foreach (var r in _content.recipes)
                if (IsUnlocked(r.id)) yield return r;
        }

        private int ApplyDiscountIfAny(int basePrice) =>
            _state.hasDiscountVoucher ? basePrice / 2 : basePrice;
    }

    public enum PurchaseAvailability
    {
        Available,
        AlreadyOwned,
        NeedsHigherMachine,
        NeedsMoreSales,
        NotEnoughMoney
    }
}
```

- [ ] **Step 2: Создать тесты `RecipeServiceTests.cs`**

Тесты создают мини-фейковые `RecipeDefinition` и `MachineTierDefinition` через `ScriptableObject.CreateInstance` — это валидный способ создавать SO в тестах.

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class RecipeServiceTests
    {
        private GameContent _content;
        private RecipeDefinition _espresso;
        private RecipeDefinition _americano;
        private RecipeDefinition _cappuccino;
        private MachineTierDefinition _t1;
        private MachineTierDefinition _t2;

        [SetUp]
        public void Setup()
        {
            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;

            _espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            _espresso.id = "espresso";
            _espresso.recipePurchasePrice = 0;
            _espresso.requiredMachineTier = _t1;

            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";
            _americano.recipePurchasePrice = 100;
            _americano.requiredMachineTier = _t1;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.recipePurchasePrice = 500;
            _cappuccino.requiredMachineTier = _t2;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.recipes.AddRange(new[] { _espresso, _americano, _cappuccino });
            _content.starterRecipe = _espresso;
        }

        private RecipeService MakeService(GameState state, out EconomyService eco)
        {
            eco = new EconomyService(state);
            var quests = new QuestService(state);
            return new RecipeService(state, _content, eco, quests);
        }

        [Test]
        public void EnsureStarterUnlocked_AddsStarterRecipe()
        {
            var state = new GameState();
            var service = MakeService(state, out _);
            service.EnsureStarterUnlocked();
            Assert.IsTrue(service.IsUnlocked("espresso"));
        }

        [Test]
        public void TryPurchase_Americano_SpendsMoneyAndUnlocks()
        {
            var state = new GameState { balance = 200 };
            var service = MakeService(state, out var eco);
            Assert.IsTrue(service.TryPurchase(_americano));
            Assert.IsTrue(service.IsUnlocked("americano"));
            Assert.AreEqual(100, eco.Balance);
        }

        [Test]
        public void TryPurchase_Cappuccino_FailsWithoutT2()
        {
            var state = new GameState { balance = 1000, currentMachineTierIndex = 1 };
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.NeedsHigherMachine,
                service.GetAvailability(_cappuccino));
            Assert.IsFalse(service.TryPurchase(_cappuccino));
        }

        [Test]
        public void TryPurchase_NotEnoughMoney_Fails()
        {
            var state = new GameState { balance = 50 };
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.NotEnoughMoney,
                service.GetAvailability(_americano));
            Assert.IsFalse(service.TryPurchase(_americano));
            Assert.AreEqual(50, state.balance);
        }

        [Test]
        public void TryPurchase_AlreadyOwned_Fails()
        {
            var state = new GameState { balance = 200 };
            state.unlockedRecipeIds.Add("americano");
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.AlreadyOwned,
                service.GetAvailability(_americano));
            Assert.IsFalse(service.TryPurchase(_americano));
        }

        [Test]
        public void TryPurchase_AppliesDiscountVoucher()
        {
            var state = new GameState { balance = 60, hasDiscountVoucher = true };
            var service = MakeService(state, out var eco);
            Assert.IsTrue(service.TryPurchase(_americano)); // 100 * 0.5 = 50
            Assert.AreEqual(10, eco.Balance);
            Assert.IsFalse(state.hasDiscountVoucher); // ваучер потрачен
        }

        [Test]
        public void TryPurchase_RequiresQuestComplete()
        {
            // делаем латте с квестом 'продать 15 капучино'
            var latte = ScriptableObject.CreateInstance<RecipeDefinition>();
            latte.id = "latte";
            latte.recipePurchasePrice = 600;
            latte.requiredMachineTier = _t2;
            latte.unlockQuestTargetRecipe = _cappuccino;
            latte.unlockQuestTargetCount = 15;

            var state = new GameState
            {
                balance = 1000,
                currentMachineTierIndex = 2
            };
            var service = MakeService(state, out _);

            Assert.AreEqual(PurchaseAvailability.NeedsMoreSales,
                service.GetAvailability(latte));
        }

        [Test]
        public void RecipeUnlocked_EventFires_OnSuccessfulPurchase()
        {
            var state = new GameState { balance = 200 };
            var service = MakeService(state, out _);
            RecipeDefinition unlocked = null;
            service.RecipeUnlocked += r => unlocked = r;
            service.TryPurchase(_americano);
            Assert.AreSame(_americano, unlocked);
        }
    }
}
```

- [ ] **Step 3: Run all tests — зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): RecipeService with TryPurchase, availability states, discount voucher"
```

---

## Task 7: `MachineService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/MachineService.cs`
- Create: `Assets/Tests/EditMode/MachineServiceTests.cs`

- [ ] **Step 1: Создать `MachineService.cs`**

```csharp
using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Текущий тир кофемашины и логика прокачки.
    public class MachineService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;

        /// Стреляет после успешной прокачки. Параметр — новый тир.
        public event Action<MachineTierDefinition> Upgraded;

        public MachineService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
        }

        public int CurrentTierIndex => _state.currentMachineTierIndex;

        public MachineTierDefinition CurrentTier
        {
            get
            {
                foreach (var t in _content.machineTiers)
                    if (t.tierIndex == _state.currentMachineTierIndex) return t;
                return null;
            }
        }

        /// Следующий тир (или null если уже максимальный).
        public MachineTierDefinition NextTier
        {
            get
            {
                int next = _state.currentMachineTierIndex + 1;
                foreach (var t in _content.machineTiers)
                    if (t.tierIndex == next) return t;
                return null;
            }
        }

        /// Доступна ли прокачка прямо сейчас.
        public UpgradeAvailability GetUpgradeAvailability()
        {
            var next = NextTier;
            if (next == null) return UpgradeAvailability.MaxTier;
            if (_economy.Balance < next.purchasePrice) return UpgradeAvailability.NotEnoughMoney;
            if (next.questTargetRecipe1 != null
                && _quests.GetSoldCount(next.questTargetRecipe1.id) < next.questTargetCount1)
                return UpgradeAvailability.QuestIncomplete;
            if (next.questTargetRecipe2 != null
                && _quests.GetSoldCount(next.questTargetRecipe2.id) < next.questTargetCount2)
                return UpgradeAvailability.QuestIncomplete;
            return UpgradeAvailability.Available;
        }

        /// Прокачать машину на следующий тир.
        public bool TryUpgrade()
        {
            if (GetUpgradeAvailability() != UpgradeAvailability.Available) return false;
            var next = NextTier;
            if (!_economy.TrySpend(next.purchasePrice)) return false;
            _state.currentMachineTierIndex = next.tierIndex;
            Upgraded?.Invoke(next);
            return true;
        }
    }

    public enum UpgradeAvailability
    {
        Available,
        MaxTier,
        NotEnoughMoney,
        QuestIncomplete
    }
}
```

- [ ] **Step 2: Создать тесты `MachineServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class MachineServiceTests
    {
        private GameContent _content;
        private MachineTierDefinition _t1, _t2, _t3;
        private RecipeDefinition _americano, _cappuccino, _latte;

        [SetUp]
        public void Setup()
        {
            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";

            _latte = ScriptableObject.CreateInstance<RecipeDefinition>();
            _latte.id = "latte";

            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;
            _t1.purchasePrice = 0;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;
            _t2.purchasePrice = 1500;
            _t2.questTargetRecipe1 = _americano;
            _t2.questTargetCount1 = 10;

            _t3 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t3.tierIndex = 3;
            _t3.purchasePrice = 5000;
            _t3.questTargetRecipe1 = _cappuccino;
            _t3.questTargetCount1 = 5;
            _t3.questTargetRecipe2 = _latte;
            _t3.questTargetCount2 = 5;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.machineTiers.AddRange(new[] { _t1, _t2, _t3 });
        }

        private MachineService MakeService(GameState state, out EconomyService eco, out QuestService quests)
        {
            eco = new EconomyService(state);
            quests = new QuestService(state);
            return new MachineService(state, _content, eco, quests);
        }

        [Test]
        public void CurrentTier_T1_ByDefault()
        {
            var state = new GameState();
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(1, service.CurrentTierIndex);
            Assert.AreSame(_t1, service.CurrentTier);
        }

        [Test]
        public void NextTier_T2_FromT1()
        {
            var state = new GameState();
            var service = MakeService(state, out _, out _);
            Assert.AreSame(_t2, service.NextTier);
        }

        [Test]
        public void NextTier_Null_AtMax()
        {
            var state = new GameState { currentMachineTierIndex = 3 };
            var service = MakeService(state, out _, out _);
            Assert.IsNull(service.NextTier);
            Assert.AreEqual(UpgradeAvailability.MaxTier, service.GetUpgradeAvailability());
        }

        [Test]
        public void GetUpgradeAvailability_NotEnoughMoney()
        {
            var state = new GameState { balance = 100 };
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(UpgradeAvailability.NotEnoughMoney, service.GetUpgradeAvailability());
        }

        [Test]
        public void GetUpgradeAvailability_QuestIncomplete()
        {
            var state = new GameState { balance = 5000 }; // достаточно денег
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(UpgradeAvailability.QuestIncomplete, service.GetUpgradeAvailability());
        }

        [Test]
        public void TryUpgrade_ToT2_WhenAllConditionsMet()
        {
            var state = new GameState { balance = 2000 };
            var service = MakeService(state, out var eco, out var quests);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");

            Assert.AreEqual(UpgradeAvailability.Available, service.GetUpgradeAvailability());
            Assert.IsTrue(service.TryUpgrade());
            Assert.AreEqual(2, service.CurrentTierIndex);
            Assert.AreEqual(500, eco.Balance); // 2000 - 1500
        }

        [Test]
        public void TryUpgrade_FiresUpgradedEvent()
        {
            var state = new GameState { balance = 2000 };
            var service = MakeService(state, out _, out var quests);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");

            MachineTierDefinition upgraded = null;
            service.Upgraded += t => upgraded = t;
            service.TryUpgrade();
            Assert.AreSame(_t2, upgraded);
        }
    }
}
```

- [ ] **Step 3: Run all tests — зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): MachineService with TryUpgrade and tier progression"
```

---

## Task 8: `GoalTrackerService` + тесты

**Files:**
- Create: `Assets/Scripts/Core/Goal.cs`
- Create: `Assets/Scripts/Core/GoalTrackerService.cs`
- Create: `Assets/Tests/EditMode/GoalTrackerServiceTests.cs`

Вычисляет текущую "цель сверху" — что игрок должен сделать дальше.

- [ ] **Step 1: Создать `Goal.cs`**

```csharp
namespace DrinkitGame.Core
{
    /// Что игроку нужно сделать дальше (текстовая цель сверху главного экрана).
    public readonly struct Goal
    {
        public readonly string Description;       // напр. "Купи рецепт американо"
        public readonly string ProgressLabel;     // напр. "100 / 100 ₽" или "7 / 10"
        public readonly bool IsFinal;             // true если игрок открыл всё

        public Goal(string description, string progressLabel, bool isFinal = false)
        {
            Description = description;
            ProgressLabel = progressLabel;
            IsFinal = isFinal;
        }

        public static Goal Final =>
            new("Все рецепты открыты! Просто играй.", string.Empty, true);
    }
}
```

- [ ] **Step 2: Создать `GoalTrackerService.cs`**

```csharp
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Считает текущую "первую невыполненную цель" в линейной прогрессии.
    /// Логика приоритета (см. спек, §11.2):
    ///   1. Купить рецепт американо
    ///   2. Купить машину T2 (квест + цена)
    ///   3. Купить капучино, латте, какао, раф (в порядке)
    ///   4. Купить машину T3
    ///   5. Купить фильтр, матчу
    ///   6. Финал
    public class GoalTrackerService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;
        private readonly MachineService _machine;

        public GoalTrackerService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests,
            MachineService machine)
        {
            _state = state;
            _content = content;
            _economy = economy;
            _quests = quests;
            _machine = machine;
        }

        public Goal CurrentGoal()
        {
            // Линейный обход в порядке, который мы хотим:
            // 1. Сначала идём по рецептам в порядке id: americano → cappuccino → latte → cacao → raf → filter → matcha
            // 2. Между ними проверяем апгрейды машины (когда они уже доступны и нужны)
            string[] orderedRecipeIds =
            {
                "americano", "cappuccino", "latte", "cacao", "raf", "filter", "matcha"
            };

            // Сначала проверяем: если до cappuccino дойти, нужна машина T2
            // Проверим прогрессию через машину тоже:
            var nextRecipe = NextUnlockTarget(orderedRecipeIds);
            if (nextRecipe == null)
                return Goal.Final;

            // Если для следующего рецепта нужна машина, цель = "купи машину"
            if (nextRecipe.requiredMachineTier != null
                && _state.currentMachineTierIndex < nextRecipe.requiredMachineTier.tierIndex)
            {
                return MakeMachineUpgradeGoal();
            }

            // Иначе цель — купить сам рецепт
            return MakeRecipePurchaseGoal(nextRecipe);
        }

        private RecipeDefinition NextUnlockTarget(string[] orderedIds)
        {
            foreach (var id in orderedIds)
            {
                if (_state.unlockedRecipeIds.Contains(id)) continue;
                foreach (var r in _content.recipes)
                    if (r.id == id) return r;
            }
            return null;
        }

        private Goal MakeRecipePurchaseGoal(RecipeDefinition recipe)
        {
            // Если есть квест-условие — показываем его прогресс
            if (recipe.unlockQuestTargetRecipe != null && recipe.unlockQuestTargetCount > 0)
            {
                int sold = _quests.GetSoldCount(recipe.unlockQuestTargetRecipe.id);
                int target = recipe.unlockQuestTargetCount;
                if (sold < target)
                {
                    return new Goal(
                        recipe.unlockQuestDescription,
                        $"{sold} / {target}");
                }
            }
            int price = _state.hasDiscountVoucher ? recipe.recipePurchasePrice / 2 : recipe.recipePurchasePrice;
            return new Goal(
                $"Купи рецепт «{recipe.displayName}»",
                $"{_economy.Balance} / {price} ₽");
        }

        private Goal MakeMachineUpgradeGoal()
        {
            var next = _machine.NextTier;
            if (next == null) return Goal.Final;

            // Проверим квест машины
            if (next.questTargetRecipe1 != null && next.questTargetCount1 > 0)
            {
                int sold = _quests.GetSoldCount(next.questTargetRecipe1.id);
                if (sold < next.questTargetCount1)
                {
                    return new Goal(
                        next.questDescription,
                        $"{sold} / {next.questTargetCount1}");
                }
            }
            if (next.questTargetRecipe2 != null && next.questTargetCount2 > 0)
            {
                int sold = _quests.GetSoldCount(next.questTargetRecipe2.id);
                if (sold < next.questTargetCount2)
                {
                    return new Goal(
                        next.questDescription,
                        $"{sold} / {next.questTargetCount2}");
                }
            }
            return new Goal(
                $"Купи кофемашину «{next.displayName}»",
                $"{_economy.Balance} / {next.purchasePrice} ₽");
        }
    }
}
```

- [ ] **Step 3: Создать тесты `GoalTrackerServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class GoalTrackerServiceTests
    {
        private GameContent _content;
        private MachineTierDefinition _t1, _t2;
        private RecipeDefinition _espresso, _americano, _cappuccino;

        [SetUp]
        public void Setup()
        {
            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;
            _t2.purchasePrice = 1500;
            _t2.displayName = "Бариста";
            _t2.questDescription = "Продай 10 американо";
            _t2.questTargetCount1 = 10;

            _espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            _espresso.id = "espresso";
            _espresso.requiredMachineTier = _t1;

            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";
            _americano.displayName = "Американо";
            _americano.recipePurchasePrice = 100;
            _americano.requiredMachineTier = _t1;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.displayName = "Капучино";
            _cappuccino.recipePurchasePrice = 500;
            _cappuccino.requiredMachineTier = _t2;

            _t2.questTargetRecipe1 = _americano;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.machineTiers.AddRange(new[] { _t1, _t2 });
            _content.recipes.AddRange(new[] { _espresso, _americano, _cappuccino });
        }

        private GoalTrackerService MakeService(GameState state)
        {
            var eco = new EconomyService(state);
            var quests = new QuestService(state);
            var machine = new MachineService(state, _content, eco, quests);
            return new GoalTrackerService(state, _content, eco, quests, machine);
        }

        [Test]
        public void FirstGoal_IsBuyAmericano()
        {
            var state = new GameState();
            state.unlockedRecipeIds.Add("espresso");
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("Американо", goal.Description);
        }

        [Test]
        public void AfterAmericano_GoalIsT2Quest()
        {
            var state = new GameState();
            state.unlockedRecipeIds.AddRange(new[] { "espresso", "americano" });
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("10 американо", goal.Description);
        }

        [Test]
        public void AfterQuestSatisfied_GoalIsBuyT2()
        {
            var state = new GameState { balance = 0 };
            state.unlockedRecipeIds.AddRange(new[] { "espresso", "americano" });
            var quests = new QuestService(state);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("Бариста", goal.Description);
            StringAssert.Contains("/ 1500 ₽", goal.ProgressLabel);
        }

        [Test]
        public void AllRecipesUnlocked_GoalIsFinal()
        {
            var state = new GameState { currentMachineTierIndex = 2 };
            state.unlockedRecipeIds.AddRange(new[]
            {
                "espresso", "americano", "cappuccino"
            });
            // ВНИМАНИЕ: в setUp у нас только эти три. Для полноты тестируем что
            // когда все из orderedIds, что есть в content.recipes, открыты — Final.
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            Assert.IsTrue(goal.IsFinal);
        }
    }
}
```

- [ ] **Step 4: Run all tests — зелёные**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): GoalTrackerService computes next progression goal"
```

---

## Task 9: `SaveService` + тесты

**Files:**
- Create: `Assets/Scripts/Save/SaveService.cs`
- Create: `Assets/Tests/EditMode/SaveServiceTests.cs`

JSON в PlayerPrefs. Простой как пробка.

- [ ] **Step 1: Создать `SaveService.cs`**

В `Assets/Scripts/Save/`:

```csharp
using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.Save
{
    /// Сохраняет/загружает GameState в PlayerPrefs как JSON.
    /// В WebGL PlayerPrefs мапится на IndexedDB браузера (Telegram-клиент кэширует).
    public class SaveService
    {
        public const string Key = "DrinkitGame.Save.v1";

        /// Сохранить состояние в PlayerPrefs (синхронно).
        public void Save(GameState state)
        {
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        /// Загрузить состояние. null если сейв ещё не сделан.
        public GameState Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            string json = PlayerPrefs.GetString(Key);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<GameState>(json);
        }

        /// Удалить сейв (полезно для тестов или сброса прогресса).
        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
```

- [ ] **Step 2: Создать тесты `SaveServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Save;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class SaveServiceTests
    {
        private SaveService _save;

        [SetUp]
        public void Setup()
        {
            _save = new SaveService();
            _save.Clear(); // чистим перед каждым тестом
        }

        [TearDown]
        public void TearDown()
        {
            _save.Clear();
        }

        [Test]
        public void Load_ReturnsNull_WhenNoSave()
        {
            Assert.IsNull(_save.Load());
        }

        [Test]
        public void Save_ThenLoad_RoundTripsBalance()
        {
            var state = new GameState { balance = 1234, reputation = 4.2f };
            _save.Save(state);
            var loaded = _save.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1234, loaded.balance);
            Assert.AreEqual(4.2f, loaded.reputation, 0.0001f);
        }

        [Test]
        public void Save_PreservesInventoryAndUnlockedRecipes()
        {
            var state = new GameState();
            state.balance = 500;
            state.inventory.Add(new InventorySlot("beans", 25));
            state.inventory.Add(new InventorySlot("milk_oat", 8));
            state.unlockedRecipeIds.Add("espresso");
            state.unlockedRecipeIds.Add("americano");
            _save.Save(state);

            var loaded = _save.Load();
            Assert.AreEqual(2, loaded.inventory.Count);
            Assert.AreEqual("beans", loaded.inventory[0].productId);
            Assert.AreEqual(25, loaded.inventory[0].count);
            CollectionAssert.AreEqual(
                new[] { "espresso", "americano" },
                loaded.unlockedRecipeIds);
        }

        [Test]
        public void Clear_RemovesSave()
        {
            _save.Save(new GameState { balance = 100 });
            _save.Clear();
            Assert.IsNull(_save.Load());
        }
    }
}
```

- [ ] **Step 3: Run all tests — зелёные**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests/EditMode && git commit -m "feat(save): SaveService persists GameState as JSON in PlayerPrefs"
```

---

## Task 10: `GameStateManager` — единственный MonoBehaviour-корень

**Files:**
- Create: `Assets/Scripts/Core/GameStateManager.cs`

Собирает все сервисы вместе. Создаёт `GameState` на старте (загружая из сейва или с нуля), создаёт каждый сервис, подписывает `SaveService` на их события. Висит на `GameRoot`.

- [ ] **Step 1: Создать `GameStateManager.cs`**

```csharp
using DrinkitGame.Data;
using DrinkitGame.Save;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Корневой MonoBehaviour. Создаёт GameState, сервисы, подписывает Save.
    /// Висит на GameObject 'GameRoot' в сцене Main.
    public class GameStateManager : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Корневой GameContent.asset со всеми SO-данными.")]
        public GameContent content;

        // Открытые ссылки на сервисы (UI будет их подписывать в Phase 4).
        public GameState State { get; private set; }
        public EconomyService Economy { get; private set; }
        public InventoryService Inventory { get; private set; }
        public ReputationService Reputation { get; private set; }
        public QuestService Quests { get; private set; }
        public RecipeService Recipes { get; private set; }
        public MachineService Machine { get; private set; }
        public GoalTrackerService GoalTracker { get; private set; }
        public SaveService Save { get; private set; }

        private void Awake()
        {
            if (content == null)
            {
                Debug.LogError("[GameStateManager] GameContent не назначен в инспекторе!");
                return;
            }

            Save = new SaveService();
            State = Save.Load() ?? CreateFreshState();

            Economy = new EconomyService(State);
            Inventory = new InventoryService(State);
            Reputation = new ReputationService(State);
            Quests = new QuestService(State);
            Recipes = new RecipeService(State, content, Economy, Quests);
            Machine = new MachineService(State, content, Economy, Quests);
            GoalTracker = new GoalTrackerService(State, content, Economy, Quests, Machine);

            // Гарантируем что стартовый рецепт открыт (даже если в сейве пропал почему-то)
            Recipes.EnsureStarterUnlocked();

            // Подписываем сохранение на любые изменения
            Economy.BalanceChanged += _ => Save.Save(State);
            Inventory.StockChanged += (_, __) => Save.Save(State);
            Reputation.ReputationChanged += _ => Save.Save(State);
            Quests.CountChanged += (_, __) => Save.Save(State);
            Recipes.RecipeUnlocked += _ => Save.Save(State);
            Machine.Upgraded += _ => Save.Save(State);

            Debug.Log(
                $"[GameStateManager] Start. " +
                $"Balance={Economy.Balance}, Reputation={Reputation.Reputation:F1}, " +
                $"MachineT={Machine.CurrentTierIndex}, " +
                $"UnlockedRecipes={string.Join(",", State.unlockedRecipeIds)}, " +
                $"Beans={Inventory.GetStock("beans")}");

            var goal = GoalTracker.CurrentGoal();
            Debug.Log($"[GoalTracker] {goal.Description} — {goal.ProgressLabel}");
        }

        private GameState CreateFreshState()
        {
            var state = new GameState
            {
                balance = content.starterBalance,
                reputation = 5f,
                currentMachineTierIndex = content.starterMachineTier?.tierIndex ?? 1
            };
            if (content.starterRecipe != null)
                state.unlockedRecipeIds.Add(content.starterRecipe.id);
            if (content.starterBeansStock > 0)
            {
                // Найдём продукт с id='beans' (или возьмём первый Beans-категории)
                foreach (var p in content.products)
                {
                    if (p.id == "beans")
                    {
                        state.inventory.Add(new InventorySlot(p.id, content.starterBeansStock));
                        break;
                    }
                }
            }
            return state;
        }

        /// Утилитный метод для сброса (вызывается из тестов и из debug-меню в будущем).
        public void ResetProgress()
        {
            Save.Clear();
            State = CreateFreshState();
            Debug.Log("[GameStateManager] Прогресс сброшен.");
        }
    }
}
```

- [ ] **Step 2: Дождаться компиляции (Console чистая)**

- [ ] **Step 3: Прицепить компонент на `GameRoot` в сцене**

В Unity:
1. Открой сцену `Main` (если не открыта)
2. В Hierarchy выбери `GameRoot`
3. В Inspector → `Add Component` → набери `Game State Manager` → выбери и нажми Enter
4. У компонента в поле `Content` перетащи `Assets/Data/GameContent.asset`

- [ ] **Step 4: Сохранить сцену**

`Cmd+S`.

- [ ] **Step 5: Запустить Play и проверить Console**

Нажми ▶. В Console должна появиться примерно такая строка:
```
[GameStateManager] Start. Balance=0, Reputation=5.0, MachineT=1, UnlockedRecipes=espresso, Beans=10
[GoalTracker] Купи рецепт «Американо» — 0 / 100 ₽
```

Если есть **красные ошибки** — копируй текст и пиши.

Останови Play (▶ ещё раз).

- [ ] **Step 6: Проверить что состояние сохранилось**

Запусти Play ещё раз. В Console должны быть **те же значения**. PlayerPrefs сохранил.

(При желании можно вручную поднять balance — открой Inspector компонента GameStateManager в Play mode, но в прототипе это нам пока не нужно.)

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Core Assets/Scenes/Main.unity && git commit -m "feat(core): GameStateManager root MonoBehaviour wiring all services"
```

---

## Task 11: Финальная сверка фазы

- [ ] **Step 1: Все тесты зелёные**

Test Runner → EditMode → Run All. Сколько тестов всего:
- 1 SmokeTest
- 10 GameContentIntegrity
- 6 EconomyService
- 8 InventoryService
- 6 ReputationService
- 3 QuestService
- 8 RecipeService
- 7 MachineService
- 4 GoalTracker
- 4 SaveService

Итого ~57 тестов. Все зелёные.

- [ ] **Step 2: Console чистая**

При нажатии Play — только наши Debug.Log'и, никаких красных и жёлтых.

- [ ] **Step 3: Сейв работает**

Запусти, останови, запусти снова — состояние сохраняется.

- [ ] **Step 4: Git log проверка**

```bash
git log --oneline | head -15
```

Должно быть 10 коммитов Phase 3 (один на каждый сервис + GameStateManager + финал).

---

## Self-Review

После прохождения:
1. ✅ 10 файлов в `Assets/Scripts/Core/` (GameState, 7 сервисов, GameStateManager, Goal)
2. ✅ 1 файл в `Assets/Scripts/Save/` (SaveService)
3. ✅ 8 файлов тестов в `Assets/Tests/EditMode/` для каждого сервиса
4. ✅ ~57 зелёных тестов
5. ✅ `GameStateManager` висит на `GameRoot` в сцене с подключённым `GameContent`
6. ✅ Play → Console показывает стартовое состояние и текущий goal
7. ✅ Между запусками состояние сохраняется

**Готово → пиши `Phase 3 done`. Дальше Phase 4: Main Screen UI (топбар, слоты заказов, кнопки магазина и колеса, отображение машины).**

---

## Что НЕ делаем в этой фазе (anti-scope)

- ❌ Никакого UI — все сервисы безголовые. UI будет в Phase 4.
- ❌ Никакой логики заказов / клиентов / готовки — это Phases 5-8.
- ❌ Никакого спавна заказов — Phase 5.
- ❌ Никакой логики колеса — будет Phase 9.
- ❌ Никаких бустов (DoubleNextOrder, DiscountVoucher применяется только в RecipeService; колесо не выдаёт пока).
- ❌ Не используем Play Mode тесты — все тесты в Edit Mode.
