# Phase 11 — Save Persisted Orders + Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Закрыть последние пробелы прототипа: **активные заказы переживают рестарт** (был "Капучино со 120 сек терпения" → закрыл TG → открыл → видишь его на той же секунде); **таймеры не тикают пока игрок вне окна** (модель "пауза при выходе"); финальная балансировка экономики на плейтесте.

**Architecture:**
- `PersistedOrder` POCO — снимок заказа по id'шкам (без SO-ссылок).
- `OrderService.SerializeToState(GameState)` / `RestoreFromState(GameState, GameContent)`.
- Все save-подписки в `GameStateManager` оборачиваются в helper `SaveAll()`, который сначала зовёт `Orders.SerializeToState`.
- `OrderServiceTicker` ставится на паузу через `OnApplicationFocus/OnApplicationPause`.
- Балансировка — ручной плейтест с чек-листом.

**Tech Stack:** C# 9 · Unity 2022.3 · NUnit.

**Конец фазы:** Полный цикл играбелен, состояние переживает рестарт, таймеры паузятся в фоне. Прототип готов к показу.

---

## Task 1: `PersistedOrder` + поле в `GameState`

**Files:**
- Modify: `Assets/Scripts/Core/GameState.cs`

- [ ] **Step 1: Добавить класс `PersistedOrder`**

В `GameState.cs` после класса `RecipeSoldCount` добавь:

```csharp
    /// Снимок активного заказа для сохранения между сессиями (по id-шкам, без SO-ссылок).
    [Serializable]
    public class PersistedOrder
    {
        public string recipeId;
        public string milkId;
        public string creamId;
        public string syrupId;
        public string toppingId;
        public bool isToGo;
        public float remainingPatience;
        public int slotIndex;
    }
```

В `GameState` добавь поле:

```csharp
        [Tooltip("Активные заказы в слотах — снимок для сохранения.")]
        public List<PersistedOrder> persistedOrders = new();
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Core/GameState.cs && git commit -m "feat(core): PersistedOrder POCO for cross-session orders"
```

---

## Task 2: `OrderService.SerializeToState` + `RestoreFromState`

**Files:**
- Modify: `Assets/Scripts/Core/OrderService.cs`

- [ ] **Step 1: Добавить методы**

Открой `OrderService.cs`. Перед закрывающим `}` класса добавь:

```csharp
        /// Записать текущие слоты в GameState.persistedOrders (стирая старый список).
        public void SerializeToState(GameState state)
        {
            state.persistedOrders.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                var order = _slots[i];
                if (order == null) continue;
                state.persistedOrders.Add(new PersistedOrder
                {
                    recipeId = order.recipe != null ? order.recipe.id : null,
                    milkId = order.milk != null ? order.milk.id : null,
                    creamId = order.cream != null ? order.cream.id : null,
                    syrupId = order.syrup != null ? order.syrup.id : null,
                    toppingId = order.topping != null ? order.topping.id : null,
                    isToGo = order.isToGo,
                    remainingPatience = order.remainingPatience,
                    slotIndex = order.slotIndex
                });
            }
        }

        /// Восстановить заказы из снимка в GameState. Вызывается один раз после загрузки сейва.
        public void RestoreFromState(GameState state, DrinkitGame.Data.GameContent content)
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = null;
            if (state.persistedOrders == null) return;

            foreach (var p in state.persistedOrders)
            {
                if (string.IsNullOrEmpty(p.recipeId)) continue;
                if (p.slotIndex < 0 || p.slotIndex >= SlotCount) continue;

                var order = new Order
                {
                    recipe = FindRecipe(content, p.recipeId),
                    milk = FindProduct(content, p.milkId),
                    cream = FindProduct(content, p.creamId),
                    syrup = FindProduct(content, p.syrupId),
                    topping = FindProduct(content, p.toppingId),
                    isToGo = p.isToGo,
                    remainingPatience = p.remainingPatience,
                    slotIndex = p.slotIndex
                };
                if (order.recipe == null) continue; // рецепт удалили — пропустим
                _slots[p.slotIndex] = order;
                OrderSpawned?.Invoke(order);
            }
        }

        private static DrinkitGame.Data.RecipeDefinition FindRecipe(DrinkitGame.Data.GameContent content, string id)
        {
            if (string.IsNullOrEmpty(id) || content == null) return null;
            foreach (var r in content.recipes) if (r != null && r.id == id) return r;
            return null;
        }

        private static DrinkitGame.Data.ProductDefinition FindProduct(DrinkitGame.Data.GameContent content, string id)
        {
            if (string.IsNullOrEmpty(id) || content == null) return null;
            foreach (var p in content.products) if (p != null && p.id == id) return p;
            return null;
        }
```

- [ ] **Step 2: Тесты — `Assets/Tests/EditMode/OrderServicePersistenceTests.cs`**

```csharp
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
            var s1 = new OrderService(_generator, _reputation, new System.Random(1));
            for (int i = 0; i < 30 && s1.GetSlot(0) == null; i++) s1.Tick(1f);
            Assert.IsNotNull(s1.GetSlot(0));

            // Запомним детали
            var original = s1.GetSlot(0);
            float patience = original.remainingPatience;

            // Сохраняем в state
            s1.SerializeToState(_state);
            Assert.AreEqual(1, _state.persistedOrders.Count);

            // Восстанавливаем в новый сервис
            var s2 = new OrderService(_generator, _reputation, new System.Random(1));
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

            var s = new OrderService(_generator, _reputation, new System.Random(1));
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

            var s = new OrderService(_generator, _reputation, new System.Random(1));
            s.RestoreFromState(_state, _content);

            var restored = s.GetSlot(1);
            Assert.IsNotNull(restored);
            Assert.AreEqual("milk_oat", restored.milk.id);
            Assert.IsTrue(restored.isToGo);
            Assert.AreEqual(250f, restored.remainingPatience, 0.001f);
        }
    }
}
```

- [ ] **Step 3: Run All, Commit**

```bash
git add Assets/Scripts/Core/OrderService.cs Assets/Tests/EditMode/OrderServicePersistenceTests.cs Assets/Tests/EditMode/OrderServicePersistenceTests.cs.meta && git commit -m "feat(core): OrderService persistence (Serialize/Restore via PersistedOrder)"
```

---

## Task 3: Подключить персистенцию в `GameStateManager`

**Files:**
- Modify: `Assets/Scripts/Core/GameStateManager.cs`

- [ ] **Step 1: Добавить helper `SaveAll()` и подцепить**

В классе `GameStateManager` добавь приватный метод (перед `OnDestroy`):

```csharp
        private void SaveAll()
        {
            if (Orders != null) Orders.SerializeToState(State);
            if (Save != null) Save.Save(State);
        }
```

В `Awake`, в блоке подписок сейва, **замени** строки вида `Save.Save(State)` на `SaveAll()`:

Было:
```csharp
            Economy.BalanceChanged += _ => Save.Save(State);
            Inventory.StockChanged += (_, __) => Save.Save(State);
            Reputation.ReputationChanged += _ => Save.Save(State);
            Quests.CountChanged += (_, __) => Save.Save(State);
            Recipes.RecipeUnlocked += _ => Save.Save(State);
            Machine.Upgraded += _ => Save.Save(State);
            Orders.OrderSpawned += _ => Save.Save(State);
            Orders.OrderAbandoned += _ => Save.Save(State);
            Wheel.TokensChanged += _ => Save.Save(State);
```

Стало:
```csharp
            Economy.BalanceChanged += _ => SaveAll();
            Inventory.StockChanged += (_, __) => SaveAll();
            Reputation.ReputationChanged += _ => SaveAll();
            Quests.CountChanged += (_, __) => SaveAll();
            Recipes.RecipeUnlocked += _ => SaveAll();
            Machine.Upgraded += _ => SaveAll();
            Orders.OrderSpawned += _ => SaveAll();
            Orders.OrderAbandoned += _ => SaveAll();
            Orders.OrderRemoved += _ => SaveAll();
            Wheel.TokensChanged += _ => SaveAll();
```

- [ ] **Step 2: Восстановление заказов после создания сервисов**

В `Awake`, после `Recipes.EnsureStarterUnlocked();` добавь:

```csharp
            // Восстанавливаем заказы из сейва
            Orders.RestoreFromState(State, content);
```

- [ ] **Step 3: Auto-save на закрытие приложения**

Добавь в `GameStateManager`:

```csharp
        private void OnApplicationQuit() => SaveAll();
        private void OnApplicationPause(bool pause) { if (pause) SaveAll(); }
```

- [ ] **Step 4: Compile, Commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs && git commit -m "feat(core): wire order persistence and auto-save on app pause/quit"
```

---

## Task 4: Пауза тика заказов когда игра в фоне

**Files:**
- Modify: `Assets/Scripts/Core/OrderServiceTicker.cs`

- [ ] **Step 1: Добавить focus/pause обработчики**

Полностью замени содержимое `OrderServiceTicker.cs`:

```csharp
using UnityEngine;

namespace DrinkitGame.Core
{
    /// MonoBehaviour-обёртка вокруг OrderService.Tick().
    /// Не тикает когда приложение в фоне (модель "пауза при выходе").
    public class OrderServiceTicker : MonoBehaviour
    {
        private bool _paused;

        private void Update()
        {
            if (_paused) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.Orders == null) return;
            gsm.Orders.Tick(Time.deltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _paused = !hasFocus;
        }

        private void OnApplicationPause(bool pause)
        {
            _paused = pause;
        }
    }
}
```

- [ ] **Step 2: Тест паузы (вручную)**

1. Запусти Play
2. Дождись появления заказа, видишь таймер тикает
3. Альт+Таб в другое окно (на macOS Cmd+Tab) — Unity потеряет фокус
4. Возвращайся через 30 сек — таймер должен показывать **то же значение** (плюс-минус, пауза работает)
5. Если таймер уменьшился ровно на 30 сек — пауза НЕ сработала; проверь OnApplicationFocus

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/OrderServiceTicker.cs && git commit -m "feat(core): OrderServiceTicker pauses ticks when app loses focus"
```

---

## Task 5: Debug-панель статистики (опционально)

**Files:**
- Create: `Assets/Scripts/UI/DebugStatsOverlay.cs`
- Modify: `Assets/Scenes/Main.unity`

Маленький полупрозрачный текст в углу для плейтеста.

- [ ] **Step 1: `DebugStatsOverlay.cs`**

```csharp
using DrinkitGame.Core;
using TMPro;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Маленький оверлей со статистикой в углу — для отладки/плейтеста.
    public class DebugStatsOverlay : MonoBehaviour
    {
        public TMP_Text statsLabel;
        public bool visibleByDefault = true;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (!visibleByDefault && statsLabel != null) statsLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_gsm == null || statsLabel == null || !statsLabel.gameObject.activeSelf) return;
            statsLabel.text =
                $"Balance: {_gsm.Economy.Balance}₽\n" +
                $"Reputation: {_gsm.Reputation.Reputation:F1}\n" +
                $"Machine: T{_gsm.Machine.CurrentTierIndex}\n" +
                $"Orders done: {_gsm.State.totalOrdersCompleted}\n" +
                $"Wheel tokens: {_gsm.Wheel.Tokens}";
        }
    }
}
```

- [ ] **Step 2: Создать TMP в сцене**

В Hierarchy → `Canvas` → правый клик → `UI → Text - TextMeshPro`. Переименуй в `DebugStats`.

- RectTransform: top-right anchor, anchored X=-12, Y=-150, W=200, H=120
- Text: `(stats)`
- Font Size: 11
- Color: HEX `666666`
- Alignment: Right + Top
- Add Component → `DebugStatsOverlay`. Поле `Stats Label` = сам этот объект.

(Если не хочешь видеть в финальной сборке — деактивируй DebugStats галочкой.)

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/DebugStatsOverlay.cs Assets/Scripts/UI/DebugStatsOverlay.cs.meta Assets/Scenes/Main.unity && git commit -m "feat(ui): DebugStatsOverlay shows runtime stats in corner"
```

---

## Task 6: Чек-лист балансировки (ручной плейтест)

Этот таск — **не код**, а ручная проверка экономики.

- [ ] **Step 1: Полный прогон с нуля**

Wipe Save → Play. Засеки время.

Прицелье из спека (раздел 10.3):
- 0–3 мин: онбординг + первая выдача
- 3–10 мин: накопить 100₽, купить американо
- 10–25 мин: продать 10 американо + накопить 1500₽, купить T2
- 25–45 мин: открыть капучино/латте/какао/раф
- 45–55 мин: квест 5+5, копить 5000₽, купить T3
- 55–60 мин: фильтр + матча, "финал"

Если получается значительно быстрее (< 40 мин) или медленнее (> 90 мин) — крути цены в `GameContent` ассетах:
- Дороже хочется → подними `recipePurchasePrice` рецептов, `purchasePrice` тиров
- Дешевле хочется → опусти их

Все цены — в `Assets/Data/Recipes/*.asset` и `Assets/Data/Machines/*.asset`. После правок Cmd+S не нужен (они автосохранятся), просто перезапусти Play.

- [ ] **Step 2: Поиграй и зафиксируй**

В таблицу заноси:
| Этап | Целевое время | Фактическое | Что подкрутить |
|---|---|---|---|
| До американо | 7 мин | ? | |
| До T2 | 15 мин | ? | |
| До всех средних | 20 мин | ? | |
| До T3 | 10 мин | ? | |
| До финала | 5 мин | ? | |
| Всего | 60 мин | ? | |

- [ ] **Step 3: Коммит всех правок ассетов**

```bash
git add Assets/Data && git commit -m "balance: tune economy after playtest"
```

(Если ничего не правил — Commit пропусти.)

---

## Task 7: Финальный смок и тэг

- [ ] **Step 1: Все тесты зелёные**

Run All. ~115 зелёных тестов (Phase 1-10 + 3 OrderServicePersistence).

- [ ] **Step 2: Полный пробег**

- Новая игра (wipe) → онбординг → 1 час игры → все 8 рецептов открыты, T3 куплена
- Никаких NullReferenceException в Console
- Сохранение между запусками работает
- Пауза в фоне работает

- [ ] **Step 3: Финальный тэг релиза**

```bash
git tag -a v0.1.0-prototype -m "Phase 11 — playable prototype complete"
git log --oneline | head -20
```

---

## Self-Review

После прохождения:
1. ✅ Активные заказы переживают рестарт
2. ✅ Таймеры паузятся в фоне
3. ✅ Auto-save на закрытии TG
4. ✅ Debug stats overlay
5. ✅ Балансировка пройдена в плейтесте
6. ✅ Все тесты зелёные
7. ✅ Тэг v0.1.0-prototype

**Готово → Phase 12: подмена плейсхолдеров на финальный арт.**

---

## Common Pitfalls

**1. `Restore` восстанавливает заказы НО рисует поверх онбординга**
Это норм — заказы спавнятся, OrderSpawned event вызывается, контроллер UI рендерит. Если онбординг ещё активен, заказ просто стоит за оверлеем, по выходу из онбординга станет видимым.

**2. После рестарта заказы потеряны**
Скорее всего `SaveAll()` не вызывался перед закрытием. Проверь:
- `OnApplicationQuit` и `OnApplicationPause(true)` в `GameStateManager` вызывают `SaveAll`
- `SaveAll` вызывает `Orders.SerializeToState(State)` перед `Save.Save(State)`

**3. Таймеры тикают даже когда Unity в фоне**
Проверь `OrderServiceTicker.OnApplicationFocus` — `_paused` должен становиться `true`. На macOS приложение может не терять focus при Cmd+Tab если Unity Editor в фокусе. В сбилженной игре будет работать корректнее.

**4. Дубликаты заказов после рестарта**
Возможно `RestoreFromState` вызывается дважды. Проверь что вызов один — в `GameStateManager.Awake` после создания сервисов.

**5. Тест `OrderServicePersistenceTests.SerializeRestore_RoundTripsOneOrder` падает**
Проверь что после `Tick(1f)` действительно появился заказ (Random(1) может его задержать). Если падает на `Assert.IsNotNull(s1.GetSlot(0))` — повысь число тиков с 30 до 60.

**6. Сборка не открывается / Player crash при OnApplicationQuit**
Не вызывай UI-операции в OnApplicationQuit. Только сейв.
