# Phase 2 — Data Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Описать все типы данных игры как `ScriptableObject` и создать assets для 8 рецептов, 15 продуктов, 3 уровней кофемашины, 9 секторов колеса. После фазы — в Unity есть полный каталог контента, на который дальше будут опираться сервисы.

**Architecture:** Каждый тип контента — `ScriptableObject` с атрибутом `[CreateAssetMenu]`. Один master-registry `GameContent` хранит ссылки на все остальные ассеты — это единая точка входа для сервисов в Phase 3. Кросс-ссылки между ScriptableObject (рецепт → машина → продукт) разрешены и нормальны.

**Tech Stack:** C# 9 · Unity 2022.3 · NUnit (для Edit Mode тестов)

**Конец фазы:** В `Assets/Data/` лежат 36 ассетов (15 + 3 + 8 + 9 + 1 GameContent). Edit Mode тест валидирует целостность: все ссылки не null, шансы колеса в сумме 100%.

---

## Task 1: Создать enum-типы

**Files:**
- Create: `Assets/Scripts/Data/Enums.cs`

Один файл с несколькими enum'ами — для прототипа достаточно. Дробить на отдельные файлы будем когда станет неудобно.

- [ ] **Step 1: Создать файл `Enums.cs`**

В Unity Project → `Assets/Scripts/Data/` → правый клик → `Create → C# Script` → имя `Enums`. Открой в IDE, **полностью замени** содержимое на:

```csharp
namespace DrinkitGame.Data
{
    /// Категория продукта на складе.
    public enum ProductCategory
    {
        Beans,         // Зерно
        Milk,          // Молоко (любого типа)
        Cream,         // Сливки
        Powder,        // Порошок (матча, какао)
        Syrup,         // Сироп
        Topping,       // Топпинг (корица, какао-посыпка, зефирки)
        Cup            // Стакан "с собой"
    }

    /// Семейство рецепта — определяет схему готовки и какие мини-игры запускаются.
    public enum RecipeFamily
    {
        Espresso,      // эспрессо: помол + экстракция
        Americano,     // эспрессо + горячая вода
        Cappuccino,    // эспрессо + молоко взбитое
        Latte,         // как cappuccino, но другие пропорции
        Raf,           // эспрессо + сливки взбитые
        Filter,        // помол + проливание через V60
        Matcha,        // матча + венчик + (опционально) молоко
        Cacao          // какао + молоко взбитое
    }

    /// Типы мини-игр (используются в Phase 8 для готовки).
    public enum MiniGameType
    {
        None,
        Grinding,      // M1 — помол
        MilkSteaming,  // M2 — вспенивание молока/сливок
        PourOver,      // M3 — проливание (long-tap)
        Whisking       // M4 — взбивание венчиком
    }

    /// Категории модификаторов в заказе (что клиент может попросить сверху).
    public enum ModifierCategory
    {
        MilkType,      // какое молоко (для рецептов с молоком)
        Syrup,         // какой сироп
        Topping,       // какой топпинг
        Container      // тут или с собой
    }

    /// Что выдаёт сектор колеса.
    public enum WheelPrizeType
    {
        Coins,                // деньги
        IngredientPack,       // пачка ингредиента (например молоко x10)
        DiscountVoucher,      // -50% на следующий рецепт
        DoubleNextOrder,      // следующий заказ платит x2
        Nothing               // "не повезло"
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

В Unity внизу справа должна крутиться иконка ~3-5 секунд. После — посмотри Console (`Window → General → Console`). Не должно быть ошибок.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): core enums for products, recipes, mini-games, modifiers, wheel"
```

---

## Task 2: Создать struct `IngredientAmount`

**Files:**
- Create: `Assets/Scripts/Data/IngredientAmount.cs`

Сериализуемая структура "1 единица такого-то продукта" — пригодится в рецептах (`фиксированные ингредиенты`) и в призах колеса (`пачка из N единиц`).

- [ ] **Step 1: Создать файл `IngredientAmount.cs`**

В `Assets/Scripts/Data/` создай скрипт `IngredientAmount`. Замени содержимое на:

```csharp
using System;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Пара "продукт + количество". Используется в фиксированных ингредиентах
    /// рецепта и в призах колеса.
    [Serializable]
    public struct IngredientAmount
    {
        [Tooltip("ScriptableObject продукта.")]
        public ProductDefinition product;

        [Tooltip("Количество единиц этого продукта.")]
        [Min(1)]
        public int amount;

        public IngredientAmount(ProductDefinition product, int amount)
        {
            this.product = product;
            this.amount = amount;
        }
    }
}
```

Файл сейчас не скомпилится — `ProductDefinition` ещё не определён. Это нормально, исправим в Task 3.

---

## Task 3: ScriptableObject — `ProductDefinition`

**Files:**
- Create: `Assets/Scripts/Data/ProductDefinition.cs`

15 SKU в инвентаре — все типы будут описаны одним ScriptableObject.

- [ ] **Step 1: Создать `ProductDefinition.cs`**

В `Assets/Scripts/Data/` создай скрипт `ProductDefinition`. Замени содержимое на:

```csharp
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Описание одного продукта (зерно, молоко, сироп и т.д.).
    /// Один ассет = один SKU на складе.
    [CreateAssetMenu(
        fileName = "Product_",
        menuName = "DrinkitGame/Product",
        order = 10)]
    public class ProductDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Уникальный технический ID, например 'beans', 'milk_oat'.")]
        public string id;

        [Tooltip("Отображаемое название для UI.")]
        public string displayName;

        [Tooltip("Иконка в UI инвентаря и заказов. Можно временно null (плейсхолдер).")]
        public Sprite icon;

        [Header("Category")]
        [Tooltip("Категория продукта — для логики генерации заказов и UI.")]
        public ProductCategory category;

        [Tooltip(
            "Только для категории Milk. true означает 'премиум' молоко "
            + "(овсяное/кокосовое/миндальное), за которое клиент платит надбавку.")]
        public bool isPremiumMilk;

        [Header("Economy")]
        [Tooltip("Закупочная цена за 1 единицу (₽).")]
        [Min(0)]
        public int purchasePrice;

        [Tooltip(
            "Надбавка к чеку, если этот продукт используется как модификатор заказа. "
            + "60 для премиум-молока, 40 для сиропов, 30 для топпингов, "
            + "0 для базовых (зерно, коровье молоко, стакан с собой).")]
        [Min(0)]
        public int sellMarkup;
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

Console чистая, без ошибок.

- [ ] **Step 3: Проверить меню `Create`**

В Project панели правый клик → `Create → DrinkitGame` → должен появиться пункт `Product`. **Не создавай ассет сейчас** — это пригодится в Task 8.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): ProductDefinition ScriptableObject (15 SKUs)"
```

---

## Task 4: ScriptableObject — `MachineTierDefinition`

**Files:**
- Create: `Assets/Scripts/Data/MachineTierDefinition.cs`

3 уровня кофемашины (T1/T2/T3). Каждый тир описывает свои параметры мини-игр и квест для покупки.

- [ ] **Step 1: Создать `MachineTierDefinition.cs`**

В `Assets/Scripts/Data/` создай скрипт `MachineTierDefinition`. Замени содержимое на:

```csharp
using System;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Один уровень кофемашины (T1/T2/T3).
    [CreateAssetMenu(
        fileName = "Machine_",
        menuName = "DrinkitGame/Machine Tier",
        order = 20)]
    public class MachineTierDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Индекс тира: 1, 2 или 3.")]
        [Min(1)]
        public int tierIndex;

        [Tooltip("Отображаемое название: 'Старенькая', 'Бариста', 'Профи'.")]
        public string displayName;

        [Tooltip("Спрайт кофемашины в UI. Можно временно null.")]
        public Sprite icon;

        [Header("Purchase")]
        [Tooltip("Стоимость покупки этого уровня (₽). 0 для стартового T1.")]
        [Min(0)]
        public int purchasePrice;

        [Tooltip(
            "Описание квеста для UI (например, 'Продай 10 американо'). "
            + "Пусто для стартового T1.")]
        public string questDescription;

        [Tooltip(
            "Какой рецепт нужно продавать для квеста на покупку этого тира. "
            + "null для T1.")]
        public RecipeDefinition questTargetRecipe1;

        [Tooltip("Сколько нужно продать quest target 1.")]
        [Min(0)]
        public int questTargetCount1;

        [Tooltip("Второй рецепт квеста (например 'продай 5 капучино + 5 латте'). null если не нужно.")]
        public RecipeDefinition questTargetRecipe2;

        [Tooltip("Сколько нужно продать quest target 2.")]
        [Min(0)]
        public int questTargetCount2;

        [Header("Gameplay parameters")]
        [Tooltip(
            "Ширина зелёной зоны для мини-игры помола (0..1, доля от полосы). "
            + "T1: узкая (0.15), T2: средняя (0.25), T3: широкая (0.4).")]
        [Range(0.05f, 0.5f)]
        public float grindingZoneWidth = 0.25f;

        [Tooltip("Ширина зелёной зоны для вспенивания молока (0..1).")]
        [Range(0.05f, 0.5f)]
        public float milkSteamingZoneWidth = 0.25f;

        [Tooltip("Ширина зелёной зоны для проливания фильтра (0..1).")]
        [Range(0.05f, 0.5f)]
        public float pourOverZoneWidth = 0.25f;

        [Tooltip(
            "Длительность авто-экстракции эспрессо, сек. "
            + "T1: 3.0, T2: 2.0, T3: 1.0.")]
        [Range(0.5f, 5f)]
        public float extractionTimeSeconds = 3f;

        [Tooltip(
            "Премиум-бонус: +N% к финальному чеку всех напитков. "
            + "T1: 0, T2: 0, T3: 10.")]
        [Range(0, 50)]
        public int checkBonusPercent;
    }
}
```

- [ ] **Step 2: Дождаться компиляции (опять Console чистая)**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): MachineTierDefinition ScriptableObject (3 tiers)"
```

---

## Task 5: ScriptableObject — `RecipeDefinition`

**Files:**
- Create: `Assets/Scripts/Data/RecipeDefinition.cs`

Самый большой тип — описание рецепта со всеми его условиями.

- [ ] **Step 1: Создать `RecipeDefinition.cs`**

В `Assets/Scripts/Data/` создай скрипт `RecipeDefinition`. Замени содержимое на:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Описание одного рецепта (один напиток в каталоге).
    [CreateAssetMenu(
        fileName = "Recipe_",
        menuName = "DrinkitGame/Recipe",
        order = 30)]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Уникальный технический ID, например 'espresso', 'cappuccino'.")]
        public string id;

        [Tooltip("Отображаемое название для UI.")]
        public string displayName;

        [Tooltip("Иконка напитка для UI заказа и каталога.")]
        public Sprite icon;

        [Header("Recipe family")]
        [Tooltip("Семейство — определяет схему готовки в Phase 8.")]
        public RecipeFamily family;

        [Header("Economy")]
        [Tooltip("Базовая цена продажи (₽), без модификаторов и бонусов.")]
        [Min(0)]
        public int basePrice;

        [Tooltip("Стоимость покупки самого рецепта в Магазине (₽). 0 для стартового эспрессо.")]
        [Min(0)]
        public int recipePurchasePrice;

        [Header("Machine gating")]
        [Tooltip("Минимальный тир кофемашины, который нужен для приготовления.")]
        public MachineTierDefinition requiredMachineTier;

        [Header("Ingredients")]
        [Tooltip(
            "Фиксированные ингредиенты — продукты, которые всегда нужны "
            + "(кроме категории, выбираемой заказом). "
            + "Например для эспрессо: [Beans x1]. Для матчи: [Matcha x1]. "
            + "Для капучино: [Beans x1] — молоко выбирается заказом.")]
        public List<IngredientAmount> fixedIngredients = new();

        [Tooltip(
            "Нужно ли молоко (любого типа). Если true — у заказа всегда "
            + "будет указан тип молока (коровье/овсяное/кокос/миндаль).")]
        public bool needsMilk;

        [Tooltip("Нужны ли сливки (только raf).")]
        public bool needsCream;

        [Header("Optional modifiers")]
        [Tooltip("Может ли заказ просить сироп.")]
        public bool canHaveSyrup;

        [Tooltip("Какие топпинги допустимы для этого рецепта (если есть).")]
        public List<ProductDefinition> compatibleToppings = new();

        [Tooltip("Может ли быть 'с собой' (бумажный стакан).")]
        public bool canBeToGo = true;

        [Header("Unlock condition")]
        [Tooltip(
            "Какой рецепт нужно продавать для разблокировки этого. "
            + "null = нет квеста, доступен сразу при наличии денег и машины.")]
        public RecipeDefinition unlockQuestTargetRecipe;

        [Tooltip("Сколько нужно продать для разблокировки.")]
        [Min(0)]
        public int unlockQuestTargetCount;

        [Tooltip("Описание квеста для UI.")]
        public string unlockQuestDescription;
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): RecipeDefinition ScriptableObject (8 recipes)"
```

---

## Task 6: ScriptableObject — `WheelSectorDefinition`

**Files:**
- Create: `Assets/Scripts/Data/WheelSectorDefinition.cs`

9 секторов колеса.

- [ ] **Step 1: Создать `WheelSectorDefinition.cs`**

В `Assets/Scripts/Data/` создай скрипт `WheelSectorDefinition`. Замени содержимое на:

```csharp
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Один сектор колеса удачи.
    [CreateAssetMenu(
        fileName = "Wheel_",
        menuName = "DrinkitGame/Wheel Sector",
        order = 40)]
    public class WheelSectorDefinition : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Короткое описание приза для UI: '50 ₽', 'Молоко x10', и т.д.")]
        public string displayLabel;

        [Tooltip("Иконка приза для UI колеса (может быть null временно).")]
        public Sprite icon;

        [Header("Probability")]
        [Tooltip("Шанс выпадения в процентах (0..100). Сумма всех секторов должна быть = 100.")]
        [Range(0, 100)]
        public int probabilityPercent;

        [Header("Prize")]
        [Tooltip("Что выдаёт этот сектор.")]
        public WheelPrizeType prizeType;

        [Tooltip("Количество монет (только для Coins).")]
        [Min(0)]
        public int coinsAmount;

        [Tooltip("Какой продукт выдать (только для IngredientPack).")]
        public ProductDefinition packProduct;

        [Tooltip("Сколько единиц этого продукта (только для IngredientPack).")]
        [Min(0)]
        public int packQuantity;
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): WheelSectorDefinition ScriptableObject (9 sectors)"
```

---

## Task 7: ScriptableObject — `GameContent` (master registry)

**Files:**
- Create: `Assets/Scripts/Data/GameContent.cs`

Единый ассет, который держит ссылки на ВСЕ остальные. Сервисы получают `GameContent` через инспектор и читают всё из него.

- [ ] **Step 1: Создать `GameContent.cs`**

В `Assets/Scripts/Data/` создай скрипт `GameContent`. Замени содержимое на:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Корневой ассет со ссылками на весь игровой контент.
    /// Создаётся ОДИН ассет на проект; передаётся сервисам через инспектор.
    [CreateAssetMenu(
        fileName = "GameContent",
        menuName = "DrinkitGame/Game Content (root)",
        order = 0)]
    public class GameContent : ScriptableObject
    {
        [Header("Products (15)")]
        public List<ProductDefinition> products = new();

        [Header("Machine tiers (3)")]
        public List<MachineTierDefinition> machineTiers = new();

        [Header("Recipes (8)")]
        public List<RecipeDefinition> recipes = new();

        [Header("Wheel sectors (9)")]
        public List<WheelSectorDefinition> wheelSectors = new();

        [Header("Starter setup")]
        [Tooltip("Рецепт, открытый с самого начала игры (эспрессо).")]
        public RecipeDefinition starterRecipe;

        [Tooltip("Стартовый тир кофемашины (T1).")]
        public MachineTierDefinition starterMachineTier;

        [Tooltip("Стартовый баланс игрока в рублях.")]
        public int starterBalance = 0;

        [Tooltip(
            "Стартовый запас зерна (чтоб игрок мог сделать первые эспрессо). "
            + "0 = игрок должен купить с самого начала; обычно 5-10 для онбординга.")]
        public int starterBeansStock = 10;
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Data && git commit -m "feat(data): GameContent master registry ScriptableObject"
```

---

## Task 8: Создать 15 ассетов продуктов

**Files:**
- Create: 15 ассетов под `Assets/Data/Products/`

Создаём каждый продукт через Project → Create → DrinkitGame → Product, переименовываем, заполняем поля в Inspector.

- [ ] **Step 1: Создать все 15 ассетов**

В Project зайди в `Assets/Data/Products/`. Для каждого продукта ниже:
1. Правый клик → `Create → DrinkitGame → Product`
2. Переименуй файл как указано в колонке `Asset name`
3. Выбери созданный ассет, в Inspector заполни поля (`id`, `displayName`, `category`, `isPremiumMilk`, `purchasePrice`, `sellMarkup`)

| Asset name | id | displayName | category | isPremiumMilk | purchasePrice | sellMarkup |
|---|---|---|---|---|---|---|
| `Product_Beans` | `beans` | `Кофе зерно` | `Beans` | — | `15` | `0` |
| `Product_MilkCow` | `milk_cow` | `Молоко коровье` | `Milk` | `false` | `25` | `0` |
| `Product_MilkOat` | `milk_oat` | `Молоко овсяное` | `Milk` | `true` | `60` | `60` |
| `Product_MilkCoconut` | `milk_coconut` | `Молоко кокосовое` | `Milk` | `true` | `70` | `60` |
| `Product_MilkAlmond` | `milk_almond` | `Молоко миндальное` | `Milk` | `true` | `70` | `60` |
| `Product_Cream` | `cream` | `Сливки` | `Cream` | — | `80` | `0` |
| `Product_MatchaPowder` | `matcha_powder` | `Матча-порошок` | `Powder` | — | `90` | `0` |
| `Product_CacaoPowder` | `cacao_powder` | `Какао-порошок` | `Powder` | — | `40` | `0` |
| `Product_SyrupVanilla` | `syrup_vanilla` | `Сироп ваниль` | `Syrup` | — | `30` | `40` |
| `Product_SyrupCaramel` | `syrup_caramel` | `Сироп карамель` | `Syrup` | — | `30` | `40` |
| `Product_SyrupHazelnut` | `syrup_hazelnut` | `Сироп лесной орех` | `Syrup` | — | `30` | `40` |
| `Product_Cinnamon` | `topping_cinnamon` | `Корица` | `Topping` | — | `10` | `30` |
| `Product_CacaoDust` | `topping_cacao_dust` | `Какао-посыпка` | `Topping` | — | `10` | `30` |
| `Product_Marshmallow` | `topping_marshmallow` | `Зефирки` | `Topping` | — | `20` | `30` |
| `Product_CupTakeaway` | `cup_takeaway` | `Стакан с собой` | `Cup` | — | `15` | `0` |

Для строк, где `isPremiumMilk` отмечен прочерком (`—`) — поле всё равно есть в инспекторе, но оставь его в дефолтном `false`.

- [ ] **Step 2: Проверить количество**

В терминале:
```bash
ls /Users/anashkin/DrinkitGame/Assets/Data/Products/*.asset | wc -l
```

Expected: `15`

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/Products && git commit -m "feat(data): seed 15 product assets"
```

---

## Task 9: Создать 3 ассета уровней кофемашины

**Files:**
- Create: 3 ассета под `Assets/Data/Machines/`

- [ ] **Step 1: Создать ассеты T1, T2, T3**

В `Assets/Data/Machines/` для каждого тира:
1. Правый клик → `Create → DrinkitGame → Machine Tier`
2. Переименуй файл
3. В инспекторе заполни:

**`Machine_T1`** (стартовая):
- tierIndex: `1`
- displayName: `Старенькая`
- purchasePrice: `0`
- questDescription: *(пусто)*
- questTargetRecipe1: *(пусто — оставь None)*
- questTargetCount1: `0`
- questTargetRecipe2: *(пусто)*
- questTargetCount2: `0`
- grindingZoneWidth: `0.15`
- milkSteamingZoneWidth: `0.15`
- pourOverZoneWidth: `0.15`
- extractionTimeSeconds: `3.0`
- checkBonusPercent: `0`

**`Machine_T2`** (бариста):
- tierIndex: `2`
- displayName: `Бариста`
- purchasePrice: `1500`
- questDescription: `Продай 10 американо`
- questTargetRecipe1: *(оставь None — настроим в Task 12 после создания рецептов)*
- questTargetCount1: `10`
- questTargetRecipe2: *(пусто)*
- questTargetCount2: `0`
- grindingZoneWidth: `0.25`
- milkSteamingZoneWidth: `0.25`
- pourOverZoneWidth: `0.25`
- extractionTimeSeconds: `2.0`
- checkBonusPercent: `0`

**`Machine_T3`** (профи):
- tierIndex: `3`
- displayName: `Профи`
- purchasePrice: `5000`
- questDescription: `Продай 5 капучино + 5 латте`
- questTargetRecipe1: *(None пока)*
- questTargetCount1: `5`
- questTargetRecipe2: *(None пока)*
- questTargetCount2: `5`
- grindingZoneWidth: `0.40`
- milkSteamingZoneWidth: `0.40`
- pourOverZoneWidth: `0.40`
- extractionTimeSeconds: `1.0`
- checkBonusPercent: `10`

- [ ] **Step 2: Проверить количество**

```bash
ls /Users/anashkin/DrinkitGame/Assets/Data/Machines/*.asset | wc -l
```

Expected: `3`

- [ ] **Step 3: Commit (без ссылок на рецепты пока)**

```bash
git add Assets/Data/Machines && git commit -m "feat(data): seed 3 machine tier assets (T1/T2/T3)"
```

---

## Task 10: Создать 8 ассетов рецептов

**Files:**
- Create: 8 ассетов под `Assets/Data/Recipes/`

- [ ] **Step 1: Создать ассеты**

В `Assets/Data/Recipes/` для каждого рецепта правый клик → `Create → DrinkitGame → Recipe`. Переименуй и заполни поля.

**`Recipe_Espresso`** (старт, бесплатно):
- id: `espresso`
- displayName: `Эспрессо`
- family: `Espresso`
- basePrice: `130`
- recipePurchasePrice: `0`
- requiredMachineTier: перетащи `Machine_T1`
- fixedIngredients: добавь 1 элемент → product=`Product_Beans`, amount=`1`
- needsMilk: `false`
- needsCream: `false`
- canHaveSyrup: `true`
- compatibleToppings: `Product_Cinnamon`
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто
- unlockQuestTargetCount: `0`
- unlockQuestDescription: пусто

**`Recipe_Americano`**:
- id: `americano`
- displayName: `Американо`
- family: `Americano`
- basePrice: `160`
- recipePurchasePrice: `100`
- requiredMachineTier: `Machine_T1`
- fixedIngredients: `Product_Beans` x`1`
- needsMilk: `false`
- needsCream: `false`
- canHaveSyrup: `true`
- compatibleToppings: `Product_Cinnamon`
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто
- unlockQuestTargetCount: `0`

**`Recipe_Cappuccino`**:
- id: `cappuccino`
- displayName: `Капучино`
- family: `Cappuccino`
- basePrice: `250`
- recipePurchasePrice: `500`
- requiredMachineTier: `Machine_T2`
- fixedIngredients: `Product_Beans` x`1`
- needsMilk: `true`
- needsCream: `false`
- canHaveSyrup: `true`
- compatibleToppings: добавь `Product_Cinnamon` и `Product_CacaoDust`
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто (просто за деньги после T2)
- unlockQuestTargetCount: `0`

**`Recipe_Latte`**:
- id: `latte`
- displayName: `Латте`
- family: `Latte`
- basePrice: `280`
- recipePurchasePrice: `600`
- requiredMachineTier: `Machine_T2`
- fixedIngredients: `Product_Beans` x`1`
- needsMilk: `true`
- needsCream: `false`
- canHaveSyrup: `true`
- compatibleToppings: `Product_Cinnamon`, `Product_CacaoDust`
- canBeToGo: `true`
- unlockQuestTargetRecipe: `Recipe_Cappuccino`
- unlockQuestTargetCount: `15`
- unlockQuestDescription: `Продай 15 капучино`

**`Recipe_Cacao`**:
- id: `cacao`
- displayName: `Какао`
- family: `Cacao`
- basePrice: `270`
- recipePurchasePrice: `400`
- requiredMachineTier: `Machine_T2`
- fixedIngredients: `Product_CacaoPowder` x`1`
- needsMilk: `true`
- needsCream: `false`
- canHaveSyrup: `false`
- compatibleToppings: `Product_Marshmallow`
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто
- unlockQuestTargetCount: `0`

**`Recipe_Raf`**:
- id: `raf`
- displayName: `Раф`
- family: `Raf`
- basePrice: `320`
- recipePurchasePrice: `1000`
- requiredMachineTier: `Machine_T2`
- fixedIngredients: `Product_Beans` x`1`
- needsMilk: `false`
- needsCream: `true`
- canHaveSyrup: `true`
- compatibleToppings: `Product_Cinnamon`, `Product_CacaoDust`
- canBeToGo: `true`
- unlockQuestTargetRecipe: `Recipe_Latte`
- unlockQuestTargetCount: `10`
- unlockQuestDescription: `Продай 10 латте`

**`Recipe_Filter`**:
- id: `filter`
- displayName: `Фильтр`
- family: `Filter`
- basePrice: `220`
- recipePurchasePrice: `2000`
- requiredMachineTier: `Machine_T3`
- fixedIngredients: `Product_Beans` x`1`
- needsMilk: `false`
- needsCream: `false`
- canHaveSyrup: `true`
- compatibleToppings: `Product_Cinnamon`
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто
- unlockQuestTargetCount: `0`

**`Recipe_Matcha`**:
- id: `matcha`
- displayName: `Матча`
- family: `Matcha`
- basePrice: `330`
- recipePurchasePrice: `1500`
- requiredMachineTier: `Machine_T3`
- fixedIngredients: `Product_MatchaPowder` x`1`
- needsMilk: `true`
- needsCream: `false`
- canHaveSyrup: `false`
- compatibleToppings: пусто
- canBeToGo: `true`
- unlockQuestTargetRecipe: пусто
- unlockQuestTargetCount: `0`

- [ ] **Step 2: Проверить**

```bash
ls /Users/anashkin/DrinkitGame/Assets/Data/Recipes/*.asset | wc -l
```

Expected: `8`

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/Recipes && git commit -m "feat(data): seed 8 recipe assets (espresso → matcha)"
```

---

## Task 11: Дозаполнить ссылки в Machine_T2 и Machine_T3 (квесты)

Теперь, когда рецепты созданы, можно прицепить их к машинам.

- [ ] **Step 1: Открыть `Machine_T2`**

В `Assets/Data/Machines/Machine_T2` → Inspector:
- questTargetRecipe1: перетащи `Recipe_Americano`
- (остальное уже заполнено)

- [ ] **Step 2: Открыть `Machine_T3`**

- questTargetRecipe1: `Recipe_Cappuccino`
- questTargetRecipe2: `Recipe_Latte`

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/Machines && git commit -m "feat(data): wire machine quest references to recipes"
```

---

## Task 12: Создать 9 ассетов секторов колеса

**Files:**
- Create: 9 ассетов под `Assets/Data/WheelSectors/`

- [ ] **Step 1: Создать ассеты**

В `Assets/Data/WheelSectors/` для каждого сектора правый клик → `Create → DrinkitGame → Wheel Sector`. Переименуй и заполни:

**`Wheel_Coins50`**:
- displayLabel: `50 ₽`
- probabilityPercent: `22`
- prizeType: `Coins`
- coinsAmount: `50`
- packProduct: пусто; packQuantity: `0`

**`Wheel_Coins200`**:
- displayLabel: `200 ₽`
- probabilityPercent: `18`
- prizeType: `Coins`
- coinsAmount: `200`

**`Wheel_Coins500`**:
- displayLabel: `500 ₽`
- probabilityPercent: `10`
- prizeType: `Coins`
- coinsAmount: `500`

**`Wheel_Coins1500`**:
- displayLabel: `1 500 ₽`
- probabilityPercent: `3`
- prizeType: `Coins`
- coinsAmount: `1500`

**`Wheel_MilkPack`**:
- displayLabel: `Молоко ×10`
- probabilityPercent: `14`
- prizeType: `IngredientPack`
- coinsAmount: `0`
- packProduct: перетащи `Product_MilkCow`
- packQuantity: `10`

**`Wheel_BeansPack`**:
- displayLabel: `Зерно ×20`
- probabilityPercent: `14`
- prizeType: `IngredientPack`
- packProduct: `Product_Beans`
- packQuantity: `20`

**`Wheel_DiscountVoucher`**:
- displayLabel: `−50% на рецепт`
- probabilityPercent: `7`
- prizeType: `DiscountVoucher`

**`Wheel_DoubleOrder`**:
- displayLabel: `Заказ ×2`
- probabilityPercent: `7`
- prizeType: `DoubleNextOrder`

**`Wheel_Nothing`**:
- displayLabel: `Не повезло`
- probabilityPercent: `5`
- prizeType: `Nothing`

Сумма шансов: 22+18+10+3+14+14+7+7+5 = **100** ✓

- [ ] **Step 2: Проверить**

```bash
ls /Users/anashkin/DrinkitGame/Assets/Data/WheelSectors/*.asset | wc -l
```

Expected: `9`

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/WheelSectors && git commit -m "feat(data): seed 9 wheel sector assets (sum = 100%)"
```

---

## Task 13: Создать корневой `GameContent` ассет и связать всё

**Files:**
- Create: `Assets/Data/GameContent.asset`

- [ ] **Step 1: Создать ассет**

В `Assets/Data/` (не в подпапке!) → правый клик → `Create → DrinkitGame → Game Content (root)`. Файл оставь с именем `GameContent`.

- [ ] **Step 2: Заполнить ссылки**

Выбери `GameContent` в Project → в Inspector:

**`products`** — нажми `+` 15 раз, перетащи туда все 15 ассетов из `Assets/Data/Products/`. Порядок не важен.

**`machineTiers`** — `+` 3 раза, перетащи `Machine_T1`, `Machine_T2`, `Machine_T3` в порядке возрастания.

**`recipes`** — `+` 8 раз, перетащи все 8 рецептов.

**`wheelSectors`** — `+` 9 раз, перетащи все 9 секторов.

**`starterRecipe`**: `Recipe_Espresso`
**`starterMachineTier`**: `Machine_T1`
**`starterBalance`**: `0`
**`starterBeansStock`**: `10`

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/GameContent.asset Assets/Data/GameContent.asset.meta && git commit -m "feat(data): GameContent asset linking all data"
```

---

## Task 14: Edit Mode тест на целостность `GameContent`

**Files:**
- Create: `Assets/Tests/EditMode/GameContentIntegrityTests.cs`

Этот тест проверяет, что GameContent заполнен корректно — все ссылки на месте, шансы колеса = 100%. Запускается в Edit Mode, без Play.

- [ ] **Step 1: Создать тестовый файл**

В `Assets/Tests/EditMode/` правый клик → `Create → C# Script` → имя `GameContentIntegrityTests`. Замени содержимое:

```csharp
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
```

- [ ] **Step 2: Запустить тесты**

`Window → General → Test Runner` → EditMode → `Run All`.

Expected: все тесты зелёные, включая `SmokeTest.OnePlusOne_Equals_Two`.

Если какой-то красный — Test Runner покажет, где. Скопируй ошибку и пиши, разрулим.

Типичная ошибка: "У рецепта 'cappuccino' в fixedIngredients продукт = null" → значит в Recipe_Cappuccino забыл прикрепить `Product_Beans` в список.

- [ ] **Step 3: Commit (только когда все тесты зелёные)**

```bash
git add Assets/Tests/EditMode/GameContentIntegrityTests.cs Assets/Tests/EditMode/GameContentIntegrityTests.cs.meta && git commit -m "test: GameContent integrity tests (refs, probabilities, starter)"
```

---

## Task 15: Финальная сверка фазы

- [ ] **Step 1: Структура ассетов**

```bash
find /Users/anashkin/DrinkitGame/Assets/Data -name "*.asset" | sort
```

Expected: 36 ассетов:
- 1 GameContent (в `Assets/Data/`)
- 15 в `Products/`
- 3 в `Machines/`
- 8 в `Recipes/`
- 9 в `WheelSectors/`

```bash
find /Users/anashkin/DrinkitGame/Assets/Data -name "*.asset" | wc -l
```

Expected: `36`

- [ ] **Step 2: Все тесты зелёные**

В Test Runner → Run All → все галочки зелёные (как минимум `SmokeTest` + 10 тестов `GameContentIntegrityTests`).

- [ ] **Step 3: Console чистая**

`Window → General → Console` → ошибок (красных) и предупреждений (жёлтых) быть не должно. Если есть warnings — скопируй текст, обсудим.

- [ ] **Step 4: Финальный git log**

```bash
git log --oneline | head -20
```

Expected: коммиты Phase 2 на верху (примерно 11 штук от Task 1 до Task 14).

---

## Self-Review

После прохождения:
1. ✅ 5 типов SO (`ProductDefinition`, `MachineTierDefinition`, `RecipeDefinition`, `WheelSectorDefinition`, `GameContent`) описаны в `Assets/Scripts/Data/`
2. ✅ 5 enum'ов + 1 struct в том же месте
3. ✅ 36 ассетов созданы и заполнены
4. ✅ Все кросс-ссылки настроены: рецепты ссылаются на машины и продукты; машины ссылаются на рецепты для квестов; GameContent на всё
5. ✅ Edit Mode тест валидирует целостность, зелёный

**Готово → пиши `Phase 2 done`. Дальше Phase 3: Core Services (Inventory, Economy, Recipe, Machine, Reputation, Goal — с тестами для каждого).**

---

## Что НЕ делаем в этой фазе (anti-scope)

- ❌ Иконки спрайтов — поля `icon` оставляем `null` (плейсхолдеры подключим, когда нарисуешь арт)
- ❌ Никакой логики сервисов или UI — это Phase 3 и далее
- ❌ Никаких MonoBehaviour — только pure data
- ❌ Не описываем cooking flow в SO — мы делаем это процедурно в Phase 8 по `RecipeFamily`
- ❌ Не описываем OnboardingStep как SO — отложим до Phase 10
