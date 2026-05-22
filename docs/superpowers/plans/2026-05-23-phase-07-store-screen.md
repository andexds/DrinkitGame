# Phase 7 — Store Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Сделать рабочий **Магазин** с 3 вкладками: **Рецепты** (купить рецепт за деньги/квест), **Ингредиенты** (докупить SKU на склад), **Машина** (прокачать на следующий тир). Тап по табу "Магазин" в нижнем таб-баре открывает экран. Все покупки списывают деньги, обновляют состояние и UI реактивно.

**Architecture:**
- `StoreScreenPanel` — корневая панель в Canvas, скрыта по умолчанию. Внутри: внутренний таб-бар (3 кнопки) + 3 ScrollView (по одному на вкладку).
- 2 prefab'а строк: `RecipeRow`, `IngredientRow` — кладутся в Scroll Content.
- `RecipeRow` / `IngredientRow` — компоненты на префабах с публичным `Bind(...)` и событиями кликов.
- `StoreScreenController` — главный контроллер, в `Start` подписывается на сервисы, в `Show()` строит списки.
- `RecipesTabController`, `IngredientsTabController`, `MachineTabController` — по одному на вкладку, чтобы не делать `StoreScreenController` гигантским.
- В `UIRouter` добавляем `OpenStore()` и `Store Screen Panel` ссылку.

**Tech Stack:** uGUI · TMPro · ScrollRect · VerticalLayoutGroup.

**Конец фазы:** Жмёшь "Магазин" в таб-баре → открывается экран с вкладкой "Рецепты" по умолчанию → видишь 8 строк с состоянием каждого рецепта → жмёшь "Купить" на американо (если есть 100₽) → деньги списываются, рецепт открывается, заказы американо начинают появляться. Переключаешь на "Ингредиенты" → видишь 15 SKU с остатками и ценами, можешь докупить. Переключаешь на "Машина" → видишь карточку T1 + карточку "Следующий тир: Бариста" с прогрессом квеста и ценой.

---

## Task 1: `StoreScreenPanel` базовый layout

**Files:**
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `StoreScreenPanel`**

В Hierarchy → `Canvas` → правый клик → `UI → Panel`. Переименуй в `StoreScreenPanel`.

- RectTransform: stretch на весь Canvas, L/R/T/B = 0
- Image → Color: HEX `E3EEFF`

- [ ] **Step 2: Заголовок и кнопка "Назад"**

Внутри `StoreScreenPanel`:

1. `UI → Button - TextMeshPro`, переименуй в `BackButton`.
   - Text inside: `← Назад`, Font Size 14, чёрный
   - Image → Color: HEX `FFFFFF`
   - RectTransform: top-left, Top=20, Left=12, W=90, H=32

2. `UI → Text - TextMeshPro`, `Title`:
   - Text: `Магазин`
   - Font Size: 22, Bold
   - Color: чёрный, Alignment Center+Middle
   - RectTransform: top, Top=24, Left=120, Right=120, H=32

- [ ] **Step 3: Внутренний таб-бар (3 кнопки)**

Правый клик `StoreScreenPanel` → `Create Empty` → `InnerTabs`.
- RectTransform: top-stretch, Top=70, Left=12, Right=12, H=40
- Add Component → `Horizontal Layout Group`:
  - Padding: 0
  - Spacing: 4
  - Child Alignment: Middle Center
  - Control Child Size: ✓ W, ✓ H
  - Child Force Expand: ✓ Width, ❌ Height

Внутри `InnerTabs`:
- Правый клик → `UI → Button - TextMeshPro`. Переименуй в `Tab_Recipes`.
  - Text: `Рецепты`, Font Size 14, Bold, белый
  - Image → Color: HEX `5A8DDC` (активная)

- Дублируй (Cmd+D) → `Tab_Ingredients`.
  - Text: `Ингредиенты`
  - Image → Color: HEX `B5C7E5` (неактивная)

- Дублируй → `Tab_Machine`.
  - Text: `Машина`
  - Image → Color: HEX `B5C7E5`

- [ ] **Step 4: Контейнер для содержимого вкладок**

Правый клик `StoreScreenPanel` → `Create Empty` → `TabContent`.
- RectTransform: stretch (anchor: top-stretch + bottom-stretch — Alt+click `stretch/stretch` потом задай поля), Top=120, Bottom=80, Left=8, Right=8

Этот контейнер будет хранить 3 ScrollView'a — по одному на вкладку.

- [ ] **Step 5: Сохрани сцену (Cmd+S). Не коммитим — продолжаем в Task 2.**

---

## Task 2: ScrollView для вкладки "Рецепты" + prefab `RecipeRow`

**Files:**
- Modify: `Assets/Scenes/Main.unity`
- Create: `Assets/Scripts/UI/RecipeRow.cs`
- Create: `Assets/Prefabs/RecipeRow.prefab` (через Unity)

- [ ] **Step 1: ScrollView для рецептов**

В Hierarchy → `StoreScreenPanel/TabContent` → правый клик → `UI → Scroll View`. Переименуй в `RecipesScroll`.

- RectTransform: stretch на родителя (Alt+click `stretch/stretch`, L/R/T/B = 0)
- В Scroll View компоненте: Horizontal = false, Vertical = true
- `RecipesScroll/Viewport/Content` — выбери Content. Add Component → `Vertical Layout Group`:
  - Padding: 8 по всем
  - Spacing: 8
  - Child Alignment: Upper Center
  - Control Child Size: ✓ W, ✓ H
  - Force Expand: ✓ Width, ❌ Height
- Content также Add Component → `Content Size Fitter`:
  - Horizontal Fit: Unconstrained
  - Vertical Fit: Preferred Size

(Это автоматически расширяет Content под количество строк.)

- [ ] **Step 2: Создать `RecipeRow.cs`**

В `Assets/Scripts/UI/`:

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Одна строка в списке рецептов Магазина.
    public class RecipeRow : MonoBehaviour
    {
        public Image background;
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text statusLabel;
        public TMP_Text priceLabel;
        public Button buyButton;
        public TMP_Text buyButtonLabel;

        public System.Action<RecipeDefinition> OnBuyClicked;

        private RecipeDefinition _recipe;

        private void Awake()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_recipe));
        }

        public void Bind(RecipeDefinition recipe, RecipeService recipes, EconomyService economy)
        {
            _recipe = recipe;
            if (nameLabel != null) nameLabel.text = recipe.displayName;
            if (icon != null && recipe.icon != null) icon.sprite = recipe.icon;

            var availability = recipes.GetAvailability(recipe);
            string status;
            string buyText = "Купить";
            bool buyActive = false;

            switch (availability)
            {
                case PurchaseAvailability.AlreadyOwned:
                    status = "✓ Открыт";
                    buyText = "—";
                    break;
                case PurchaseAvailability.NeedsHigherMachine:
                    status = $"Нужна машина T{recipe.requiredMachineTier.tierIndex}";
                    buyText = "🔒";
                    break;
                case PurchaseAvailability.NeedsMoreSales:
                    status = string.IsNullOrEmpty(recipe.unlockQuestDescription)
                        ? "Выполни условие"
                        : recipe.unlockQuestDescription;
                    buyText = "🔒";
                    break;
                case PurchaseAvailability.NotEnoughMoney:
                    status = "Не хватает денег";
                    buyText = "Купить";
                    break;
                default: // Available
                    status = "Можно купить";
                    buyText = "Купить";
                    buyActive = true;
                    break;
            }

            if (statusLabel != null) statusLabel.text = status;
            if (priceLabel != null)
                priceLabel.text = recipe.recipePurchasePrice > 0 ? $"{recipe.recipePurchasePrice} ₽" : "";
            if (buyButtonLabel != null) buyButtonLabel.text = buyText;
            if (buyButton != null) buyButton.interactable = buyActive;
        }
    }
}
```

- [ ] **Step 3: Собрать RecipeRow в сцене (как заготовку для prefab'a)**

В Hierarchy выбери `RecipesScroll/Viewport/Content`. Правый клик → `UI → Image`. Переименуй в `RecipeRow`.

- Image → Color: HEX `FFFFFF`
- Layout Element: Preferred Height = `72`

Внутри `RecipeRow`:

1. `UI → Image`, переименуй в `Icon`. Цвет HEX `B5C7E5` (плейсхолдер).
   - RectTransform: left-anchor, anchored X=8, центрировано по вертикали; W=48, H=48

2. `UI → Text - TextMeshPro`, переименуй в `Name`.
   - Text: `Эспрессо`
   - Font Size: 16, Bold, чёрный
   - Alignment: Left + Top
   - RectTransform: Left=68, Right=140, Top=8, H=24

3. `UI → Text - TextMeshPro`, переименуй в `Status`.
   - Text: `Можно купить`
   - Font Size: 12, color HEX `666666`
   - Alignment: Left + Top
   - RectTransform: Left=68, Right=140, Top=36, H=20

4. `UI → Text - TextMeshPro`, переименуй в `Price`.
   - Text: `100 ₽`
   - Font Size: 14, Bold, color HEX `2D9F4E`
   - Alignment: Right + Middle
   - RectTransform: Right=10 anchor, anchored, W=70, H=24, top-right area

5. `UI → Button - TextMeshPro`, переименуй в `BuyButton`.
   - Image → Color HEX `5A8DDC`
   - Внутри `Text (TMP)`: `Купить`, Font Size 13, белый
   - RectTransform: right-anchor, anchored X=-8, центр по вертикали; W=80, H=32

- [ ] **Step 4: Прицепить компонент `RecipeRow`**

Выбери `RecipeRow` GameObject → Add Component → `Recipe Row`. Заполни:
- Background: сам `RecipeRow` (Image)
- Icon: `Icon`
- Name Label: `Name`
- Status Label: `Status`
- Price Label: `Price`
- Buy Button: `BuyButton`
- Buy Button Label: `BuyButton/Text (TMP)`

- [ ] **Step 5: Превратить в Prefab**

Перетащи `RecipeRow` GameObject из Hierarchy в `Assets/Prefabs/`. Готов prefab.

**После создания prefab'a — удали `RecipeRow` из Hierarchy** (он остался как инстанс, нам не нужен — будем спавнить программно из контроллера).

- [ ] **Step 6: Save сцены, Console чистая, Commit**

```bash
git add Assets/Scripts/UI/RecipeRow.cs Assets/Scripts/UI/RecipeRow.cs.meta Assets/Prefabs Assets/Scenes/Main.unity && git commit -m "feat(ui): RecipeRow component and prefab for store recipes list"
```

---

## Task 3: ScrollView для вкладки "Ингредиенты" + prefab `IngredientRow`

**Files:**
- Modify: `Assets/Scenes/Main.unity`
- Create: `Assets/Scripts/UI/IngredientRow.cs`
- Create: `Assets/Prefabs/IngredientRow.prefab`

- [ ] **Step 1: ScrollView для ингредиентов**

В `StoreScreenPanel/TabContent` правый клик → `UI → Scroll View`. Переименуй в `IngredientsScroll`. Настрой как в Task 2.1.

- [ ] **Step 2: `IngredientRow.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    public class IngredientRow : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text stockLabel;
        public TMP_Text priceLabel;
        public Button plus1Button;
        public Button plus10Button;
        public Button plus50Button;

        public System.Action<ProductDefinition, int> OnBuyClicked;

        private ProductDefinition _product;

        private void Awake()
        {
            if (plus1Button != null) plus1Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 1));
            if (plus10Button != null) plus10Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 10));
            if (plus50Button != null) plus50Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 50));
        }

        public void Bind(ProductDefinition product, InventoryService inventory, EconomyService economy)
        {
            _product = product;
            if (nameLabel != null) nameLabel.text = product.displayName;
            if (icon != null && product.icon != null) icon.sprite = product.icon;
            if (stockLabel != null) stockLabel.text = $"× {inventory.GetStock(product.id)}";
            if (priceLabel != null) priceLabel.text = $"{product.purchasePrice} ₽";

            // Активность кнопок — по бюджету
            if (plus1Button != null) plus1Button.interactable = economy.Balance >= product.purchasePrice;
            if (plus10Button != null) plus10Button.interactable = economy.Balance >= product.purchasePrice * 10;
            // +50 со скидкой 5%
            if (plus50Button != null) plus50Button.interactable = economy.Balance >= (int)(product.purchasePrice * 50 * 0.95f);
        }
    }
}
```

- [ ] **Step 3: Собрать `IngredientRow` в сцене**

В `IngredientsScroll/Viewport/Content` → правый клик → `UI → Image`. Переименуй в `IngredientRow`.

- Image → Color: HEX `FFFFFF`
- Layout Element: Preferred Height = `64`

Внутри:
1. `Icon` — UI Image, HEX `B5C7E5`, W=40, H=40, left anchor, X=8
2. `Name` — TMP, Text `Кофе зерно`, Size 14, Bold, чёрный, Left=56, Right=180, Top=8, H=20
3. `Stock` — TMP, Text `× 10`, Size 12, HEX `666666`, Left=56, Right=180, Top=32, H=16
4. `Price` — TMP, Text `15 ₽`, Size 12, Bold, HEX `2D9F4E`, Right anchor X=-150, W=44, центр по вертикали
5. Три кнопки в один ряд (HEX `5A8DDC`, белый текст, Bold Size 12):
   - `Plus1Button` — Text `+1`, Right=8, W=36, H=28
   - `Plus10Button` — Text `+10`, Right=48, W=40, H=28
   - `Plus50Button` — Text `+50`, Right=92, W=44, H=28

- [ ] **Step 4: Прицепить компонент**

`IngredientRow` → Add Component → `Ingredient Row`. Заполни поля (Icon, Name, Stock, Price, Plus1/10/50 кнопки).

- [ ] **Step 5: Превратить в Prefab + удалить из Hierarchy**

Перетащи в `Assets/Prefabs/`. Удали из Hierarchy.

- [ ] **Step 6: Save и Commit**

```bash
git add Assets/Scripts/UI/IngredientRow.cs Assets/Scripts/UI/IngredientRow.cs.meta Assets/Prefabs Assets/Scenes/Main.unity && git commit -m "feat(ui): IngredientRow component and prefab for store inventory list"
```

---

## Task 4: Вкладка "Машина"

**Files:**
- Modify: `Assets/Scenes/Main.unity`

Тут две карточки: текущая машина + (опционально) следующая с прогрессом. Без сложных списков.

- [ ] **Step 1: Контейнер вкладки**

В `StoreScreenPanel/TabContent` → правый клик → `Create Empty` → `MachinePanel`.

- RectTransform: stretch на родителя, L/R/T/B=0
- Add Component → `Vertical Layout Group`: Padding 12, Spacing 16, Force Expand Width, Control Child Size W+H

- [ ] **Step 2: Карточка "Текущая машина"**

Внутри `MachinePanel` → `UI → Image`, переименуй в `CurrentCard`.
- Image → Color: HEX `FFFFFF`
- Layout Element: Preferred Height = `200`

Внутри `CurrentCard`:
1. TMP `CurrentTitle`: Text `Текущая: T1 — Старенькая`, Size 18, Bold, чёрный, Top=12, Left=12, Right=12, H=24
2. UI Image `MachineImage`: HEX `B5C7E5`, центр, W=120, H=120
3. TMP `CurrentDescription`: Text плейсхолдер `Помол: узкая зона. Экстракция: 3.0 сек.`, Size 12, чёрный, Bottom=12, Left=12, Right=12, H=40

- [ ] **Step 3: Карточка "Следующая машина"**

Внутри `MachinePanel` → `UI → Image`, переименуй в `NextCard`.
- Layout Element: Preferred Height = `260`

Внутри:
1. TMP `NextTitle`: Text `Следующая: T2 — Бариста`, Size 18, Bold, чёрный
2. TMP `QuestLine`: Text `Продай 10 американо: 0 / 10`, Size 13, HEX `666666`
3. TMP `PriceLine`: Text `Цена: 1500 ₽`, Size 14, Bold, HEX `2D9F4E`
4. UI Button `BuyMachineButton`: Text `Купить`, Size 14, Bold, белый; Image HEX `5A8DDC`, H=40

(Точное расположение этих элементов — на твой вкус, главное чтобы все были видны.)

- [ ] **Step 4: Save сцены и продолжаем в Task 5**

(контроллеры пишем дальше)

---

## Task 5: Контроллеры вкладок + `StoreScreenController`

**Files:**
- Create: `Assets/Scripts/UI/RecipesTabController.cs`
- Create: `Assets/Scripts/UI/IngredientsTabController.cs`
- Create: `Assets/Scripts/UI/MachineTabController.cs`
- Create: `Assets/Scripts/UI/StoreScreenController.cs`

- [ ] **Step 1: `RecipesTabController.cs`**

В `Assets/Scripts/UI/`:

```csharp
using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Строит и обновляет список рецептов.
    public class RecipesTabController : MonoBehaviour
    {
        [Tooltip("Содержимое ScrollView (RecipesScroll/Viewport/Content)")]
        public Transform listRoot;
        [Tooltip("Prefab строки рецепта")]
        public RecipeRow recipeRowPrefab;

        private readonly List<RecipeRow> _rows = new();
        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            Subscribe();
            Rebuild();
        }

        private void OnDisable()
        {
            Unsubscribe();
            Clear();
        }

        private void Subscribe()
        {
            _gsm.Economy.BalanceChanged += OnAnyChange;
            _gsm.Quests.CountChanged += OnQuestsChanged;
            _gsm.Recipes.RecipeUnlocked += OnRecipeUnlocked;
            _gsm.Machine.Upgraded += OnMachineUpgraded;
        }

        private void Unsubscribe()
        {
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged -= OnAnyChange;
            _gsm.Quests.CountChanged -= OnQuestsChanged;
            _gsm.Recipes.RecipeUnlocked -= OnRecipeUnlocked;
            _gsm.Machine.Upgraded -= OnMachineUpgraded;
        }

        private void OnAnyChange(int _) => RefreshRows();
        private void OnQuestsChanged(string _, int __) => RefreshRows();
        private void OnRecipeUnlocked(RecipeDefinition _) => RefreshRows();
        private void OnMachineUpgraded(MachineTierDefinition _) => RefreshRows();

        private void Rebuild()
        {
            Clear();
            foreach (var recipe in _gsm.GameContent_Recipes())
            {
                var row = Instantiate(recipeRowPrefab, listRoot);
                row.Bind(recipe, _gsm.Recipes, _gsm.Economy);
                row.OnBuyClicked += OnBuyClicked;
                _rows.Add(row);
            }
        }

        private void RefreshRows()
        {
            int i = 0;
            foreach (var recipe in _gsm.GameContent_Recipes())
            {
                if (i >= _rows.Count) break;
                _rows[i].Bind(recipe, _gsm.Recipes, _gsm.Economy);
                i++;
            }
        }

        private void OnBuyClicked(RecipeDefinition recipe)
        {
            if (recipe == null) return;
            bool ok = _gsm.Recipes.TryPurchase(recipe);
            Debug.Log($"[Store] Купить {recipe.id}: {(ok ? "успех" : "неудача")}");
            RefreshRows();
        }

        private void Clear()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
    }
}
```

> ⚠️ Тут используется `_gsm.GameContent_Recipes()` — это helper, который нужно добавить в `GameStateManager`. Делаем это сейчас.

- [ ] **Step 2: Helper в `GameStateManager`**

Открой `Assets/Scripts/Core/GameStateManager.cs`. Перед закрывающим `}` класса (после `ResetProgress` метода) добавь:

```csharp
        /// Удобный доступ к каталогам контента (UI).
        public System.Collections.Generic.IEnumerable<DrinkitGame.Data.RecipeDefinition> GameContent_Recipes()
        {
            return content != null ? (System.Collections.Generic.IEnumerable<DrinkitGame.Data.RecipeDefinition>)content.recipes : System.Array.Empty<DrinkitGame.Data.RecipeDefinition>();
        }

        public System.Collections.Generic.IEnumerable<DrinkitGame.Data.ProductDefinition> GameContent_Products()
        {
            return content != null ? (System.Collections.Generic.IEnumerable<DrinkitGame.Data.ProductDefinition>)content.products : System.Array.Empty<DrinkitGame.Data.ProductDefinition>();
        }
```

- [ ] **Step 3: `IngredientsTabController.cs`**

```csharp
using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.UI
{
    public class IngredientsTabController : MonoBehaviour
    {
        public Transform listRoot;
        public IngredientRow rowPrefab;

        private readonly List<IngredientRow> _rows = new();
        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged += _ => RefreshAll();
            _gsm.Inventory.StockChanged += (_, __) => RefreshAll();
            _gsm.Recipes.RecipeUnlocked += _ => Rebuild(); // открытие нового рецепта может добавить новые продукты в магазин
            Rebuild();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Rebuild()
        {
            Clear();
            foreach (var product in _gsm.GameContent_Products())
            {
                if (!IsRelevant(product)) continue;
                var row = Instantiate(rowPrefab, listRoot);
                row.Bind(product, _gsm.Inventory, _gsm.Economy);
                row.OnBuyClicked += OnBuyClicked;
                _rows.Add(row);
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                // Сохраняем тот же продукт, но обновляем bind
                // (мы хранили продукт в самом row через _product, но не имеем доступа извне —
                //  rebuild через продукт по индексу)
            }
            // Простой подход: пересобрать всё.
            Rebuild();
        }

        private void OnBuyClicked(ProductDefinition product, int amount)
        {
            int unit = product.purchasePrice;
            int totalPrice = amount == 50 ? (int)(unit * 50 * 0.95f) : unit * amount;

            if (_gsm.Economy.TrySpend(totalPrice))
            {
                _gsm.Inventory.Add(product.id, amount);
                Debug.Log($"[Store] Купили {amount}x {product.id} за {totalPrice} ₽");
            }
            else
            {
                Debug.Log($"[Store] Не хватает на {amount}x {product.id}");
            }
        }

        private bool IsRelevant(ProductDefinition product)
        {
            // Показываем только те продукты, которые могут быть использованы в открытых рецептах
            foreach (var recipeId in _gsm.State.unlockedRecipeIds)
            {
                foreach (var recipe in _gsm.GameContent_Recipes())
                {
                    if (recipe.id != recipeId) continue;
                    if (IsProductUsedInRecipe(product, recipe)) return true;
                }
            }
            return false;
        }

        private bool IsProductUsedInRecipe(ProductDefinition product, RecipeDefinition recipe)
        {
            foreach (var ing in recipe.fixedIngredients)
                if (ing.product == product) return true;
            if (product.category == ProductCategory.Milk && recipe.needsMilk) return true;
            if (product.category == ProductCategory.Cream && recipe.needsCream) return true;
            if (product.category == ProductCategory.Syrup && recipe.canHaveSyrup) return true;
            if (product.category == ProductCategory.Topping
                && recipe.compatibleToppings != null
                && recipe.compatibleToppings.Contains(product)) return true;
            if (product.category == ProductCategory.Cup && recipe.canBeToGo) return true;
            return false;
        }

        private void Clear()
        {
            foreach (var row in _rows) if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
    }
}
```

- [ ] **Step 4: `MachineTabController.cs`**

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    public class MachineTabController : MonoBehaviour
    {
        [Header("Current card")]
        public TMP_Text currentTitle;
        public TMP_Text currentDescription;
        public Image currentImage;

        [Header("Next card")]
        public GameObject nextCardRoot;
        public TMP_Text nextTitle;
        public TMP_Text questLine;
        public TMP_Text priceLine;
        public Button buyButton;
        public TMP_Text buyButtonLabel;

        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged += _ => Refresh();
            _gsm.Quests.CountChanged += (_, __) => Refresh();
            _gsm.Machine.Upgraded += _ => Refresh();
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            Refresh();
        }

        private void OnDisable()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuy);
        }

        private void Refresh()
        {
            var cur = _gsm.Machine.CurrentTier;
            if (cur != null && currentTitle != null)
                currentTitle.text = $"Текущая: T{cur.tierIndex} — {cur.displayName}";
            if (cur != null && currentDescription != null)
                currentDescription.text =
                    $"Помол: зона {cur.grindingZoneWidth:0.00}. Экстракция: {cur.extractionTimeSeconds:0.0} сек." +
                    (cur.checkBonusPercent > 0 ? $" Бонус +{cur.checkBonusPercent}%." : "");
            if (cur != null && cur.icon != null && currentImage != null)
                currentImage.sprite = cur.icon;

            var next = _gsm.Machine.NextTier;
            if (next == null)
            {
                if (nextCardRoot != null) nextCardRoot.SetActive(false);
                return;
            }
            if (nextCardRoot != null) nextCardRoot.SetActive(true);

            if (nextTitle != null) nextTitle.text = $"Следующая: T{next.tierIndex} — {next.displayName}";

            var availability = _gsm.Machine.GetUpgradeAvailability();
            string questText = "";
            if (next.questTargetRecipe1 != null && next.questTargetCount1 > 0)
                questText += $"{next.questDescription}: {_gsm.Quests.GetSoldCount(next.questTargetRecipe1.id)} / {next.questTargetCount1}";
            if (next.questTargetRecipe2 != null && next.questTargetCount2 > 0)
                questText += $" + {_gsm.Quests.GetSoldCount(next.questTargetRecipe2.id)} / {next.questTargetCount2}";
            if (questLine != null) questLine.text = questText;

            if (priceLine != null) priceLine.text = $"Цена: {next.purchasePrice} ₽";

            string buyText = availability switch
            {
                UpgradeAvailability.Available => "Купить",
                UpgradeAvailability.NotEnoughMoney => "Не хватает денег",
                UpgradeAvailability.QuestIncomplete => "Выполни квест",
                _ => "—"
            };
            if (buyButtonLabel != null) buyButtonLabel.text = buyText;
            if (buyButton != null) buyButton.interactable = availability == UpgradeAvailability.Available;
        }

        private void OnBuy()
        {
            bool ok = _gsm.Machine.TryUpgrade();
            Debug.Log($"[Store] Upgrade: {(ok ? "успех" : "неудача")}");
            Refresh();
        }
    }
}
```

- [ ] **Step 5: `StoreScreenController.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Главный контроллер магазина: переключает 3 вкладки.
    public class StoreScreenController : MonoBehaviour
    {
        [Header("Tab buttons")]
        public Button recipesTabButton;
        public Button ingredientsTabButton;
        public Button machineTabButton;

        [Header("Tab content roots")]
        public GameObject recipesContent;
        public GameObject ingredientsContent;
        public GameObject machineContent;

        [Header("Tab button colors")]
        public Color activeColor = new(0.353f, 0.553f, 0.863f); // 5A8DDC
        public Color inactiveColor = new(0.710f, 0.780f, 0.898f); // B5C7E5

        private void Awake()
        {
            if (recipesTabButton != null) recipesTabButton.onClick.AddListener(() => ShowTab(0));
            if (ingredientsTabButton != null) ingredientsTabButton.onClick.AddListener(() => ShowTab(1));
            if (machineTabButton != null) machineTabButton.onClick.AddListener(() => ShowTab(2));
        }

        private void OnEnable()
        {
            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            if (recipesContent != null) recipesContent.SetActive(index == 0);
            if (ingredientsContent != null) ingredientsContent.SetActive(index == 1);
            if (machineContent != null) machineContent.SetActive(index == 2);

            SetColor(recipesTabButton, index == 0);
            SetColor(ingredientsTabButton, index == 1);
            SetColor(machineTabButton, index == 2);
        }

        private void SetColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? activeColor : inactiveColor;
        }
    }
}
```

- [ ] **Step 6: Прицепить контроллеры в сцене**

В Hierarchy:

1. `StoreScreenPanel` → Add Component → `Store Screen Controller`. Заполни:
   - Recipes Tab Button: `InnerTabs/Tab_Recipes`
   - Ingredients Tab Button: `InnerTabs/Tab_Ingredients`
   - Machine Tab Button: `InnerTabs/Tab_Machine`
   - Recipes Content: `TabContent/RecipesScroll`
   - Ingredients Content: `TabContent/IngredientsScroll`
   - Machine Content: `TabContent/MachinePanel`

2. `RecipesScroll` (тот scroll view целиком) → Add Component → `Recipes Tab Controller`. Заполни:
   - List Root: `RecipesScroll/Viewport/Content`
   - Recipe Row Prefab: перетащи `Assets/Prefabs/RecipeRow.prefab`

3. `IngredientsScroll` → Add Component → `Ingredients Tab Controller`. Заполни:
   - List Root: `IngredientsScroll/Viewport/Content`
   - Row Prefab: `Assets/Prefabs/IngredientRow.prefab`

4. `MachinePanel` → Add Component → `Machine Tab Controller`. Заполни поля (Current Title/Description/Image; Next Card Root = `NextCard`; Next Title / Quest Line / Price Line / Buy Button / Buy Button Label из дочерних элементов NextCard).

- [ ] **Step 7: Compile и Commit**

```bash
git add Assets/Scripts/UI Assets/Scripts/Core/GameStateManager.cs Assets/Scenes/Main.unity && git commit -m "feat(ui): StoreScreenController + 3 tab controllers (recipes/ingredients/machine)"
```

---

## Task 6: Подключить открытие/закрытие Store в UIRouter и таб-баре

**Files:**
- Modify: `Assets/Scripts/UI/UIRouter.cs`
- Modify: `Assets/Scripts/UI/TabBarPlaceholderController.cs` (переименуем в `TabBarController`)
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Расширить `UIRouter`**

В `Assets/Scripts/UI/UIRouter.cs`:

Найди блок полей:
```csharp
        public GameObject mainScreenPanel;
        public GameObject cookingScreenPanel;
        public GameObject orderResultPopup;
```

И добавь после него:
```csharp
        public GameObject storeScreenPanel;
```

Добавь метод `OpenStore`:

```csharp
        public void OpenStore()
        {
            SetActive(mainScreenPanel, false);
            SetActive(cookingScreenPanel, false);
            SetActive(storeScreenPanel, true);
            SetActive(orderResultPopup, false);
        }
```

В методе `ShowMain` добавь `SetActive(storeScreenPanel, false);` (рядом с другими).

В методе `OpenCooking` тоже `SetActive(storeScreenPanel, false);`.

- [ ] **Step 2: Заполнить ссылку в инспекторе**

Canvas → UIRouter → Store Screen Panel: перетащи `StoreScreenPanel`.

- [ ] **Step 3: Поменять `TabBarPlaceholderController.cs`**

Открой файл, **полностью замени** содержимое на:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Нижний таб-бар: Главная / Магазин. Магазин открывается через UIRouter.
    public class TabBarPlaceholderController : MonoBehaviour
    {
        public Button homeTab;
        public Button storeTab;

        private void Start()
        {
            if (homeTab != null)
                homeTab.onClick.AddListener(() => UIRouter.Instance?.ShowMain());
            if (storeTab != null)
                storeTab.onClick.AddListener(() => UIRouter.Instance?.OpenStore());
        }
    }
}
```

- [ ] **Step 4: Save и Play тест**

1. Запусти Play
2. Жми "Магазин" внизу → открывается Store, вкладка Рецепты по умолчанию
3. Видишь 8 строк рецептов с правильными статусами (Эспрессо — Открыт; Американо — Можно купить за 100 ₽; остальные — заблокированы)
4. Подожди появления заказа, тапни, "Выдать" → накопи 100 ₽
5. В магазине жми "Купить" у Американо → деньги списались, статус сменился на "Открыт"
6. Закрой магазин (Назад), подожди — теперь иногда спавнятся американо (вес 4 у нового)
7. Переключи на вкладку "Ингредиенты" — видишь список SKU с остатками. Жми "+1" у молока → купил
8. Переключи на "Машина" — видишь карточку T1 и карточку "Следующая T2" с квестом "Продай 10 американо: 0 / 10"

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/UI/UIRouter.cs Assets/Scripts/UI/TabBarPlaceholderController.cs Assets/Scenes/Main.unity && git commit -m "feat(ui): wire Store tab and UIRouter.OpenStore()"
```

---

## Task 7: Кнопка "Назад" в магазине

**Files:**
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Подключить `BackButton` в `StoreScreenPanel`**

В Hierarchy выбери `StoreScreenPanel/BackButton` → в Inspector компонент Button → раздел `On Click ()` → жми `+`.

Не хочется писать отдельный скрипт ради одной кнопки. Сделаем через инлайн вызов: перетащи `Canvas` в слот объекта → выпадающий метод выбери `UIRouter.ShowMain()`.

Если выпадайка не показывает `UIRouter.ShowMain` — добавь публичную обёртку. Или сделаем мелкий скрипт `Assets/Scripts/UI/BackToMainButton.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    [RequireComponent(typeof(Button))]
    public class BackToMainButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => UIRouter.Instance?.ShowMain());
        }
    }
}
```

Прицепи `BackToMainButton` на `BackButton` в магазине.

- [ ] **Step 2: Save и Play — кнопка "Назад" в магазине возвращает на главный**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/BackToMainButton.cs Assets/Scripts/UI/BackToMainButton.cs.meta Assets/Scenes/Main.unity && git commit -m "feat(ui): BackToMainButton component for store back button"
```

---

## Task 8: Финальная сверка Phase 7

- [ ] **Step 1: Все тесты зелёные** (Run All в Test Runner)

- [ ] **Step 2: Лайв сценарий — путь до T2**

1. Старт игры (баланс 0₽)
2. Дождись первого заказа, выдай → +130 ₽
3. Открой магазин → купи рецепт Американо за 100 ₽
4. Закрой магазин, начнут появляться американо
5. Продай ~10 американо
6. Открой магазин → вкладка "Машина" → теперь Купить активна (квест выполнен, денег хватает) → жми Купить
7. Машина сменилась на T2, в топ-баре Goal стал "Купи рецепт Капучино" или "Купи рецепт Какао" — и т.д.

- [ ] **Step 3: Console чистая, git log**

---

## Self-Review

После прохождения:
1. ✅ Магазин открывается через таб-бар внизу
2. ✅ 3 внутренние вкладки переключаются
3. ✅ Рецепты покупаются за деньги, гейтятся машиной и квестами
4. ✅ Ингредиенты докупаются (+1/+10/+50)
5. ✅ Машина прокачивается с квестом и ценой
6. ✅ Goal-tracker обновляется по мере прогресса

**Готово → Phase 8a: Cooking flow (полноценный пошаговый, без мини-игр).**

---

## Common Pitfalls

**1. ScrollView не скроллится / Content пустой**
Причина: на `Content` нет `Content Size Fitter` или Layout Group настроен криво. Фикс: проверь что `Content` имеет `Vertical Layout Group` + `Content Size Fitter` (Vertical Fit = Preferred).

**2. Prefab при инстанциировании не показывается**
Причина: `listRoot` в инспекторе указан не на `Content`, а на сам ScrollView. Фикс: перетащи именно `RecipesScroll/Viewport/Content`.

**3. Кнопка "Купить" всегда серая**
Причина: `Recipes.GetAvailability` возвращает не Available. Лог в `RecipeRow.Bind` — что именно возвращает. Чаще всего: не куплена машина (NeedsHigherMachine) или не выполнен квест (NeedsMoreSales).

**4. Покупка ингредиента "+50" — кнопка не нажимается даже когда есть деньги**
Причина: при цене 15₽ × 50 × 0.95 = 712₽. Если баланс точно 712 — округление может дать false. Фикс: проверь что баланс действительно ≥ targetPrice.

**5. После открытия магазина быстро возвращаешься — на главном пусто**
Причина: `OnEnable/OnDisable` в TabController рвут подписки. Норм — при следующем OnEnable строится заново. Если виснет — проверь что `Rebuild` действительно вызывается в OnEnable.

**6. "Random" ambiguity — здесь не используется**, но если будешь добавлять — всегда `System.Random`.
