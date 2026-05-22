# Phase 8a — Cooking Flow (без мини-игр) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить "одной кнопкой Выдать" из Phase 6 на **полноценный пошаговый flow готовки**. Игрок тапает по шагам в правильной последовательности: "Возьми стакан → Тапни кофемолку → Тапни эспрессо-машину → Налить молоко → Тапни 'Выдать'". Шаги mini-game (помол, вспенивание, проливание, взбивание) для 8a просто советуются тапом (Quality = 100 заглушка). Phase 8b добавит реальные мини-игры.

**Architecture:**
- `CookingStepType` enum — все возможные шаги.
- `CookingStep` POCO — один шаг (type, label, optional product reference, isMiniGame).
- `CookingFlow` — static класс, метод `GenerateSteps(Order)` возвращает `List<CookingStep>` под конкретный заказ. Логика switch'ит по `recipe.family`.
- Pure C# тесты для `CookingFlow` — каждое семейство рецептов покрыто.
- `CookingScreenController` — переписан под список шагов. Один большой "тап-button" в центре, текст подсказки сверху, прогресс шагов.

**Tech Stack:** C# 9 · Unity 2022.3 · NUnit (Edit Mode).

**Конец фазы:** Тап заказа → Cooking → серия шагов вида "Тапни стакан" → "Тапни кофемолку" → "Тапни эспрессо-машину" → "Тапни Выдать". Каждый тап подсвечивает следующую подсказку. После последнего шага → OrderResolution + OrderResult попап.

---

## Task 1: Enum `CookingStepType` и POCO `CookingStep`

**Files:**
- Create: `Assets/Scripts/Cooking/CookingStep.cs`

- [ ] **Step 1: Создать файл**

В `Assets/Scripts/Cooking/`:

```csharp
using System;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Типы шагов готовки. Используется в CookingFlow и CookingScreenController.
    public enum CookingStepType
    {
        TakeCup,           // "Тапни стакан 'тут'" или "Тапни стакан 'с собой'"
        GrindCoffee,       // M1 mini-game (заглушка в 8a)
        Extract,           // авто-экстракция эспрессо (просто прогрессбар)
        AddHotWater,       // налить воду из чайника (для американо)
        TakeMilk,          // взять питчер с молоком (нужный тип)
        SteamMilk,         // M2 — вспенивание (заглушка в 8a)
        PourMilk,          // налить молоко в стакан
        TakeCream,         // взять сливки (для рафа)
        SteamCream,        // M2 — взбивание сливок (заглушка в 8a)
        PourCream,         // налить сливки в стакан
        AddMatcha,         // насыпать матча
        SetupFilter,       // поставить V60-воронку
        PourOver,          // M3 — проливание (заглушка в 8a)
        AddCacao,          // насыпать какао
        Whisk,             // M4 — взбивание венчиком (заглушка в 8a)
        AddSyrup,          // добавить сироп
        AddTopping,        // посыпать топпинг
        Deliver            // финальный — "Тапни Выдать"
    }

    /// Один шаг готовки: что показать игроку и что произойдёт по тапу.
    [Serializable]
    public class CookingStep
    {
        public CookingStepType type;
        public string hint;                  // "Тапни кофемолку" / "Возьми стакан"
        public ProductDefinition product;    // null если не связан с конкретным продуктом
        public bool isMiniGame;              // M1/M2/M3/M4 шаги — в 8b будут запускать мини-игру

        public CookingStep(CookingStepType type, string hint, ProductDefinition product = null, bool isMiniGame = false)
        {
            this.type = type;
            this.hint = hint;
            this.product = product;
            this.isMiniGame = isMiniGame;
        }
    }
}
```

- [ ] **Step 2: Compile, Console чистая, Commit**

```bash
git add Assets/Scripts/Cooking && git commit -m "feat(cooking): CookingStepType enum and CookingStep POCO"
```

---

## Task 2: `CookingFlow` — генерация шагов по рецепту

**Files:**
- Create: `Assets/Scripts/Cooking/CookingFlow.cs`

- [ ] **Step 1: Создать файл**

```csharp
using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Статический генератор последовательности шагов готовки по заказу.
    /// Логика switch'ит по recipe.family и подмешивает модификаторы.
    public static class CookingFlow
    {
        public static List<CookingStep> GenerateSteps(Order order)
        {
            var steps = new List<CookingStep>();
            if (order == null || order.recipe == null) return steps;

            // 1. Всегда первый шаг — взять стакан
            steps.Add(new CookingStep(
                CookingStepType.TakeCup,
                order.isToGo ? "Возьми стакан 'с собой'" : "Возьми стакан 'тут'"));

            // 2. Дальше — семейство-специфичные шаги
            switch (order.recipe.family)
            {
                case RecipeFamily.Espresso:
                    AddEspressoCore(steps);
                    break;
                case RecipeFamily.Americano:
                    AddEspressoCore(steps);
                    steps.Add(new CookingStep(CookingStepType.AddHotWater, "Налить горячую воду"));
                    break;
                case RecipeFamily.Cappuccino:
                case RecipeFamily.Latte:
                    AddEspressoCore(steps);
                    AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Raf:
                    AddEspressoCore(steps);
                    AddCreamSteamPour(steps, order.cream);
                    break;
                case RecipeFamily.Cacao:
                    steps.Add(new CookingStep(CookingStepType.AddCacao, "Насыпь какао"));
                    AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Matcha:
                    steps.Add(new CookingStep(CookingStepType.AddMatcha, "Насыпь матчу"));
                    steps.Add(new CookingStep(CookingStepType.AddHotWater, "Залей горячую воду"));
                    steps.Add(new CookingStep(CookingStepType.Whisk, "Взбей венчиком (M4)", isMiniGame: true));
                    if (order.milk != null) AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Filter:
                    steps.Add(new CookingStep(CookingStepType.SetupFilter, "Поставь V60-воронку"));
                    steps.Add(new CookingStep(CookingStepType.GrindCoffee, "Намели кофе (M1)", isMiniGame: true));
                    steps.Add(new CookingStep(CookingStepType.PourOver, "Залей водой (M3)", isMiniGame: true));
                    break;
            }

            // 3. Модификаторы — сироп / топпинг
            if (order.syrup != null)
                steps.Add(new CookingStep(CookingStepType.AddSyrup, $"Добавь сироп: {order.syrup.displayName.ToLower()}", order.syrup));
            if (order.topping != null)
                steps.Add(new CookingStep(CookingStepType.AddTopping, $"Посыпь: {order.topping.displayName.ToLower()}", order.topping));

            // 4. Финальная выдача
            steps.Add(new CookingStep(CookingStepType.Deliver, "Тапни 'Выдать'"));

            return steps;
        }

        private static void AddEspressoCore(List<CookingStep> steps)
        {
            steps.Add(new CookingStep(CookingStepType.GrindCoffee, "Намели кофе (M1)", isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.Extract, "Запусти эспрессо-машину"));
        }

        private static void AddMilkSteamPour(List<CookingStep> steps, ProductDefinition milk)
        {
            string milkName = milk != null ? milk.displayName.ToLower() : "молоко";
            steps.Add(new CookingStep(CookingStepType.TakeMilk, $"Налей {milkName} в питчер", milk));
            steps.Add(new CookingStep(CookingStepType.SteamMilk, "Вспень молоко (M2)", milk, isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.PourMilk, "Налей в стакан", milk));
        }

        private static void AddCreamSteamPour(List<CookingStep> steps, ProductDefinition cream)
        {
            steps.Add(new CookingStep(CookingStepType.TakeCream, "Налей сливки в питчер", cream));
            steps.Add(new CookingStep(CookingStepType.SteamCream, "Вспень сливки (M2)", cream, isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.PourCream, "Налей в стакан", cream));
        }
    }
}
```

- [ ] **Step 2: Compile, Console чистая**

---

## Task 3: Тесты `CookingFlow`

**Files:**
- Create: `Assets/Tests/EditMode/CookingFlowTests.cs`
- Modify: `Assets/Tests/EditMode/DrinkitGame.Tests.EditMode.asmdef` (добавить ссылку если ещё не подключена сборка Cooking)

- [ ] **Step 1: Убедиться что `DrinkitGame` asmdef включает `Cooking` неймспейс**

Файл `Assets/Scripts/DrinkitGame.asmdef`: если у тебя там только `Unity.TextMeshPro` reference — этого хватит, так как все скрипты в одной сборке `DrinkitGame`. **Никаких изменений не нужно**, если asmdef один на весь Scripts/.

- [ ] **Step 2: Создать `CookingFlowTests.cs`**

```csharp
using DrinkitGame.Cooking;
using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class CookingFlowTests
    {
        private ProductDefinition _milkCow, _milkOat, _cream, _syrup, _cinnamon, _marshmallow;
        private MachineTierDefinition _t1;

        [SetUp]
        public void Setup()
        {
            _milkCow = MakeProduct("milk_cow", "Молоко коровье", ProductCategory.Milk);
            _milkOat = MakeProduct("milk_oat", "Молоко овсяное", ProductCategory.Milk);
            _cream = MakeProduct("cream", "Сливки", ProductCategory.Cream);
            _syrup = MakeProduct("syrup_vanilla", "Сироп ваниль", ProductCategory.Syrup);
            _cinnamon = MakeProduct("topping_cinnamon", "Корица", ProductCategory.Topping);
            _marshmallow = MakeProduct("topping_marshmallow", "Зефирки", ProductCategory.Topping);

            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;
        }

        private ProductDefinition MakeProduct(string id, string name, ProductCategory cat)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.id = id; p.displayName = name; p.category = cat;
            return p;
        }

        private RecipeDefinition MakeRecipe(string id, RecipeFamily family)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.id = id; r.family = family; r.requiredMachineTier = _t1;
            return r;
        }

        [Test]
        public void Espresso_ProducesMinimalSteps()
        {
            var order = new Order { recipe = MakeRecipe("espresso", RecipeFamily.Espresso) };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, Grind, Extract, Deliver = 4
            Assert.AreEqual(4, steps.Count);
            Assert.AreEqual(CookingStepType.TakeCup, steps[0].type);
            Assert.AreEqual(CookingStepType.GrindCoffee, steps[1].type);
            Assert.AreEqual(CookingStepType.Extract, steps[2].type);
            Assert.AreEqual(CookingStepType.Deliver, steps[3].type);
        }

        [Test]
        public void Americano_AddsHotWater()
        {
            var order = new Order { recipe = MakeRecipe("americano", RecipeFamily.Americano) };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, Grind, Extract, AddHotWater, Deliver = 5
            Assert.AreEqual(5, steps.Count);
            Assert.AreEqual(CookingStepType.AddHotWater, steps[3].type);
        }

        [Test]
        public void Cappuccino_WithOatMilk_ProducesMilkSteps()
        {
            var order = new Order
            {
                recipe = MakeRecipe("cappuccino", RecipeFamily.Cappuccino),
                milk = _milkOat
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, Grind, Extract, TakeMilk, SteamMilk, PourMilk, Deliver
            Assert.AreEqual(7, steps.Count);
            Assert.AreEqual(CookingStepType.TakeMilk, steps[3].type);
            Assert.AreEqual(CookingStepType.SteamMilk, steps[4].type);
            Assert.AreEqual(CookingStepType.PourMilk, steps[5].type);
            Assert.AreSame(_milkOat, steps[3].product);
        }

        [Test]
        public void Raf_UsesCream()
        {
            var order = new Order
            {
                recipe = MakeRecipe("raf", RecipeFamily.Raf),
                cream = _cream
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, Grind, Extract, TakeCream, SteamCream, PourCream, Deliver
            Assert.AreEqual(7, steps.Count);
            Assert.AreEqual(CookingStepType.TakeCream, steps[3].type);
            Assert.AreEqual(CookingStepType.SteamCream, steps[4].type);
            Assert.AreEqual(CookingStepType.PourCream, steps[5].type);
        }

        [Test]
        public void Filter_UsesPourOverFlow()
        {
            var order = new Order { recipe = MakeRecipe("filter", RecipeFamily.Filter) };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, SetupFilter, Grind, PourOver, Deliver
            Assert.AreEqual(5, steps.Count);
            Assert.AreEqual(CookingStepType.SetupFilter, steps[1].type);
            Assert.AreEqual(CookingStepType.GrindCoffee, steps[2].type);
            Assert.AreEqual(CookingStepType.PourOver, steps[3].type);
        }

        [Test]
        public void Matcha_WithoutMilk()
        {
            var order = new Order { recipe = MakeRecipe("matcha", RecipeFamily.Matcha) };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, AddMatcha, AddHotWater, Whisk, Deliver
            Assert.AreEqual(5, steps.Count);
            Assert.AreEqual(CookingStepType.AddMatcha, steps[1].type);
            Assert.AreEqual(CookingStepType.AddHotWater, steps[2].type);
            Assert.AreEqual(CookingStepType.Whisk, steps[3].type);
        }

        [Test]
        public void Matcha_WithMilk_AddsSteamingSteps()
        {
            var order = new Order
            {
                recipe = MakeRecipe("matcha", RecipeFamily.Matcha),
                milk = _milkCow
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, AddMatcha, AddHotWater, Whisk, TakeMilk, SteamMilk, PourMilk, Deliver
            Assert.AreEqual(8, steps.Count);
        }

        [Test]
        public void Cacao_HasSimpleFlow()
        {
            var order = new Order
            {
                recipe = MakeRecipe("cacao", RecipeFamily.Cacao),
                milk = _milkCow
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, AddCacao, TakeMilk, SteamMilk, PourMilk, Deliver
            Assert.AreEqual(6, steps.Count);
            Assert.AreEqual(CookingStepType.AddCacao, steps[1].type);
        }

        [Test]
        public void Modifiers_AreAppended_BeforeDeliver()
        {
            var order = new Order
            {
                recipe = MakeRecipe("cappuccino", RecipeFamily.Cappuccino),
                milk = _milkCow,
                syrup = _syrup,
                topping = _cinnamon
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Cup, Grind, Extract, TakeMilk, SteamMilk, PourMilk, AddSyrup, AddTopping, Deliver = 9
            Assert.AreEqual(9, steps.Count);
            Assert.AreEqual(CookingStepType.AddSyrup, steps[6].type);
            Assert.AreEqual(CookingStepType.AddTopping, steps[7].type);
            Assert.AreEqual(CookingStepType.Deliver, steps[8].type);
        }

        [Test]
        public void MiniGameSteps_AreFlagged()
        {
            var order = new Order
            {
                recipe = MakeRecipe("cappuccino", RecipeFamily.Cappuccino),
                milk = _milkCow
            };
            var steps = CookingFlow.GenerateSteps(order);
            // Grind (idx 1) и SteamMilk (idx 4) — мини-игры
            Assert.IsTrue(steps[1].isMiniGame, "GrindCoffee — мини-игра");
            Assert.IsTrue(steps[4].isMiniGame, "SteamMilk — мини-игра");
            Assert.IsFalse(steps[0].isMiniGame, "TakeCup — не мини-игра");
            Assert.IsFalse(steps[6].isMiniGame, "Deliver — не мини-игра");
        }
    }
}
```

- [ ] **Step 3: Run All — все зелёные, Commit**

```bash
git add Assets/Scripts/Cooking Assets/Tests/EditMode/CookingFlowTests.cs Assets/Tests/EditMode/CookingFlowTests.cs.meta && git commit -m "feat(cooking): CookingFlow generates step list per recipe family (with tests)"
```

---

## Task 4: Переписать `CookingScreenController` под список шагов

**Files:**
- Modify: `Assets/Scripts/UI/CookingScreenController.cs`

- [ ] **Step 1: Заменить класс целиком**

Открой `Assets/Scripts/UI/CookingScreenController.cs`, **полностью замени** содержимое:

```csharp
using System.Collections.Generic;
using System.Text;
using DrinkitGame.Cooking;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Контроллер экрана готовки: ведёт игрока по шагам CookingFlow.
    /// В 8a все шаги (включая мини-игры) — просто тап для перехода дальше, quality=100.
    /// В 8b мини-игры подменим на реальные оверлеи.
    public class CookingScreenController : MonoBehaviour
    {
        [Header("Labels")]
        public TMP_Text orderSummaryLabel;     // "Капучино на овсяном · с собой"
        public TMP_Text hintLabel;             // "Тапни кофемолку"
        public TMP_Text progressLabel;         // "Шаг 3 из 7"
        public TMP_Text patienceLabel;

        [Header("Buttons")]
        public Button advanceButton;
        public TMP_Text advanceButtonLabel;
        public Button cancelButton;

        private Order _order;
        private List<CookingStep> _steps;
        private int _currentIndex;
        private float _qualitySum;
        private int _qualityCount;

        private void Awake()
        {
            if (advanceButton != null) advanceButton.onClick.AddListener(OnAdvance);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        }

        public void Bind(Order order)
        {
            _order = order;
            _steps = CookingFlow.GenerateSteps(order);
            _currentIndex = 0;
            _qualitySum = 0f;
            _qualityCount = 0;

            if (orderSummaryLabel != null)
                orderSummaryLabel.text = BuildSummary(order);

            ShowCurrentStep();
        }

        private void Update()
        {
            if (_order != null && patienceLabel != null)
            {
                _order.remainingPatience -= Time.deltaTime;
                if (_order.remainingPatience < 0) _order.remainingPatience = 0;
                patienceLabel.text = $"Терпение: {FormatTime(_order.remainingPatience)}";
            }
        }

        private void ShowCurrentStep()
        {
            if (_steps == null || _currentIndex >= _steps.Count) return;
            var step = _steps[_currentIndex];
            if (hintLabel != null) hintLabel.text = step.hint;
            if (progressLabel != null) progressLabel.text = $"Шаг {_currentIndex + 1} из {_steps.Count}";
            if (advanceButtonLabel != null)
                advanceButtonLabel.text = step.type == CookingStepType.Deliver ? "Выдать" : "Дальше";
        }

        private void OnAdvance()
        {
            if (_steps == null || _currentIndex >= _steps.Count) return;
            var step = _steps[_currentIndex];

            // 8a: мини-игры заглушены — Quality = 100
            if (step.isMiniGame)
            {
                _qualitySum += 100f;
                _qualityCount += 1;
            }

            _currentIndex++;
            if (_currentIndex >= _steps.Count)
            {
                CompleteOrder();
            }
            else
            {
                ShowCurrentStep();
            }
        }

        private void OnCancel()
        {
            if (_order == null)
            {
                UIRouter.Instance.ShowMain();
                return;
            }
            // Вернуть заказ обратно в слот
            var gsm = GameStateManager.Instance;
            gsm.Orders.ReinsertOrder(_order);
            UIRouter.Instance.ShowMain();
            _order = null;
        }

        private void CompleteOrder()
        {
            if (_order == null) return;
            var gsm = GameStateManager.Instance;

            float quality = _qualityCount > 0 ? _qualitySum / _qualityCount : 100f;
            float elapsed = OrderService.Patience - _order.remainingPatience;

            var resolution = gsm.OrderResolution.Complete(_order, quality, elapsed);

            UIRouter.Instance.ShowMain();
            UIRouter.Instance.ShowOrderResult(resolution);
            _order = null;
        }

        private static string BuildSummary(Order order)
        {
            var sb = new StringBuilder();
            sb.Append(order.recipe.displayName);
            if (order.milk != null) sb.Append(" · ").Append(order.milk.displayName.ToLower());
            if (order.cream != null) sb.Append(" · сливки");
            if (order.syrup != null) sb.Append(" · ").Append(order.syrup.displayName.ToLower());
            if (order.topping != null) sb.Append(" · ").Append(order.topping.displayName.ToLower());
            sb.Append(" · ").Append(order.isToGo ? "с собой" : "тут");
            return sb.ToString();
        }

        private static string FormatTime(float seconds)
        {
            int t = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{t / 60}:{(t % 60):00}";
        }
    }
}
```

- [ ] **Step 2: Compile, Console чистая**

---

## Task 5: Обновить UI `CookingScreenPanel` под новую логику

**Files:**
- Modify: `Assets/Scenes/Main.unity`

В сцене сейчас у CookingScreen есть Title, RecipeLabel, ModifiersLabel, PatienceLabel, ServeButton, CancelButton. Переименуем и добавим прогресс/подсказку.

- [ ] **Step 1: Удалить старые TMP-лейблы и переиспользовать в новой структуре**

В Hierarchy → `CookingScreenPanel`:

Удалить (правый клик → Delete):
- `Title` (заголовок "Готовка" — не нужен)
- `ServeButton` (заменим на `AdvanceButton`)

Переименовать:
- `RecipeLabel` → `OrderSummary` (Top=24, Left=12, Right=12, H=28). Меньше шрифт — Size 14.
- `ModifiersLabel` → `HintLabel` (Top=70, Left=12, Right=12, H=60). Шрифт Size 22, Bold, чёрный.
- `PatienceLabel` оставить как есть (теперь Top=140, H=24).

Добавить новый TMP `ProgressLabel`:
- Text: `Шаг 1 из 5`
- Font Size: 14, color HEX `666666`
- Alignment: Center+Middle
- RectTransform: Top=180, Left=12, Right=12, H=20

Создать новую `UI → Button - TextMeshPro`, назвать `AdvanceButton`:
- Image → Color: HEX `2D9F4E` (зелёный)
- Внутри Text (TMP): `Дальше`, Size 22, Bold, белый
- RectTransform: bottom anchor, Bottom=110, Height=80, Left=24, Right=24

`CancelButton` оставить как есть.

- [ ] **Step 2: Обновить ссылки в `CookingScreenController`**

В Hierarchy выбери `CookingScreenPanel` → в компоненте `Cooking Screen Controller`:
- Order Summary Label: `OrderSummary`
- Hint Label: `HintLabel`
- Progress Label: `ProgressLabel`
- Patience Label: `PatienceLabel`
- Advance Button: `AdvanceButton`
- Advance Button Label: `AdvanceButton/Text (TMP)`
- Cancel Button: `CancelButton`

(Старые поля `Recipe Label`, `Modifiers Label`, `Serve Button` в инспекторе теперь должны исчезнуть, т.к. их нет в скрипте.)

- [ ] **Step 3: Save сцены, Play, лайв-тест**

1. Подожди заказа, тапни → Cooking-экран открывается
2. Видишь сверху сводку заказа, посередине большой текст подсказки ("Возьми стакан 'тут'"), снизу прогресс "Шаг 1 из 5"
3. Тап "Дальше" → следующий шаг ("Тапни кофемолку")
4. Продолжай до последнего шага "Тапни 'Выдать'"
5. Жми "Выдать" → OrderResult попап с базой/бонусами/итогом
6. OK → возврат на главный

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/CookingScreenController.cs Assets/Scenes/Main.unity && git commit -m "feat(cooking): CookingScreenController drives step-by-step flow"
```

---

## Task 6: Финальная сверка Phase 8a

- [ ] **Step 1: Все тесты зелёные**

Run All — ~95 зелёных тестов (Phase 1-6 + CookingFlow 10).

- [ ] **Step 2: Лайв-сценарий все 8 рецептов**

Чтобы проверить все семейства:
1. Эспрессо (4 шага)
2. Американо (5 шагов)
3. Капучино (7 шагов) — нужно купить рецепт + машину T2
4. Латте (7 шагов) — нужно продать 15 капучино
5. Какао (6 шагов) — после T2
6. Раф (7 шагов) — после 10 латте + ингредиент сливки
7. Фильтр (5 шагов) — после T3
8. Матча (5 шагов без молока / 8 шагов с молоком) — после T3

В реальном плейтесте дойти до всех 8 — это финальная проверка экономики. Можно подкрутить цены в SO ассетах для быстроты.

- [ ] **Step 3: Git log проверка**

Должно быть 4 коммита Phase 8a.

---

## Self-Review

После прохождения:
1. ✅ `CookingStep` + `CookingStepType` определены
2. ✅ `CookingFlow.GenerateSteps` корректно генерирует под 8 семейств
3. ✅ 10 тестов покрывают каждое семейство и модификаторы
4. ✅ `CookingScreenController` ведёт игрока по шагам
5. ✅ Все 8 рецептов готовятся пошагово
6. ✅ Quality = 100 пока (заглушка)

**Готово → Phase 8b: реальные 4 мини-игры на M1/M2/M3/M4 шагах.**

---

## Common Pitfalls

**1. NullReferenceException в `Bind`**
Причина: order.recipe == null. Это значит RecipeDefinition был удалён или ссылка не подцепилась. Проверь GameContent.

**2. CookingFlow не находит namespace**
Файл лежит в `Assets/Scripts/Cooking/`. Убедись namespace = `DrinkitGame.Cooking` и в контроллере `using DrinkitGame.Cooking;`.

**3. После Cancel заказ не возвращается в слот**
`OrderService.ReinsertOrder` мы добавили в Phase 6. Проверь что метод там есть.

**4. Tests "DrinkitGame.Cooking" assembly not found**
В asmdef `DrinkitGame.Tests.EditMode.asmdef` должны быть ссылки `DrinkitGame`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`. Сам namespace `DrinkitGame.Cooking` живёт в той же сборке `DrinkitGame` — дополнительные ссылки не нужны.

**5. Кнопка "Дальше" не реагирует**
Проверь что в инспекторе CookingScreenController поле `Advance Button` заполнено правильным компонентом Button (не GameObject). И что есть `advanceButton.onClick.AddListener(OnAdvance)` в Awake (есть в коде).

**6. Тесты CookingFlow генерируют разное число шагов в разных запусках**
Тесты детерминированы (не используют Random). Если разное — где-то опечатался в семействе или забыл обработать модификатор.
