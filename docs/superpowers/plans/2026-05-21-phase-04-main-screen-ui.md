# Phase 4 — Main Screen UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Собрать главный экран по Figma-лайауту с цветными квадратными плейсхолдерами. Топбар (рейтинг/баланс/goal) реактивно обновляется от сервисов. Кофемашина показывает текущий тир. Drinchik — большой плейсхолдер. Кнопка колеса и таб-бар Главная/Магазин видны, но Магазин ещё не работает (Phase 7).

**Architecture:**
- Один `Canvas` уже есть из Phase 1.
- Внутри — `MainScreenPanel` с `VerticalLayoutGroup` (стек сверху вниз: топбар → заказы → машина → Дринчик → таб-бар).
- Каждая логическая секция — отдельный child GameObject со своим контроллером (`TopBarController`, `OrderSlotsController`, `MachineDisplayController`).
- Контроллеры получают `GameStateManager` через `static Instance` (singleton pattern, простой и достаточный для прототипа).
- Все цвета и спрайты — плейсхолдеры (синие, серые квадраты). Финальные арты подключим позже.

**Tech Stack:** uGUI · TextMeshPro · `VerticalLayoutGroup` / `HorizontalLayoutGroup` для адаптивности

**Конец фазы:** Жмёшь Play → видишь главный экран с топбаром, тремя пустыми слотами заказов, секцией кофемашины ("Кофемашина T1"), большим синим прямоугольником-Дринчиком, кнопкой колеса и таб-баром. Цифры в топбаре совпадают со стартовым состоянием (Rating 5.0, Balance 0 ₽, Goal "Купи рецепт Американо 0/100 ₽").

---

## Task 1: Сделать `GameStateManager` синглтоном

**Files:**
- Modify: `Assets/Scripts/Core/GameStateManager.cs`

UI-контроллеры должны легко находить менеджер. Прокинем `static Instance`.

- [ ] **Step 1: Добавить `Instance`**

Открой `Assets/Scripts/Core/GameStateManager.cs`. Замени блок `public class GameStateManager : MonoBehaviour { ... // Открытые ссылки` (начало класса) — добавь `Instance` сразу после `public GameContent content;`:

Найди эту строку:
```csharp
        // Открытые ссылки на сервисы (UI будет их подписывать в Phase 4).
        public GameState State { get; private set; }
```

И **перед ней** добавь:

```csharp
        public static GameStateManager Instance { get; private set; }

```

В методе `Awake`, в самом начале (до проверки `content == null`) добавь:

```csharp
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

```

В конце `Awake` (после `Debug.Log`'ов), и в новом методе `OnDestroy` добавь сброс:

```csharp
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
```

- [ ] **Step 2: Дождаться компиляции (Console чистая)**

- [ ] **Step 3: Запусти Play — Console показывает тот же лог что раньше, без ошибок**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs && git commit -m "feat(core): GameStateManager singleton via static Instance"
```

---

## Task 2: Настроить корневой `MainScreenPanel` под вертикальный лайаут

**Files:**
- Modify: `Assets/Scenes/Main.unity` (через Unity Editor)

Текущий MainScreenPanel — просто белая Panel на всю Canvas. Превратим в вертикальный контейнер.

- [ ] **Step 1: Открыть MainScreenPanel**

В Hierarchy выбери `Canvas → MainScreenPanel`.

- [ ] **Step 2: Настроить RectTransform на полный экран**

В Inspector → `Rect Transform`:
- Якоря: жми на иконку якоря (квадрат с крестом), Alt+клик по правой нижней опции `stretch / stretch` (растягивает на весь родитель)
- Left/Right/Top/Bottom = `0`

- [ ] **Step 3: Убрать дефолтный Image / сделать прозрачным**

В компоненте `Image` (он создан вместе с Panel):
- Color: измени Alpha (A) на `255` для светлого фона. Hex: `E3EEFF` (светло-голубой как в Figma)
- Source Image: можно оставить дефолтный или поставить None

- [ ] **Step 4: Добавить `VerticalLayoutGroup`**

`Add Component` → `Vertical Layout Group`. Настрой:
- Padding: Left=`12`, Right=`12`, Top=`50` (для status bar iOS), Bottom=`12`
- Spacing: `12`
- Child Alignment: `Upper Center`
- Control Child Size: ✓ Width, ✓ Height (обе галочки)
- Child Force Expand: ✓ Width, **❌** Height

- [ ] **Step 5: Сохранить сцену (Cmd+S) и Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(ui): MainScreenPanel as vertical layout root with E3EEFF background"
```

---

## Task 3: Топбар — 3 пилюли (плейсхолдеры)

**Files:**
- Modify: `Assets/Scenes/Main.unity`

В Figma это три горизонтально расположенные синие "пилюли" с текстом ("Рейтинг 4.8", "0 ₽", "Купи Американо ...").

- [ ] **Step 1: Создать `TopBar` контейнер**

В Hierarchy кликни правой кнопкой по `MainScreenPanel` → `Create Empty`. Переименуй в `TopBar`.

В Inspector:
- `Rect Transform` → Height: `32` (через `Layout Element`, см. ниже)
- `Add Component` → `Horizontal Layout Group`:
  - Padding: все 0
  - Spacing: `8`
  - Child Alignment: `Middle Center`
  - Control Child Size: ✓ Width, ✓ Height
  - Child Force Expand: ✓ Width, ❌ Height
- `Add Component` → `Layout Element`:
  - Preferred Height: `32`
  - Min Height: `32`

- [ ] **Step 2: Создать пилюлю "Рейтинг"**

Правый клик по `TopBar` → `UI → Image`. Переименуй в `Pill_Rating`.

В Inspector:
- `Image` → Color: HEX `5A8DDC` (синий)
- `Image` → Source Image: дефолтный (UISprite, чтобы были закруглённые углы)
- `Image Type`: `Sliced`
- `Layout Element`: Preferred Height = `32`, Min Width = `100`

Правый клик по `Pill_Rating` → `UI → Text - TextMeshPro`. **Если Unity предложит импортировать TMP Essentials — нажми "Import TMP Essentials"**. Появится поле `Text (TMP)` внутри пилюли.

Переименуй в `Label`. В Inspector:
- Rect Transform → растяни на весь родитель (Alt+стрелочка в иконке якорей → `stretch / stretch`, Left/Right/Top/Bottom = 0)
- TMP_Text:
  - Text: `Рейтинг 5.0`
  - Font Size: `14`
  - Color: белый
  - Alignment: Center (горизонтально) + Middle (вертикально)
  - Wrapping: ❌ Disabled

- [ ] **Step 3: Дублировать пилюлю для "Баланс"**

В Hierarchy выбери `Pill_Rating` → Cmd+D дублирует. Переименуй копию в `Pill_Balance`.
Внутри найди `Label`, измени:
- Text: `0 ₽`

- [ ] **Step 4: Дублировать пилюлю для "Goal"**

`Pill_Rating` → Cmd+D → переименуй в `Pill_Goal`.
Внутри `Label`:
- Text: `Купи рецепт «Американо» — 0 / 100 ₽`
- Font Size: `12` (текст длиннее, уменьшаем)

В Inspector у `Pill_Goal` → `Layout Element` → Flexible Width: `2` (займёт больше места чем другие)

- [ ] **Step 5: Проверить в Game view**

Сверху должна быть полоса из трёх синих пилюль. Game view → выбери разрешение `iPhone 16 (393x852)` или `iPhone X (375x812)`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(ui): top bar with 3 placeholder pills (rating, balance, goal)"
```

---

## Task 4: `TopBarController` — реактивная привязка к сервисам

**Files:**
- Create: `Assets/Scripts/UI/TopBarController.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `TopBarController.cs`**

В `Assets/Scripts/UI/`:

```csharp
using DrinkitGame.Core;
using TMPro;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Отображает рейтинг, баланс и текущий goal — обновляется на событиях сервисов.
    public class TopBarController : MonoBehaviour
    {
        [Header("Labels (TMP) inside pills")]
        public TMP_Text ratingLabel;
        public TMP_Text balanceLabel;
        public TMP_Text goalLabel;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null)
            {
                Debug.LogError("[TopBar] GameStateManager.Instance == null. Убедись что GameStateManager на GameRoot и сцена корректна.");
                return;
            }

            // Подписки
            _gsm.Economy.BalanceChanged += OnBalanceChanged;
            _gsm.Reputation.ReputationChanged += OnReputationChanged;

            // Goal не имеет своего события — пересчитываем когда что-то меняется
            _gsm.Economy.BalanceChanged += _ => RefreshGoal();
            _gsm.Quests.CountChanged += (_, __) => RefreshGoal();
            _gsm.Recipes.RecipeUnlocked += _ => RefreshGoal();
            _gsm.Machine.Upgraded += _ => RefreshGoal();

            // Первый рендер
            OnBalanceChanged(_gsm.Economy.Balance);
            OnReputationChanged(_gsm.Reputation.Reputation);
            RefreshGoal();
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged -= OnBalanceChanged;
            _gsm.Reputation.ReputationChanged -= OnReputationChanged;
            // Лямбды не отписываются по делегату — для прототипа допустимо: GSM умирает вместе со сценой.
        }

        private void OnBalanceChanged(int newBalance)
        {
            if (balanceLabel != null) balanceLabel.text = $"{newBalance} ₽";
        }

        private void OnReputationChanged(float newRep)
        {
            if (ratingLabel != null) ratingLabel.text = $"Рейтинг {newRep:F1}";
        }

        private void RefreshGoal()
        {
            if (goalLabel == null || _gsm == null) return;
            var goal = _gsm.GoalTracker.CurrentGoal();
            goalLabel.text = string.IsNullOrEmpty(goal.ProgressLabel)
                ? goal.Description
                : $"{goal.Description} — {goal.ProgressLabel}";
        }
    }
}
```

- [ ] **Step 2: Дождаться компиляции**

- [ ] **Step 3: Прицепить компонент**

В Hierarchy выбери `TopBar` → `Add Component` → `Top Bar Controller`.

В Inspector у компонента:
- Rating Label: перетащи `TopBar/Pill_Rating/Label`
- Balance Label: перетащи `TopBar/Pill_Balance/Label`
- Goal Label: перетащи `TopBar/Pill_Goal/Label`

- [ ] **Step 4: Сохранить и запустить Play**

Cmd+S, потом Play. Должно быть видно:
- `Рейтинг 5.0`
- `0 ₽`
- `Купи рецепт «Американо» — 0 / 100 ₽`

Если показывает дефолтные тексты — проверь, что все Label-поля подключены в инспекторе. Если ошибки — копируй в чат.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity && git commit -m "feat(ui): TopBarController binds pills to economy/reputation/goal"
```

---

## Task 5: Секция "Заказы" с тремя пустыми слотами

**Files:**
- Modify: `Assets/Scenes/Main.unity`

Тут пока статические плейсхолдеры. Спавн заказов и наполнение — Phase 5.

- [ ] **Step 1: Создать `OrdersSection`**

Правый клик по `MainScreenPanel` → `Create Empty`. Переименуй в `OrdersSection`.

Add Component → `Vertical Layout Group`:
- Padding: 0
- Spacing: `8`
- Child Alignment: `Upper Left`
- Control Child Size: ✓ Width, ✓ Height
- Child Force Expand: ✓ Width, ❌ Height

Add Component → `Layout Element`:
- Preferred Height: `156` (заголовок + слоты)

- [ ] **Step 2: Заголовок "Заказы"**

Правый клик по `OrdersSection` → `UI → Text - TextMeshPro`. Переименуй в `Title`.
- Text: `Заказы`
- Font Size: `20`
- Color: чёрный
- Alignment: Left + Middle
- Layout Element: Preferred Height = `28`

- [ ] **Step 3: Контейнер слотов**

Правый клик по `OrdersSection` → `Create Empty`. Переименуй в `SlotsRow`.
Add Component → `Horizontal Layout Group`:
- Spacing: `7`
- Child Alignment: `Middle Center`
- Control Child Size: ✓ Width, ✓ Height
- Child Force Expand: ✓ Width, ❌ Height
- Padding: 0

Add Component → `Layout Element`:
- Preferred Height: `120`
- Min Height: `120`

- [ ] **Step 4: Создать 3 слота-плейсхолдера**

Правый клик по `SlotsRow` → `UI → Image`. Переименуй в `Slot_1`.
- Image → Color: HEX `EEEEEE` (светло-серый)
- Image → Source Image: дефолтный (закругл. рамки)
- Image Type: Sliced
- Layout Element: Preferred Width = `110`, Preferred Height = `120`

Внутрь `Slot_1` добавь `UI → Text - TextMeshPro`. Переименуй в `Status`:
- Text: `Пусто`
- Font Size: `12`
- Color: HEX `666666`
- Alignment: Center + Middle
- RectTransform: stretch на весь родитель, Left/Right/Top/Bottom = 0

Дублируй `Slot_1` ещё 2 раза (Cmd+D). Переименуй копии в `Slot_2`, `Slot_3`.

- [ ] **Step 5: Save и Play — видны 3 серых прямоугольника со словом "Пусто"**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(ui): orders section header + 3 empty slot placeholders"
```

---

## Task 6: Секция кофемашины с привязкой к `MachineService`

**Files:**
- Create: `Assets/Scripts/UI/MachineDisplayController.cs`
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать `MachineSection`**

Правый клик по `MainScreenPanel` → `Create Empty`. Переименуй в `MachineSection`.
- Vertical Layout Group: Padding 0, Spacing 4, Control Child Size W+H, Force Expand W
- Layout Element: Preferred Height = `220`

- [ ] **Step 2: Заголовок "Кофемашина T1"**

Внутри добавь `UI → Text - TextMeshPro`. Переименуй в `TierLabel`.
- Text: `Кофемашина T1`
- Font Size: `18`
- Color: чёрный
- Alignment: Left + Middle
- Layout Element: Preferred Height = `28`

- [ ] **Step 3: Плейсхолдер картинки машины**

Правый клик по `MachineSection` → `UI → Image`. Переименуй в `MachineImage`.
- Color: HEX `B5C7E5` (бледно-синий)
- Layout Element: Preferred Height = `180`, Preferred Width = `180`

Это будет место под спрайт. Пока — просто закрашенный прямоугольник.

- [ ] **Step 4: Создать `MachineDisplayController.cs`**

В `Assets/Scripts/UI/`:

```csharp
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Отображает текущий тир кофемашины (текст + спрайт) и реагирует на прокачку.
    public class MachineDisplayController : MonoBehaviour
    {
        [Tooltip("Текстовая подпись 'Кофемашина T1'")]
        public TMP_Text tierLabel;

        [Tooltip("Картинка машины. Source Image возьмётся из MachineTierDefinition.icon если задан, иначе оставляем плейсхолдер-цвет.")]
        public Image machineImage;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Machine.Upgraded += OnUpgraded;
            Refresh(_gsm.Machine.CurrentTier);
        }

        private void OnDestroy()
        {
            if (_gsm != null) _gsm.Machine.Upgraded -= OnUpgraded;
        }

        private void OnUpgraded(MachineTierDefinition newTier) => Refresh(newTier);

        private void Refresh(MachineTierDefinition tier)
        {
            if (tier == null) return;
            if (tierLabel != null)
                tierLabel.text = $"Кофемашина T{tier.tierIndex}" +
                                 (string.IsNullOrEmpty(tier.displayName) ? "" : $" — {tier.displayName}");
            if (machineImage != null && tier.icon != null)
                machineImage.sprite = tier.icon;
        }
    }
}
```

- [ ] **Step 5: Прицепить компонент**

В Hierarchy → `MachineSection` → Add Component → `Machine Display Controller`. В инспекторе:
- Tier Label: перетащи `MachineSection/TierLabel`
- Machine Image: перетащи `MachineSection/MachineImage`

- [ ] **Step 6: Save и Play — видно "Кофемашина T1 — Старенькая" + бледно-синий прямоугольник**

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity && git commit -m "feat(ui): MachineDisplayController shows current tier reactively"
```

---

## Task 7: Дринчик-плейсхолдер

**Files:**
- Modify: `Assets/Scenes/Main.unity`

Просто большой синий прямоугольник в центре экрана. В Phase 10 заменим на анимированного маскота.

- [ ] **Step 1: Создать `DrinchikSection`**

Правый клик по `MainScreenPanel` → `Create Empty` → переименуй в `DrinchikSection`.

Add Component → `Layout Element`:
- Preferred Height: `240`
- Flexible Height: `1` (займёт оставшееся место)

- [ ] **Step 2: Создать плейсхолдер**

Внутри `DrinchikSection` → `UI → Image`. Переименуй в `DrinchikPlaceholder`.
- Color: HEX `4FA7D9` (голубой — цвет китёнка)
- Source Image: дефолтный (закруглённый)
- Image Type: Sliced
- RectTransform: stretch на родитель, Left/Right/Top/Bottom = `20` (отступы со всех сторон)

- [ ] **Step 3: Подпись (опционально, для понятности)**

Внутри `DrinchikPlaceholder` → `UI → Text - TextMeshPro`. Переименуй в `Label`.
- Text: `🐳 Дринчик`
- Font Size: `32`
- Color: белый
- Alignment: Center + Middle
- RectTransform: stretch на родитель

- [ ] **Step 4: Save и Play — видишь большой голубой прямоугольник с подписью**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(ui): Drinchik placeholder section (color-blocked, art TBD)"
```

---

## Task 8: Кнопка колеса удачи (плейсхолдер)

**Files:**
- Modify: `Assets/Scenes/Main.unity`

В Figma колесо торчит справа от Дринчика. Пока — просто кнопка в углу. Логику колеса напишем в Phase 9.

- [ ] **Step 1: Создать кнопку колеса (прямой ребёнок Canvas)**

Кнопка должна "плавать" над всем контентом, поэтому делаем её **прямым ребёнком `Canvas`**, не внутри `MainScreenPanel` (чтобы её не двигал VerticalLayoutGroup).

В Hierarchy выбери `Canvas` → правый клик → `UI → Button - TextMeshPro`. **Если предложит импорт TMP — Import**. Переименуй в `WheelButton`.

Чтобы кнопка отрисовалась **поверх** `MainScreenPanel`, перетащи `WheelButton` в Hierarchy так, чтобы она оказалась **ниже** `MainScreenPanel` в списке детей Canvas (Unity рисует UI в порядке списка, нижние — поверх). Должно быть так:
```
Canvas
  MainScreenPanel
  WheelButton    ← здесь, ниже MainScreenPanel
```

В Inspector у `WheelButton`:
- RectTransform:
  - Anchors: жми иконку якорей, Alt+клик на `bottom right` — это привяжет к правому нижнему углу Canvas
  - Anchored Position X = `-20`, Y = `100` (X — отступ от правого края, Y — от низа, выше таб-бара)
  - Width = `100`, Height = `60`
- Image → Color: HEX `5A8DDC` (тот же синий что и пилюли)

Внутри `WheelButton` → `Text (TMP)`:
- Text: `Колесо\nудачи` (двухстрочное)
- Font Size: `14`
- Color: белый
- Alignment: Center + Middle

- [ ] **Step 2: На время — кнопка ничего не делает**

Это нормально, поведение — Phase 9. Можно для отладки прицепить временный лог:

В Inspector у `WheelButton` → `Button` → `On Click ()` → жми `+` → перетащи сам же `WheelButton` в слот объекта → выбери `Debug.Log` ... нет, проще сделать через скрипт.

Создай `Assets/Scripts/UI/WheelButtonPlaceholderController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Временный плейсхолдер: пока колесо не реализовано, кнопка просто пишет в Console.
    [RequireComponent(typeof(Button))]
    public class WheelButtonPlaceholderController : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
                Debug.Log("[WheelButton] Колесо ещё не реализовано (Phase 9)."));
        }
    }
}
```

В Hierarchy выбери `WheelButton` → Add Component → `Wheel Button Placeholder Controller`.

- [ ] **Step 3: Save и Play — справа внизу синяя кнопка с надписью**

Жми на неё — в Console: `[WheelButton] Колесо ещё не реализовано (Phase 9).`

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity && git commit -m "feat(ui): wheel button placeholder (logs click; logic deferred to Phase 9)"
```

---

## Task 9: Нижний таб-бар (Главная / Магазин)

**Files:**
- Modify: `Assets/Scenes/Main.unity`

По Figma — закруглённый блок снизу с двумя кнопками. Магазин ничего не делает (Phase 7), Главная подсвечена.

- [ ] **Step 1: Создать `TabBar`**

Правый клик по `MainScreenPanel` → `UI → Image`. Переименуй в `TabBar`.

В Inspector:
- Image → Color: HEX `E3EEFF` (тот же что фон — сольётся, но с тенью наверх)
- Source Image: дефолтный (закруглённый)
- Image Type: Sliced
- Layout Element: Preferred Height = `60`, Min Height = `60`

Add Component → `Horizontal Layout Group`:
- Padding: Left=`12`, Right=`12`, Top=`12`, Bottom=`12`
- Spacing: `8`
- Child Alignment: Middle Center
- Control Child Size: ✓ W, ✓ H
- Child Force Expand: ✓ Width, ❌ Height

- [ ] **Step 2: Кнопка "Главная" (активная)**

Правый клик по `TabBar` → `UI → Button - TextMeshPro`. Переименуй в `Tab_Home`.
- Button → Image → Color: HEX `5A8DDC` (синий, активный)
- Text внутри: `Главная`, белый, Font Size = `14`, Bold

Layout Element на Tab_Home: Flexible Width = `1`

- [ ] **Step 3: Кнопка "Магазин" (пока неактивная)**

Дублируй `Tab_Home` → переименуй в `Tab_Store`.
- Button → Image → Color: прозрачный (Alpha = 0) — на фоне таб-бара
- Text: `Магазин`, чёрный

- [ ] **Step 4: Прицепить временный лог на оба таба**

Создай `Assets/Scripts/UI/TabBarPlaceholderController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Временные обработчики табов — лог в Console.
    /// "Магазин" будет реализован в Phase 7 (Store Screen).
    public class TabBarPlaceholderController : MonoBehaviour
    {
        public Button homeTab;
        public Button storeTab;

        private void Start()
        {
            if (homeTab != null)
                homeTab.onClick.AddListener(() =>
                    Debug.Log("[TabBar] Главная — мы уже тут."));
            if (storeTab != null)
                storeTab.onClick.AddListener(() =>
                    Debug.Log("[TabBar] Магазин ещё не реализован (Phase 7)."));
        }
    }
}
```

В Hierarchy → `TabBar` → Add Component → `Tab Bar Placeholder Controller`. В инспекторе перетащи:
- Home Tab: `Tab_Home`
- Store Tab: `Tab_Store`

- [ ] **Step 5: Save и Play — нижний таб-бар с двумя кнопками; клик по Магазин логирует "не реализован".**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/UI Assets/Scenes/Main.unity && git commit -m "feat(ui): bottom tab bar (Home active, Store placeholder)"
```

---

## Task 10: Финальная сверка Phase 4

- [ ] **Step 1: Запусти Play и сверь экран**

В Game view (разрешение iPhone X / 375×812 или iPhone 16):

Сверху вниз должно быть:
1. **Топбар** — 3 синие пилюли: `Рейтинг 5.0`, `0 ₽`, `Купи рецепт «Американо» — 0 / 100 ₽`
2. **"Заказы"** — серый заголовок + 3 пустых серых слота
3. **"Кофемашина T1 — Старенькая"** — заголовок + бледно-синий квадрат-плейсхолдер
4. **Дринчик** — большой голубой прямоугольник с надписью "🐳 Дринчик", справа внизу — кнопка "Колесо удачи"
5. **Таб-бар** — снизу две кнопки: "Главная" (синяя, активная) и "Магазин"

- [ ] **Step 2: Все Edit Mode тесты зелёные**

Test Runner → Run All → 57 тестов зелёные (фазы 1-3) + ничего не сломано.

- [ ] **Step 3: Console при Play чистая**

Только наши Debug.Log от GameStateManager и GoalTracker. Никаких NullReferenceException.

- [ ] **Step 4: Проверь что значения корректны**

В Inspector компонента `GameStateManager` (Play mode) глянь:
- Economy.Balance = 0
- Reputation.Reputation = 5
- Machine.CurrentTierIndex = 1
- Inventory.GetStock("beans") = 10 (через консольный лог в Start)

- [ ] **Step 5: Финальный git log**

```bash
git log --oneline | head -15
```

Должно быть ~10 коммитов Phase 4.

---

## Self-Review

После прохождения:
1. ✅ Главный экран собран, 5 секций сверху вниз
2. ✅ Топбар реактивно реагирует на сервисы (если что-то изменится — текст обновится)
3. ✅ Кофемашина показывает текущий тир (T1 Старенькая)
4. ✅ Дринчик — плейсхолдер (синий блок)
5. ✅ Кнопка колеса и таб-бар работают как заглушки с логами
6. ✅ Game view в портрете 375×812 выглядит читаемо

**Готово → пиши `Phase 4 done`. Дальше Phase 5: спавн заказов в слоты + таймеры терпения + переход в (пустой пока) Cooking-экран.**

---

## Что НЕ делаем в этой фазе (anti-scope)

- ❌ Реальные спрайты — все цветные квадраты-плейсхолдеры
- ❌ Спавн заказов — Phase 5
- ❌ Логика магазина — Phase 7
- ❌ Логика колеса — Phase 9
- ❌ Анимация маскота — Phase 10
- ❌ Переключение экранов (Cooking, Wheel, Store) — будем добавлять router когда понадобится первый дополнительный экран (Phase 5/6)
- ❌ Адаптивность под планшеты/desktop — целимся в мобильный портрет
