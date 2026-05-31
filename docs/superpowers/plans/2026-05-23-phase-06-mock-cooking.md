# Phase 6 — Mock Cooking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Закрыть **базовый игровой цикл**. Тап по заказу → открывается Cooking-экран (плейсхолдер с одной кнопкой "Выдать") → нажал "Выдать" → списываются ингредиенты, начисляется чек по формуле, увеличивается счётчик квеста, возвращаемся на главный. Появляется поп-ап "OrderResult" с разбивкой суммы.

**Architecture:**
- `OrderResolution` (POCO) + `OrderResolutionService` (pure C#) — считает финальный чек и атомарно завершает заказ (ингредиенты, деньги, квест).
- `UIRouter` (MonoBehaviour) — переключает панели Main / Cooking, паблик API `OpenMain()` / `OpenCooking(Order)` / `OpenOrderResult(OrderResolution)`.
- `CookingScreenPanel` — новая UI-панель внутри Canvas, скрыта по умолчанию.
- `CookingScreenController` — рендерит детали заказа + слушает "Выдать" кнопку.
- `OrderResultPopup` — небольшая модалка с базой/бонусами/итогом.
- `OrderSlotsController` уже есть из Phase 5 — поменяем `Debug.Log` на `UIRouter.Instance.OpenCooking(order)`.

**Quality в Phase 6 = 100 (нет мини-игр).** Speed bonus вычисляется реально по `300 - remainingPatience`.

**Tech Stack:** C# 9 · Unity 2022.3 · uGUI · TMPro · NUnit (Edit Mode).

**Конец фазы:** Тап заказа → новый экран → "Выдать" → +N₽ на балансе → возврат на главный с поп-апом → следующий заказ в слоте. Counter "Продано американо" в квесте растёт. Можно реально дойти до апгрейда T2.

---

## Task 1: `OrderResolution` POCO + `OrderResolutionService`

**Files:**
- Create: `Assets/Scripts/Core/OrderResolution.cs`
- Create: `Assets/Scripts/Core/OrderResolutionService.cs`

- [ ] **Step 1: `OrderResolution.cs`**

В `Assets/Scripts/Core/`:

```csharp
using System;

namespace DrinkitGame.Core
{
    /// Результат выдачи заказа: что заплатили и почему.
    /// Используется OrderResultPopup для визуализации.
    [Serializable]
    public class OrderResolution
    {
        public string recipeId;
        public string recipeDisplayName;
        public int basePrice;              // База: цена напитка + надбавки модификаторов
        public float speedMultiplier;      // +0.3 / 0 / -0.1
        public float qualityMultiplier;    // +0.2 / 0 / -0.1
        public float tierBonusMultiplier;  // 0 или 0.1 (только T3)
        public bool doubleApplied;         // был ли использован буст "следующий заказ ×2"
        public int finalPayout;            // итоговая выплата

        // Категория скорости и качества — для UI текста
        public string speedLabel;          // "быстро" / "норм" / "медленно"
        public string qualityLabel;        // "отлично" / "норм" / "плохо"
    }
}
```

- [ ] **Step 2: `OrderResolutionService.cs`**

```csharp
using System;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Атомарно завершает заказ: считает чек, списывает ингредиенты, начисляет деньги,
    /// записывает продажу в квесты.
    public class OrderResolutionService
    {
        public const float SpeedFastThreshold = 60f;
        public const float SpeedNormalThreshold = 180f;
        public const float QualityHighThreshold = 80f;
        public const float QualityLowThreshold = 50f;
        public const float SpeedBonusFast = 0.30f;
        public const float SpeedBonusSlow = -0.10f;
        public const float QualityBonusHigh = 0.20f;
        public const float QualityBonusLow = -0.10f;

        private readonly GameState _state;
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly QuestService _quests;
        private readonly MachineService _machine;

        public OrderResolutionService(
            GameState state,
            EconomyService economy,
            InventoryService inventory,
            QuestService quests,
            MachineService machine)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// Завершить выдачу заказа.
        /// quality — 0..100 (в Phase 6 всегда 100; в Phase 8 — среднее мини-игр).
        /// elapsedSeconds — сколько секунд клиент ждал (Patience - remainingPatience).
        public OrderResolution Complete(Order order, float quality, float elapsedSeconds)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            // 1. База
            int basePrice = order.recipe.basePrice;
            if (order.milk != null) basePrice += order.milk.sellMarkup;
            if (order.syrup != null) basePrice += order.syrup.sellMarkup;
            if (order.topping != null) basePrice += order.topping.sellMarkup;

            // 2. Бонусы
            (float speedMult, string speedLabel) = ComputeSpeed(elapsedSeconds);
            (float qualityMult, string qualityLabel) = ComputeQuality(quality);
            float tierMult = (_machine.CurrentTier?.checkBonusPercent ?? 0) / 100f;

            float totalMultiplier = 1f + speedMult + qualityMult + tierMult;
            int payout = Mathf.Max(0, Mathf.RoundToInt(basePrice * totalMultiplier));

            // 3. Буст "×2 на следующий заказ" если активен
            bool doubleApplied = _state.hasDoubleNextOrderBuff;
            if (doubleApplied)
            {
                payout *= 2;
                _state.hasDoubleNextOrderBuff = false;
            }

            // 4. Расход ингредиентов
            ConsumeIngredients(order);

            // 5. Деньги
            if (payout > 0) _economy.Earn(payout);

            // 6. Квест-счётчик
            _quests.RecordSale(order.recipe.id);

            return new OrderResolution
            {
                recipeId = order.recipe.id,
                recipeDisplayName = order.recipe.displayName,
                basePrice = basePrice,
                speedMultiplier = speedMult,
                qualityMultiplier = qualityMult,
                tierBonusMultiplier = tierMult,
                doubleApplied = doubleApplied,
                finalPayout = payout,
                speedLabel = speedLabel,
                qualityLabel = qualityLabel
            };
        }

        private static (float mult, string label) ComputeSpeed(float elapsed)
        {
            if (elapsed < SpeedFastThreshold) return (SpeedBonusFast, "быстро");
            if (elapsed < SpeedNormalThreshold) return (0f, "норм");
            return (SpeedBonusSlow, "медленно");
        }

        private static (float mult, string label) ComputeQuality(float quality)
        {
            if (quality > QualityHighThreshold) return (QualityBonusHigh, "отлично");
            if (quality < QualityLowThreshold) return (QualityBonusLow, "плохо");
            return (0f, "норм");
        }

        private void ConsumeIngredients(Order order)
        {
            foreach (var ing in order.recipe.fixedIngredients)
            {
                if (ing.product == null) continue;
                _inventory.TryConsume(ing.product.id, ing.amount);
            }
            if (order.milk != null) _inventory.TryConsume(order.milk.id, 1);
            if (order.cream != null) _inventory.TryConsume(order.cream.id, 1);
            if (order.syrup != null) _inventory.TryConsume(order.syrup.id, 1);
            if (order.topping != null) _inventory.TryConsume(order.topping.id, 1);
            if (order.isToGo)
            {
                foreach (var p in ResolveCupProductFromState(order))
                    _inventory.TryConsume(p.id, 1);
            }
        }

        private System.Collections.Generic.IEnumerable<ProductDefinition> ResolveCupProductFromState(Order order)
        {
            // У нас один тип "с собой" стакана. Найдём первый продукт категории Cup в инвентаре
            // (если их когда-нибудь будет несколько — берём первый с id "cup_takeaway").
            // Поскольку OrderResolutionService не знает про GameContent, мы возвращаем
            // фиксированный id. Логично — id вшит и стабильный.
            yield return new ProductDefinition { id = "cup_takeaway" } ;
            // ВНИМАНИЕ: создавая ScriptableObject через new — обычно плохо; здесь только
            // для передачи id в TryConsume(string), сам объект не используется как актив.
        }
    }
}
```

> ℹ️ Стакан «с собой» списываем напрямую по строковому id `"cup_takeaway"` через `InventoryService.TryConsume(string, int)` — без создания временных `ScriptableObject`. У нас один тип стакана, id стабильный.

- [ ] **Step 3: Тесты — `Assets/Tests/EditMode/OrderResolutionServiceTests.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class OrderResolutionServiceTests
    {
        private GameContent _content;
        private GameState _state;
        private EconomyService _economy;
        private InventoryService _inventory;
        private QuestService _quests;
        private MachineService _machine;
        private OrderResolutionService _resolver;

        private ProductDefinition _beans, _milkCow, _milkOat, _syrupVanilla, _cupTakeaway;
        private RecipeDefinition _cappuccino;
        private MachineTierDefinition _t1, _t3;

        [SetUp]
        public void Setup()
        {
            _beans = Make("beans", ProductCategory.Beans, 15, 0);
            _milkCow = Make("milk_cow", ProductCategory.Milk, 25, 0);
            _milkOat = Make("milk_oat", ProductCategory.Milk, 60, 60);
            _syrupVanilla = Make("syrup_vanilla", ProductCategory.Syrup, 30, 40);
            _cupTakeaway = Make("cup_takeaway", ProductCategory.Cup, 15, 0);

            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1; _t1.checkBonusPercent = 0;

            _t3 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t3.tierIndex = 3; _t3.checkBonusPercent = 10;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.displayName = "Капучино";
            _cappuccino.basePrice = 250;
            _cappuccino.requiredMachineTier = _t1;
            _cappuccino.fixedIngredients.Add(new IngredientAmount(_beans, 1));

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.products.AddRange(new[] { _beans, _milkCow, _milkOat, _syrupVanilla, _cupTakeaway });
            _content.machineTiers.AddRange(new[] { _t1, _t3 });
            _content.recipes.Add(_cappuccino);

            _state = new GameState { balance = 0, currentMachineTierIndex = 1 };
            _state.unlockedRecipeIds.Add("cappuccino");
            _economy = new EconomyService(_state);
            _inventory = new InventoryService(_state);
            _quests = new QuestService(_state);
            _machine = new MachineService(_state, _content, _economy, _quests);
            _resolver = new OrderResolutionService(_state, _economy, _inventory, _quests, _machine);

            _inventory.Add("beans", 5);
            _inventory.Add("milk_oat", 5);
            _inventory.Add("syrup_vanilla", 5);
            _inventory.Add("cup_takeaway", 5);
        }

        private ProductDefinition Make(string id, ProductCategory cat, int price, int markup)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.id = id; p.category = cat; p.purchasePrice = price; p.sellMarkup = markup;
            return p;
        }

        private Order MakeOrder(ProductDefinition milk = null, ProductDefinition syrup = null, bool toGo = false)
        {
            return new Order
            {
                recipe = _cappuccino,
                milk = milk,
                syrup = syrup,
                isToGo = toGo,
                remainingPatience = OrderService.Patience,
                slotIndex = 0
            };
        }

        [Test]
        public void Complete_BasePrice_FromRecipeAndModifiers()
        {
            var order = MakeOrder(milk: _milkOat, syrup: _syrupVanilla);
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            // 250 + 60 (oat) + 40 (syrup) = 350
            Assert.AreEqual(350, res.basePrice);
        }

        [Test]
        public void Complete_FastDelivery_GivesSpeedBonus()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            // < 60 сек → +0.3
            Assert.AreEqual(0.30f, res.speedMultiplier, 0.0001f);
            Assert.AreEqual("быстро", res.speedLabel);
        }

        [Test]
        public void Complete_SlowDelivery_GivesPenalty()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 240f);
            // > 180 сек → -0.1
            Assert.AreEqual(-0.10f, res.speedMultiplier, 0.0001f);
            Assert.AreEqual("медленно", res.speedLabel);
        }

        [Test]
        public void Complete_HighQuality_GivesBonus()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 95f, elapsedSeconds: 90f);
            Assert.AreEqual(0.20f, res.qualityMultiplier, 0.0001f);
        }

        [Test]
        public void Complete_T3_AddsTierBonus()
        {
            _state.currentMachineTierIndex = 3;
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            Assert.AreEqual(0.10f, res.tierBonusMultiplier, 0.0001f);
        }

        [Test]
        public void Complete_FullBonusStack_FinalPayout_Calculated()
        {
            _state.currentMachineTierIndex = 3;
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 95f, elapsedSeconds: 30f);
            // base=250, mult=1+0.3+0.2+0.1=1.6, payout=400
            Assert.AreEqual(250, res.basePrice);
            Assert.AreEqual(400, res.finalPayout);
        }

        [Test]
        public void Complete_DoubleOrderBuff_DoublesPayoutAndClearsBuff()
        {
            _state.hasDoubleNextOrderBuff = true;
            var order = MakeOrder();
            // quality=70 (диапазон 50-80) даёт qualityMultiplier=0, elapsed=90 даёт speedMultiplier=0
            // → base=250, mult=1.0, payout=250 × 2 = 500
            var res = _resolver.Complete(order, quality: 70f, elapsedSeconds: 90f);
            Assert.IsTrue(res.doubleApplied);
            Assert.AreEqual(500, res.finalPayout);
            Assert.IsFalse(_state.hasDoubleNextOrderBuff, "Буст должен быть потрачен");
        }

        [Test]
        public void Complete_ConsumesAllIngredients()
        {
            var order = MakeOrder(milk: _milkOat, syrup: _syrupVanilla, toGo: true);
            _resolver.Complete(order, 100f, 30f);
            Assert.AreEqual(4, _inventory.GetStock("beans"), "beans: 5 - 1 = 4");
            Assert.AreEqual(4, _inventory.GetStock("milk_oat"));
            Assert.AreEqual(4, _inventory.GetStock("syrup_vanilla"));
            Assert.AreEqual(4, _inventory.GetStock("cup_takeaway"));
        }

        [Test]
        public void Complete_AddsBalanceAndRecordsSale()
        {
            var order = MakeOrder();
            int balanceBefore = _economy.Balance;
            _resolver.Complete(order, 100f, 30f);
            Assert.Greater(_economy.Balance, balanceBefore);
            Assert.AreEqual(1, _quests.GetSoldCount("cappuccino"));
        }
    }
}
```

- [ ] **Step 4: Run All — все зелёные. Commit:**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode && git commit -m "feat(core): OrderResolutionService computes payout and finalizes order"
```

---

## Task 2: Подключить `OrderResolutionService` в `GameStateManager`

**Files:**
- Modify: `Assets/Scripts/Core/GameStateManager.cs`

- [ ] **Step 1: Добавить публичное свойство**

После `public OrderService Orders { get; private set; }` добавь:

```csharp
        public OrderResolutionService OrderResolution { get; private set; }
```

- [ ] **Step 2: Создать в `Awake`**

Сразу **после** строки `Orders = new OrderService(OrderGenerator, Reputation);` добавь:

```csharp
            OrderResolution = new OrderResolutionService(State, Economy, Inventory, Quests, Machine);
```

- [ ] **Step 3: Compile, Console чистая, commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs && git commit -m "feat(core): wire OrderResolutionService into GameStateManager"
```

---

## Task 3: `UIRouter` — переключение между Main / Cooking

**Files:**
- Create: `Assets/Scripts/UI/UIRouter.cs`
- Modify: `Assets/Scenes/Main.unity` (через Editor)

- [ ] **Step 1: `UIRouter.cs`**

В `Assets/Scripts/UI/`:

```csharp
using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Простой роутер между UI-панелями. Висит на Canvas или GameRoot.
    /// Singleton — UI компоненты находят его через Instance.
    public class UIRouter : MonoBehaviour
    {
        public static UIRouter Instance { get; private set; }

        [Header("Panels (root GameObjects)")]
        public GameObject mainScreenPanel;
        public GameObject cookingScreenPanel;
        public GameObject orderResultPopup;

        [Header("Optional cooking controller")]
        public CookingScreenController cookingController;
        public OrderResultPopupController resultPopupController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ShowMain();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowMain()
        {
            SetActive(mainScreenPanel, true);
            SetActive(cookingScreenPanel, false);
            SetActive(orderResultPopup, false);
        }

        public void OpenCooking(Order order)
        {
            if (cookingController != null) cookingController.Bind(order);
            SetActive(mainScreenPanel, false);
            SetActive(cookingScreenPanel, true);
            SetActive(orderResultPopup, false);
        }

        public void ShowOrderResult(OrderResolution resolution)
        {
            if (resultPopupController != null) resultPopupController.Show(resolution);
            SetActive(orderResultPopup, true);
        }

        public void HideOrderResult()
        {
            SetActive(orderResultPopup, false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
```

> Компилируется не сразу — ссылки `CookingScreenController` и `OrderResultPopupController` создадим в Task 4/5. Норм.

- [ ] **Step 2: Прицепить UIRouter в сцене**

Hierarchy → `Canvas` → Add Component → `UI Router`.

Не заполняй пока поля — сначала надо создать Cooking и OrderResult панели (Task 4-5).

- [ ] **Step 3: Не коммитим пока — продолжим после Task 5**

---

## Task 4: `CookingScreenPanel` + `CookingScreenController`

**Files:**
- Create: `Assets/Scripts/UI/CookingScreenController.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать панель в Canvas**

В Hierarchy → `Canvas` → правый клик → `UI → Panel`. Переименуй в `CookingScreenPanel`.

- RectTransform: stretch на весь Canvas (Anchor Presets → bottom-right Alt-click `stretch/stretch`, Left/Right/Top/Bottom = 0)
- Image → Color: HEX `F4F8FF` (чуть светлее главного фона)

Внутри `CookingScreenPanel` создай вертикальный лайаут вручную (без Layout Group для простоты):

1. `UI → Text - TextMeshPro`, переименуй в `Title`:
   - Text: `Готовка`
   - Font Size: `22`
   - Color: чёрный
   - Alignment: Center + Middle
   - RectTransform: top anchor, Top=60, Height=32, Left=12, Right=12

2. `UI → Text - TextMeshPro`, переименуй в `RecipeLabel`:
   - Text: `Эспрессо · тут` (плейсхолдер)
   - Font Size: `18`
   - Alignment: Center + Middle
   - RectTransform: top anchor, Top=110, Height=28, Left=12, Right=12

3. `UI → Text - TextMeshPro`, переименуй в `ModifiersLabel`:
   - Text: `на коровьем · без сиропа`
   - Font Size: `14`
   - Alignment: Center + Middle
   - RectTransform: top anchor, Top=148, Height=24, Left=12, Right=12

4. `UI → Text - TextMeshPro`, переименуй в `PatienceLabel`:
   - Text: `Терпение: 4:55`
   - Font Size: `14`
   - Color: HEX `5A8DDC`
   - Alignment: Center + Middle
   - RectTransform: top anchor, Top=180, Height=24, Left=12, Right=12

5. `UI → Button - TextMeshPro`, переименуй в `ServeButton`:
   - Внутри `Text (TMP)`: `Выдать`, Font Size 18, Bold, белый
   - Button → Image → Color: HEX `2D9F4E` (зелёный)
   - RectTransform: bottom anchor, Bottom=100 (выше TabBar), Height=60, Left=24, Right=24

6. `UI → Button - TextMeshPro`, переименуй в `CancelButton`:
   - Внутри `Text (TMP)`: `← Назад`, Font Size 14, чёрный
   - Button → Image → Color: HEX `EEEEEE`
   - RectTransform: top-left anchor, Top=20, Left=12, Width=90, Height=32

- [ ] **Step 2: `CookingScreenController.cs`**

В `Assets/Scripts/UI/`:

```csharp
using System.Text;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Контроллер mock-cooking экрана: показывает детали заказа + кнопка "Выдать".
    /// В Phase 8 будет полноценный пошаговый flow.
    public class CookingScreenController : MonoBehaviour
    {
        [Header("Labels")]
        public TMP_Text recipeLabel;
        public TMP_Text modifiersLabel;
        public TMP_Text patienceLabel;

        [Header("Buttons")]
        public Button serveButton;
        public Button cancelButton;

        private Order _order;

        private void Awake()
        {
            if (serveButton != null) serveButton.onClick.AddListener(OnServe);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        }

        /// Привязать заказ к экрану (вызывается UIRouter при открытии).
        public void Bind(Order order)
        {
            _order = order;
            if (order == null) return;

            if (recipeLabel != null)
                recipeLabel.text = $"{order.recipe.displayName} · {(order.isToGo ? "с собой" : "тут")}";

            if (modifiersLabel != null)
                modifiersLabel.text = BuildModifiersString(order);

            if (patienceLabel != null)
                patienceLabel.text = $"Терпение: {FormatTime(order.remainingPatience)}";
        }

        private void Update()
        {
            // Обновляем таймер терпения, пока экран открыт (заказ ушёл из слота, но мы держим референс)
            if (_order != null && patienceLabel != null)
            {
                _order.remainingPatience -= Time.deltaTime;
                if (_order.remainingPatience < 0) _order.remainingPatience = 0;
                patienceLabel.text = $"Терпение: {FormatTime(_order.remainingPatience)}";
            }
        }

        private void OnServe()
        {
            if (_order == null) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            // Мок-выдача: quality = 100, elapsedSeconds = Patience - remainingPatience
            float elapsed = OrderService.Patience - _order.remainingPatience;
            var resolution = gsm.OrderResolution.Complete(_order, quality: 100f, elapsedSeconds: elapsed);

            UIRouter.Instance.ShowOrderResult(resolution);
            // Сразу возвращаемся в Main (поп-ап рендерится поверх)
            UIRouter.Instance.ShowMain();
            UIRouter.Instance.ShowOrderResult(resolution); // показываем поверх Main
            _order = null;
        }

        private void OnCancel()
        {
            if (_order == null)
            {
                UIRouter.Instance.ShowMain();
                return;
            }
            // Возвращаем заказ обратно в слот: создаём аналогичный заказ в OrderService.
            // Простой путь: дать ему освободить слот (мы уже забрали), и кладём обратно.
            var gsm = GameStateManager.Instance;
            gsm.Orders.ReinsertOrder(_order); // нужно добавить такой метод в OrderService

            UIRouter.Instance.ShowMain();
            _order = null;
        }

        private static string FormatTime(float seconds)
        {
            int t = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{t / 60}:{(t % 60):00}";
        }

        private static string BuildModifiersString(Order order)
        {
            var sb = new StringBuilder();
            if (order.milk != null) sb.Append("на ").Append(order.milk.displayName.ToLower()).Append(" · ");
            if (order.cream != null) sb.Append("со сливками · ");
            if (order.syrup != null) sb.Append(order.syrup.displayName.ToLower()).Append(" · ");
            if (order.topping != null) sb.Append(order.topping.displayName.ToLower()).Append(" · ");
            sb.Append(order.isToGo ? "с собой" : "тут");
            return sb.ToString();
        }
    }
}
```

- [ ] **Step 3: Добавить метод `ReinsertOrder` в `OrderService`**

Открой `Assets/Scripts/Core/OrderService.cs`. Перед закрывающим `}` класса (но после `NextSpawnDelay` метода) добавь:

```csharp
        /// Положить заказ обратно в слот (например, если игрок отменил готовку).
        /// Возвращает true если получилось (слот свободен).
        public bool ReinsertOrder(Order order)
        {
            if (order == null) return false;
            if (order.slotIndex < 0 || order.slotIndex >= SlotCount) return false;
            if (_slots[order.slotIndex] != null) return false;
            _slots[order.slotIndex] = order;
            OrderSpawned?.Invoke(order);
            return true;
        }
```

- [ ] **Step 4: Прицепить контроллер на CookingScreenPanel**

В Hierarchy выбери `CookingScreenPanel` → Add Component → `Cooking Screen Controller`. Заполни поля:
- Recipe Label: `CookingScreenPanel/RecipeLabel`
- Modifiers Label: `CookingScreenPanel/ModifiersLabel`
- Patience Label: `CookingScreenPanel/PatienceLabel`
- Serve Button: `CookingScreenPanel/ServeButton`
- Cancel Button: `CookingScreenPanel/CancelButton`

- [ ] **Step 5: Compile, Console чистая (Cooking панель пока неактивна — но компилится)**

---

## Task 5: `OrderResultPopup` + `OrderResultPopupController`

**Files:**
- Create: `Assets/Scripts/UI/OrderResultPopupController.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать поп-ап в Canvas**

В Hierarchy → `Canvas` → правый клик → `UI → Panel`. Переименуй в `OrderResultPopup`.

- RectTransform: stretch на весь Canvas, Left/Right/Top/Bottom = 0
- Image → Color: HEX `000000` с Alpha = `128` (полупрозрачный чёрный — фон-затемнение)

Внутри `OrderResultPopup` → `UI → Image`. Переименуй в `Card`:
- Image → Color: белый, Source = дефолтный sliced
- RectTransform: anchor middle/center, Width=300, Height=320, anchored position 0,0

Внутри `Card`:

1. `UI → Text - TextMeshPro`, `Title`:
   - Text: `Заказ выдан!`
   - Font Size: 22, Bold
   - Color: чёрный, Alignment Center+Middle
   - RectTransform: top, Top=16, Height=32, Left=12, Right=12

2. `UI → Text - TextMeshPro`, `RecipeLine`:
   - Text: `Эспрессо` (плейсхолдер)
   - Font Size: 16
   - Alignment: Center+Middle
   - RectTransform: top, Top=56, Height=24, Left=12, Right=12

3. `UI → Text - TextMeshPro`, `BreakdownText`:
   - Text: `(подробности)`
   - Font Size: 13
   - Color: HEX `555555`
   - Alignment: Left+Top
   - RectTransform: top, Top=88, Height=140, Left=20, Right=20

4. `UI → Text - TextMeshPro`, `FinalLine`:
   - Text: `+ 130 ₽`
   - Font Size: 24, Bold
   - Color: HEX `2D9F4E`
   - Alignment: Center+Middle
   - RectTransform: top, Top=230, Height=36, Left=12, Right=12

5. `UI → Button - TextMeshPro`, `OkButton`:
   - Внутри `Text (TMP)`: `OK`, Font Size 16, Bold, белый
   - Button → Image → Color: HEX `5A8DDC`
   - RectTransform: bottom, Bottom=16, Height=44, Left=20, Right=20

- [ ] **Step 2: `OrderResultPopupController.cs`**

В `Assets/Scripts/UI/`:

```csharp
using System.Text;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Поп-ап после выдачи заказа: показывает разбивку чека.
    public class OrderResultPopupController : MonoBehaviour
    {
        public TMP_Text recipeLine;
        public TMP_Text breakdownText;
        public TMP_Text finalLine;
        public Button okButton;

        private void Awake()
        {
            if (okButton != null) okButton.onClick.AddListener(OnOk);
        }

        public void Show(OrderResolution res)
        {
            if (recipeLine != null) recipeLine.text = res.recipeDisplayName;

            var sb = new StringBuilder();
            sb.AppendLine($"База: {res.basePrice} ₽");
            sb.AppendLine($"Скорость ({res.speedLabel}): {FormatPercent(res.speedMultiplier)}");
            sb.AppendLine($"Качество ({res.qualityLabel}): {FormatPercent(res.qualityMultiplier)}");
            if (res.tierBonusMultiplier > 0)
                sb.AppendLine($"Машина T3: {FormatPercent(res.tierBonusMultiplier)}");
            if (res.doubleApplied)
                sb.AppendLine($"×2 буст применён");

            if (breakdownText != null) breakdownText.text = sb.ToString();
            if (finalLine != null) finalLine.text = $"+ {res.finalPayout} ₽";
        }

        private void OnOk()
        {
            UIRouter.Instance.HideOrderResult();
        }

        private static string FormatPercent(float mult)
        {
            int pct = Mathf.RoundToInt(mult * 100f);
            return pct >= 0 ? $"+{pct}%" : $"{pct}%";
        }
    }
}
```

- [ ] **Step 3: Прицепить контроллер**

В Hierarchy → `OrderResultPopup` → Add Component → `Order Result Popup Controller`. Заполни:
- Recipe Line: `OrderResultPopup/Card/RecipeLine`
- Breakdown Text: `OrderResultPopup/Card/BreakdownText`
- Final Line: `OrderResultPopup/Card/FinalLine`
- Ok Button: `OrderResultPopup/Card/OkButton`

- [ ] **Step 4: Compile, Console чистая**

---

## Task 6: Заполнить `UIRouter` в инспекторе

- [ ] **Step 1: Выбрать Canvas → UIRouter компонент**

В Inspector:
- Main Screen Panel: перетащи `Canvas/MainScreenPanel`
- Cooking Screen Panel: перетащи `Canvas/CookingScreenPanel`
- Order Result Popup: перетащи `Canvas/OrderResultPopup`
- Cooking Controller: перетащи `Canvas/CookingScreenPanel` (он содержит компонент)
- Result Popup Controller: перетащи `Canvas/OrderResultPopup` (содержит компонент)

- [ ] **Step 2: Сохрани сцену и Play**

Должен открыться MainScreenPanel, CookingScreenPanel и OrderResultPopup автоматически спрятаны (UIRouter.Start вызывает ShowMain).

- [ ] **Step 3: Commit всего UIRouter / Cooking / OrderResult куска**

```bash
git add Assets/Scripts/UI Assets/Scripts/Core/OrderService.cs Assets/Scenes/Main.unity && git commit -m "feat(ui): UIRouter + CookingScreenPanel + OrderResultPopup wired"
```

---

## Task 7: Связать тап по заказу с открытием Cooking-экрана

**Files:**
- Modify: `Assets/Scripts/UI/OrderSlotsController.cs`

- [ ] **Step 1: Поменять `OnSlotTapped`**

Открой `OrderSlotsController.cs`. Найди метод `OnSlotTapped` и **полностью замени** его на:

```csharp
        private void OnSlotTapped(int slotIndex)
        {
            var order = _gsm.Orders.TakeFromSlot(slotIndex);
            if (order == null) return;

            Debug.Log($"[Order tapped] Открываем cooking: {order.recipe.id}");
            UIRouter.Instance.OpenCooking(order);
        }
```

(Раньше там был `Debug.Log` + `TakeFromSlot`; теперь `TakeFromSlot` + `OpenCooking`.)

Не забудь `using DrinkitGame.UI;` если ещё не было — но мы уже в этом namespace, так что не нужно.

- [ ] **Step 2: Save сцены, Play, лайв-тест**

1. Подожди появления заказа в слоте
2. Тап → должен открыться Cooking-экран с деталями заказа и таймером
3. Жми `Выдать` → должен появиться OrderResult поп-ап с разбивкой "База / Скорость / Качество / Итог"
4. Жми `OK` → поп-ап исчезает, возврат на Main
5. Проверь топ-бар: баланс увеличился
6. Если open Cooking и нажать `← Назад` → заказ вернётся в слот

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/OrderSlotsController.cs && git commit -m "feat(ui): tap on order opens cooking screen via UIRouter"
```

---

## Task 8: Финальная сверка Phase 6

- [ ] **Step 1: Все тесты зелёные**

Test Runner → EditMode → Run All. ~76 тестов (Phase 1-5 + новые OrderResolutionService).

- [ ] **Step 2: Лайв-сценарий**

1. Запусти Play
2. Дождись появления заказа
3. Тап → cooking экран
4. Жми "Выдать"
5. Видишь OrderResult с базой ≥ 130 ₽ и финалом ≥ 169 ₽ (с +30% за скорость)
6. OK → возврат на Main
7. Баланс в топ-баре вырос
8. Goal-tracker может смениться (например, "Купи рецепт Американо" → если уже хватает, теперь "Продай 10 американо")
9. Через некоторое время — ещё один заказ, повторил, накопил на американо
10. Дойди до того, чтобы баланс ≥ 100 ₽ — это значит можно купить американо (Магазин будет в Phase 7)

- [ ] **Step 3: Console чистая, git log проверка**

```bash
git log --oneline | head -10
```

7 коммитов Phase 6.

---

## Self-Review

После прохождения:
1. ✅ `OrderResolutionService` атомарно завершает заказ + 9 тестов
2. ✅ `UIRouter` управляет панелями Main / Cooking / OrderResult
3. ✅ `CookingScreenPanel` показывает детали заказа, "Выдать" / "Назад"
4. ✅ `OrderResultPopup` показывает разбивку чека
5. ✅ Тап по заказу → cooking → выдача → OrderResult → возврат
6. ✅ Базовый цикл играбелен — можно зарабатывать деньги

**Готово → Phase 7: Магазин с 3 вкладками (рецепты / ингредиенты / машина).**

---

## Common Pitfalls (часто встречающиеся проблемы)

**1. `CookingScreenController` ругается что `UIRouter` не найден**
Причина: в `Canvas/UIRouter` не заполнено поле Cooking Controller (или Result Popup Controller). Фикс: перетащи в инспекторе.

**2. "Выдать" даёт 0 ₽**
Причина: `Quality < 50` или `elapsedSeconds > 300`. Проверь логику Bind/Update — `_order.remainingPatience` не должен ускоренно уменьшаться (deltaTime ок).

**3. После "Выдать" слот не освобождается**
Уже освобождён — `TakeFromSlot` вызывается в `OrderSlotsController.OnSlotTapped`. Если слот не пустеет, проверь что `OnSlotTapped` вызывает `TakeFromSlot` именно перед `OpenCooking`.

**4. Заказ исчезает в Cooking-экране (Bind кладёт null)**
В моменте `OpenCooking(order)` `order` уже взят из слота → не null. Проверь debug в `OrderSlotsController.OnSlotTapped`.

**5. `Random` ambiguity в тестах**
Все `new Random(...)` → `new System.Random(...)`. В этой фазе пока тестов с Random нет, но если будет — помни.

**6. `UIRouter.cs` не компилится: "type 'CookingScreenController' could not be found"**
Это происходит если ты создал `UIRouter.cs` ДО `CookingScreenController.cs`. Норм — после Task 4 ошибка уйдёт. Если осталась — проверь namespace `DrinkitGame.UI`.
