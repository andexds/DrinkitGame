# Phase 13: Tactile Cooking Screen

**Status:** не выполнено
**Дата:** 2026-06-01
**Базируется на:** Phase 8a (CookingFlow), Phase 8b (4 мини-игры)
**Заменяет UI:** убирает единый «Дальше», ставит тапы по объектам кухни

---

## Goal

Сделать экран готовки **тактильным**. Игрок видит барную стойку с предметами (кофемашина, кофемолка, молокозбивалка, питчер, стаканы, фильтр, баночки сиропов) и ведёт заказ, **тапая на нужный предмет**, а не на кнопку «Дальше». Мини-игры (M1–M4) остаются — запускаются от тапа на соответствующий объект. Шаги без мини-игр (Extract, AddSyrup и т.п.) — короткая анимация по тапу. В конце — большая кнопка «Выдать» внизу.

**Что НЕ меняем:**
- `CookingFlow.GenerateSteps()` — генерирует тот же список шагов.
- `MiniGameDispatcher` и 4 мини-игры (M1–M4) — без изменений.
- `OrderResolutionService.Complete()` — без изменений.

**Что меняем:**
- Сцена `CookingScreenPanel` — полная пересборка визуала.
- `CookingScreenController` — рулит активацией предметов вместо одной кнопки.
- Новый компонент `KitchenObject` — небольшой враппер над Button + хайлайт + декларация «какие шаги я обрабатываю».

---

## Карта шагов → объектов

| CookingStepType | Объект на сцене | Что делает по тапу |
|---|---|---|
| TakeCup | `CupHere` / `CupTakeaway` (видны оба, нужный подсвечен) | Кружка летит к машине → advance |
| GrindCoffee | `Grinder` | Запускает M1 → advance после Completed |
| Extract | `CoffeeMachine` | Анимация налива 1.5 сек → advance |
| AddHotWater | `CoffeeMachine` (та же машина — Long press OR второй тап) | Анимация налива 1 сек → advance |
| TakeMilk / TakeCream | (auto) | Авто-скип, 0.3 сек задержка |
| SteamMilk / SteamCream | `MilkFrother` | Запускает M2 → advance |
| PourMilk / PourCream | (auto) | Анимация налива из питчера 1 сек → advance |
| AddCacao | `CacaoJar` | 0.6 сек насыпать → advance |
| AddMatcha | `MatchaJar` | 0.6 сек насыпать → advance |
| Whisk | `WhiskTool` | Запускает M4 → advance |
| SetupFilter | `FilterRack` | 0.6 сек поставить → advance |
| PourOver | `PourOverKettle` | Запускает M3 → advance |
| AddSyrup | `SyrupBottle` | 0.6 сек налить → advance |
| AddTopping | `ToppingJar` | 0.6 сек посыпать → advance |
| Deliver | `ServeButton` (большая кнопка снизу) | Тап → CompleteOrder |

**Авто-шаги** (`TakeMilk`, `TakeCream`, `PourMilk`, `PourCream`) — без тапа: пауза 0.3 сек, опционально воспроизводится короткая анимация (питчер дрогнул, появилась полоска налива), advance.

**Подсветка:** активный объект — пульсирующий outline (`Outline` компонент или дочерний `Image` с тёплой обводкой). Неактивные — затемнены до alpha=0.4 чтобы было понятно что кликать нельзя.

---

## Files

**Modify:**
- `Assets/Scenes/Main.unity` — пересборка `CookingScreenPanel`
- `Assets/Scripts/UI/CookingScreenController.cs` — новый роутинг

**Create:**
- `Assets/Scripts/Cooking/KitchenObject.cs` — компонент-враппер
- `Assets/Art/Sprites/Kitchen/` — папка для PNG ассетов (машина, кофемолка, питчер, фильтр, банки)

---

## Task 1: KitchenObject компонент

**File:** `Assets/Scripts/Cooking/KitchenObject.cs`

Маленький враппер над Button, который декларирует «какие шаги я умею обрабатывать» + умеет включать/выключать подсветку. `CookingScreenController` в каждом тике шага ищет матчящие объекты и активирует их.

```csharp
using System;
using DrinkitGame.Cooking;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Тап-зона на сцене готовки (кофемолка, машина, фильтр, банка сиропа и т.п.).
    /// Декларирует, какой шаг CookingFlow она «закрывает» при тапе.
    /// CookingScreenController сам решает, активна она сейчас или нет.
    [RequireComponent(typeof(Button))]
    public class KitchenObject : MonoBehaviour
    {
        [Tooltip("Какие типы шагов закрывает этот объект. Обычно 1–2 (например, " +
                 "CoffeeMachine: Extract + AddHotWater).")]
        public CookingStepType[] handlesSteps;

        [Tooltip("GameObject подсветки/обводки — включается когда объект активен. " +
                 "Можно оставить null если подсветки нет.")]
        public GameObject highlight;

        [Tooltip("Опционально: затемняем CanvasGroup когда объект не активен. " +
                 "Если null — alpha не меняется, только highlight.")]
        public CanvasGroup canvasGroup;

        [Tooltip("Прозрачность когда объект НЕ активен (0..1). Обычно 0.4.")]
        [Range(0f, 1f)]
        public float inactiveAlpha = 0.4f;

        /// Срабатывает по тапу. Подписывается CookingScreenController.
        public event Action Tapped;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
            SetActive(false);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            // Тап считается только если объект сейчас «вооружён».
            if (_button.interactable) Tapped?.Invoke();
        }

        /// Включить/выключить объект как активную тап-зону для текущего шага.
        public void SetActive(bool active)
        {
            if (_button != null) _button.interactable = active;
            if (highlight != null) highlight.SetActive(active);
            if (canvasGroup != null)
                canvasGroup.alpha = active ? 1f : inactiveAlpha;
        }

        /// Подходит ли этот объект для указанного шага?
        public bool Handles(CookingStepType type)
        {
            if (handlesSteps == null) return false;
            for (int i = 0; i < handlesSteps.Length; i++)
                if (handlesSteps[i] == type) return true;
            return false;
        }
    }
}
```

**Что важно:** компонент требует `Button`. Подсветка — отдельный GameObject (обычно дочерний Image с обводкой), включается только когда `SetActive(true)`. `CanvasGroup` опционален — если хочешь чтобы неактивные были полупрозрачными.

---

## Task 2: Сцена — CookingScreenPanel layout

**В Hierarchy → `Canvas/CookingScreenPanel`** удали старое содержимое (HintLabel, AdvanceButton — Cancel и MiniGameOverlay оставь). Постройте новую структуру:

```
CookingScreenPanel (Image: фон стойки или просто Color)
 ├─ TopHUD                              ← инфо о заказе
 │   ├─ OrderSummary (TMP) "Капучино · на овсяном · с собой"
 │   ├─ HintLabel (TMP)    "Тапни кофемолку"
 │   ├─ ProgressLabel (TMP) "Шаг 3 из 7"
 │   ├─ PatienceLabel (TMP) "Терпение: 1:20"
 │   └─ CancelButton (Button) [×]
 ├─ KitchenStation                      ← вся стойка
 │   ├─ Background (Image: counter sprite)
 │   ├─ CoffeeMachine (Image + KitchenObject [Extract, AddHotWater])
 │   │   ├─ Highlight (Image — обводка, выкл)
 │   │   ├─ MachineSpout                ← точка откуда лить кофе
 │   │   └─ CupSlot                     ← точка куда летит стакан
 │   ├─ Grinder (Image + KitchenObject [GrindCoffee])
 │   │   └─ Highlight
 │   ├─ MilkFrother (Image + KitchenObject [SteamMilk, SteamCream])
 │   │   ├─ Highlight
 │   │   └─ Pitcher (Image)             ← питчер с молоком
 │   ├─ FilterRack (Image + KitchenObject [SetupFilter, PourOver])
 │   │   ├─ Highlight
 │   │   └─ (по умолчанию скрыт, появляется на фильтр-кофе)
 │   ├─ WhiskTool (Image + KitchenObject [Whisk])
 │   │   ├─ Highlight
 │   │   └─ (по умолчанию скрыт, появляется на матча)
 │   ├─ Shelf                           ← полка с банками/бутылками
 │   │   ├─ SyrupBottle (Image + KitchenObject [AddSyrup])
 │   │   ├─ ToppingJar  (Image + KitchenObject [AddTopping])
 │   │   ├─ CacaoJar    (Image + KitchenObject [AddCacao])
 │   │   └─ MatchaJar   (Image + KitchenObject [AddMatcha])
 │   └─ CupZone                         ← перед машиной, выбор стакана
 │       ├─ CupHere      (Image + KitchenObject [TakeCup])  ← isToGo=false
 │       └─ CupTakeaway  (Image + KitchenObject [TakeCup])  ← isToGo=true
 ├─ ServeButton (Button большой внизу)  ← Deliver step
 │   └─ Label (TMP) "Выдать"
 ├─ AnimLayer                            ← поверх всего, под MiniGame
 │   ├─ FlyingCup     (Image, обычно выкл) ← для анимации стакана к машине
 │   ├─ CoffeeStream  (Image, обычно выкл) ← наливка кофе
 │   └─ MilkStream    (Image, обычно выкл) ← наливка молока
 └─ MiniGameOverlay (как было)
```

### Step 1: Создать каркас

В `CookingScreenPanel`:
1. Удали старые `HintLabel` и `AdvanceButton`.
2. Создай `TopHUD` (UI → Panel, прозрачный фон), внутрь 5 TMP-текстов + `CancelButton` как раньше.
3. Создай `KitchenStation` (пустой GameObject с RectTransform stretch на весь экран).
4. Создай `ServeButton` (UI → Button) внизу — H=72, ширина на весь экран минус 32 padding.
5. Создай `AnimLayer` (пустой GameObject со stretch RectTransform).
6. `MiniGameOverlay` уже есть — убедись что он **последний ребёнок** (рисуется поверх всего).

### Step 2: Поставить ассеты (плейсхолдеры пока)

Пока спрайтов нет — используй цветные прямоугольники как плейсхолдеры:
- `CoffeeMachine`: тёмно-серый, 200×280, центр-низ кадра
- `Grinder`: коричневый, 100×140, слева от машины
- `MilkFrother`: серебристый, 120×200, справа от машины
- `Pitcher`: дочерний Image серого, 60×80, перед молокозбивалкой
- `FilterRack`: бежевый, 100×100, скрыт
- `WhiskTool`: жёлто-зелёный, 60×60, скрыт
- `Shelf` объекты (банки): 4 цветных квадрата 60×60 в ряд сверху-справа
- `CupHere` / `CupTakeaway`: 80×100 каждый, центр-низ, чуть впереди машины

На каждый из этих объектов:
- Добавь компонент **Button** (Transition = None, или Color Tint если хочешь подсветку нажатия)
- Добавь компонент **KitchenObject**, в поле **Handles Steps** заполни нужные значения
- Создай дочерний GameObject `Highlight` (Image — Source Image: жёлтая обводка или просто полупрозрачный жёлтый Image). По умолчанию выкл.
- Перетащи `Highlight` в поле **Highlight** на `KitchenObject`.
- Опционально: добавь `CanvasGroup` на сам объект, перетащи его в поле **Canvas Group** на `KitchenObject`.

### Step 3: ServeButton

Это не `KitchenObject`, а обычный Button — он всегда виден, но `interactable` управляется контроллером (вкл только на Deliver шаге). Цвет фона: яркий (например `5A8DDC`), текст белый, Bold, 20pt.

### Step 4: Save scene

---

## Task 3: CookingScreenController rewrite

**File:** `Assets/Scripts/UI/CookingScreenController.cs`

Полностью переписать. Старый код держим в голове (`_steps`, `_currentIndex`, `_qualitySum`, `_qualityCount` — логика та же), но теперь:
- Вместо одной кнопки Advance — массив `KitchenObject`.
- На каждом шаге активируем тот, что `Handles(currentStep.type)`.
- Авто-шаги (TakeMilk, TakeCream, PourMilk, PourCream) — пропускаем с задержкой 0.3 сек.
- Deliver — включаем `ServeButton`.

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DrinkitGame.Cooking;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Тактильный экран готовки: игрок тапает на объекты кухни (KitchenObject),
    /// каждый объект отвечает за свой тип шага CookingFlow.
    public class CookingScreenController : MonoBehaviour
    {
        [Header("HUD")]
        public TMP_Text orderSummaryLabel;
        public TMP_Text hintLabel;
        public TMP_Text progressLabel;
        public TMP_Text patienceLabel;
        public Button cancelButton;

        [Header("Kitchen objects (auto-activated по типу шага)")]
        [Tooltip("Перетащи все KitchenObject со сцены сюда. Контроллер сам ищет, " +
                 "кто Handles() текущий шаг.")]
        public List<KitchenObject> kitchenObjects = new();

        [Header("Serve (Deliver step)")]
        public Button serveButton;

        [Header("Mini-games")]
        public DrinkitGame.Cooking.MiniGameDispatcher miniGameDispatcher;

        [Header("Авто-шаги (TakeMilk, PourMilk и т.п.) — задержка перед auto-advance")]
        [Range(0.1f, 2f)]
        public float autoStepDelay = 0.3f;

        [Header("Тап-шаги без мини-игры (Extract, AddSyrup и т.п.) — задержка анимации")]
        [Range(0.2f, 3f)]
        public float tapActionDelay = 0.6f;

        private Order _order;
        private List<CookingStep> _steps;
        private int _currentIndex;
        private float _qualitySum;
        private int _qualityCount;
        private bool _stepInProgress; // защита от двойного тапа

        private void Awake()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            if (serveButton != null) serveButton.onClick.AddListener(OnServe);

            foreach (var ko in kitchenObjects)
                if (ko != null) ko.Tapped += () => OnObjectTapped(ko);
        }

        public void Bind(Order order)
        {
            _order = order;
            _steps = CookingFlow.GenerateSteps(order);
            _currentIndex = 0;
            _qualitySum = 0f;
            _qualityCount = 0;
            _stepInProgress = false;

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

            // 1. HUD
            if (hintLabel != null) hintLabel.text = step.hint;
            if (progressLabel != null) progressLabel.text = $"Шаг {_currentIndex + 1} из {_steps.Count}";

            // 2. ServeButton
            if (serveButton != null)
                serveButton.interactable = step.type == CookingStepType.Deliver;

            // 3. Активируем тот KitchenObject, который умеет этот шаг
            foreach (var ko in kitchenObjects)
                if (ko != null) ko.SetActive(ko.Handles(step.type));

            // 4. Авто-шаги (TakeMilk, TakeCream, PourMilk, PourCream) — без тапа
            if (IsAutoStep(step.type))
                StartCoroutine(AutoAdvanceAfter(autoStepDelay));
        }

        private static bool IsAutoStep(CookingStepType t)
        {
            return t == CookingStepType.TakeMilk
                || t == CookingStepType.TakeCream
                || t == CookingStepType.PourMilk
                || t == CookingStepType.PourCream;
        }

        private IEnumerator AutoAdvanceAfter(float seconds)
        {
            _stepInProgress = true;
            yield return new WaitForSeconds(seconds);
            _stepInProgress = false;
            AdvanceStep();
        }

        private void OnObjectTapped(KitchenObject ko)
        {
            if (_stepInProgress) return;
            if (_steps == null || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];
            if (!ko.Handles(step.type)) return; // защита, хотя SetActive уже отрубил неактивные

            // Особый случай: TakeCup — проверяем что выбран правильный стакан.
            // Чтобы различать CupHere и CupTakeaway, добавляем поле isToGoCup на каждый KitchenObject.
            // См. Task 4.
            if (step.type == CookingStepType.TakeCup)
            {
                bool tappedTakeaway = ko.name.ToLower().Contains("takeaway"); // временный костыль
                if (tappedTakeaway != _order.isToGo)
                {
                    // Неверный стакан — шейк/звук, не advance
                    return;
                }
            }

            if (step.isMiniGame && miniGameDispatcher != null)
            {
                var tier = GameStateManager.Instance.Machine.CurrentTier;
                bool started = miniGameDispatcher.TryBegin(step, tier);
                if (started)
                {
                    miniGameDispatcher.Completed += OnMiniGameDone;
                    return;
                }
                _qualitySum += 100f; _qualityCount += 1;
                AdvanceStep();
                return;
            }

            // Обычный тап-шаг без мини-игры — пауза анимации, потом advance.
            StartCoroutine(TapActionThenAdvance(tapActionDelay));
        }

        private IEnumerator TapActionThenAdvance(float seconds)
        {
            _stepInProgress = true;
            // Здесь будет вызов анимации (Task 5). Пока просто WaitForSeconds.
            yield return new WaitForSeconds(seconds);
            _stepInProgress = false;
            AdvanceStep();
        }

        private void OnMiniGameDone(float quality)
        {
            if (miniGameDispatcher != null) miniGameDispatcher.Completed -= OnMiniGameDone;
            _qualitySum += quality;
            _qualityCount += 1;
            AdvanceStep();
        }

        private void AdvanceStep()
        {
            _currentIndex++;
            if (_currentIndex >= _steps.Count) CompleteOrder();
            else ShowCurrentStep();
        }

        private void OnServe()
        {
            if (_steps == null || _currentIndex >= _steps.Count) return;
            var step = _steps[_currentIndex];
            if (step.type != CookingStepType.Deliver) return;
            AdvanceStep();
        }

        private void OnCancel()
        {
            if (_order == null) { UIRouter.Instance.ShowMain(); return; }
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

После замены файла → в инспекторе на `CookingScreenPanel` найди компонент `Cooking Screen Controller`, перетащи в **Kitchen Objects** все 10–14 объектов из `KitchenStation`, в **Serve Button** перетащи `ServeButton`, проверь что HUD-ссылки на месте.

---

## Task 4: Cup selection (тут vs с собой)

Чтобы контроллер понимал, какой стакан — какой, добавь в `KitchenObject` поле:

```csharp
[Tooltip("Только для TakeCup: true=стакан 'с собой', false='тут'. Игнорируется для других шагов.")]
public bool isToGoCup;
```

И в `CookingScreenController.OnObjectTapped` замени `string.Contains("takeaway")` на:
```csharp
if (step.type == CookingStepType.TakeCup && ko.isToGoCup != _order.isToGo)
    return; // неверный стакан
```

В сцене: `CupHere.isToGoCup = false`, `CupTakeaway.isToGoCup = true`.

**Polish (опционально):** при неверном тапе показывай быстрый шейк (RectTransform.anchoredPosition сдвигается на ±5px на 0.15 сек). Делается отдельной корутиной в `KitchenObject.Shake()`.

---

## Task 5: Визуальный фидбэк (cup overlays, налив молока, частицы)

3 независимых слоя фидбэка. Все сидят в `CookingScreenController` + один новый компонент `UIBurster`. Ни одна часть не блокирует следующую — можешь делать поэтапно.

### Task 5a: Кружка на кофемашине (fade-in при выборе)

**Идея:** при тапе по `CupHere` или `CupTakeaway` (тап-зона на полке) — соответствующая Image-кружка появляется на самой машине с плавным fade-in (alpha 0→1 за ~0.3 сек). Без анимации движения, просто прозрачность.

**Сцена:** как ты уже сделал, на `CoffeeMachine` лежат скрытые дочерние Image:
- `CupHere` — кружка «тут» (керамическая) на месте машины
- `CupTakeAway` — стакан «с собой» на месте машины

По дефолту оба `SetActive(false)`. Размер/позиция — ровно над «гнездом» машины, куда логически встаёт стакан.

**Контроллер:** новые поля
```csharp
public Image cupHereOnMachine;        // ссылка на CoffeeMachine/CupHere Image
public Sprite cupHereEmpty;            // sprite пустой кружки
public Sprite cupHereFull;             // sprite полной кружки (опционально)

public Image cupTakeawayOnMachine;
public Sprite cupTakeawayEmpty;
public Sprite cupTakeawayFull;         // опционально

public float cupFadeInDuration = 0.3f;
```

**Логика:** при тапе на правильный CupHere/CupTakeaway → корутина `TakeCupSequence`:
1. Выбирает нужный Image (по `_order.isToGo`)
2. `SetActive(true)`, ставит alpha=0
3. Лерпит alpha 0→1 за `cupFadeInDuration`
4. AdvanceStep

В `Bind()` — оба cup-overlay'я скрываются, sprites сбрасываются к Empty.

### Task 5b: Налив молока (PourMilk / PourCream)

**Идея:** PourMilk и PourCream — сейчас 0.3-сек авто-пропуски. Делаем из них 2.5-сек анимацию: показываем `PouringMilk` Image с лёгким пульсом scale (±1% sin-волна).

**Сцена:** на `CoffeeMachine` уже есть дочерний `PouringMilk` Image. Скрыт по дефолту.

**Контроллер:** новые поля
```csharp
public Image pouringMilkImage;
public float milkPourDuration = 2.5f;
public float pourPulseAmplitude = 0.01f;  // ±1%
```

**Логика:** в `ShowCurrentStep` для типов PourMilk/PourCream — НЕ запускать `AutoAdvanceAfter`, а корутину `PourMilkSequence`:
1. `pouringMilkImage.SetActive(true)`
2. Параллельно стартует `PulseScale` корутина (sin 8 Гц × pourPulseAmplitude)
3. WaitForSeconds(milkPourDuration)
4. `SetActive(false)`, AdvanceStep

Остальные авто-шаги (TakeMilk, TakeCream) идут через прежний `AutoAdvanceAfter(autoStepDelay)`.

### Task 5c: Частицы «DONE» (UIBurster)

**Идея:** на каждый «положил в стакан» шаг — короткий взрыв шариков/звёздочек из позиции кружки. Без `ParticleSystem` (не дружит со Screen Space Overlay Canvas). Простой клон Image-шаблона + полёт по кругу + затухание.

**Новый компонент:** `Assets/Scripts/UI/UIBurster.cs`

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Простой «успех»-фидбэк: Burst() клонирует Image-шаблон N раз,
    /// разбрасывает копии по кругу с затуханием.
    public class UIBurster : MonoBehaviour
    {
        public Image template;        // шаблон, GameObject выключен
        [Range(1, 32)] public int particleCount = 8;
        [Range(10f, 400f)] public float radius = 80f;
        [Range(0.2f, 2f)] public float duration = 0.7f;
        [Range(0f, 1f)] public float angleJitter = 0.3f;
        public Vector2 scaleStartEnd = new Vector2(1f, 0.5f);

        public void Burst()
        {
            if (template == null) return;
            var parent = template.transform.parent ?? transform;
            var origin = template.rectTransform != null
                ? template.rectTransform.anchoredPosition : Vector2.zero;
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (i / (float)particleCount) * Mathf.PI * 2f
                              + Random.Range(-angleJitter, angleJitter);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var go = Instantiate(template.gameObject, parent);
                go.SetActive(true);
                StartCoroutine(Animate(go, origin, dir));
            }
        }

        private IEnumerator Animate(GameObject p, Vector2 origin, Vector2 dir)
        {
            var rt = p.GetComponent<RectTransform>();
            var img = p.GetComponent<Image>();
            if (rt == null || img == null) { Destroy(p); yield break; }
            rt.anchoredPosition = origin;
            rt.localScale = Vector3.one * scaleStartEnd.x;
            Color c = img.color; c.a = 1f; img.color = c;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - k) * (1f - k);
                rt.anchoredPosition = origin + dir * radius * ease;
                rt.localScale = Vector3.one * Mathf.Lerp(scaleStartEnd.x, scaleStartEnd.y, k);
                c.a = 1f - k; img.color = c;
                yield return null;
            }
            Destroy(p);
        }
    }
}
```

**Сцена:** на CoffeeMachine (или рядом с кружками) создай GameObject `SuccessBurster`:
1. Внутрь положи Image-шаблон `Particle` (маленький белый шарик или звёздочка, sprite по вкусу), W=H=16, alpha 100%.
2. `Particle` **выключи** (SetActive false).
3. На `SuccessBurster` повесь `UIBurster`, перетащи `Particle` в поле **Template**.

**Контроллер:** новое поле `public UIBurster successBurster;` и вызов `successBurster.Burst()` после каждого «положил в стакан» шага.

### Какие шаги триггерят что

| Тип шага | Cup→full sprite | UIBurster.Burst() | PouringMilk overlay |
|---|---|---|---|
| TakeCup | — | — | — |
| GrindCoffee | — | — | — |
| Extract | ✓ (если first fill) | ✓ | — |
| AddHotWater | ✓ (если first fill) | ✓ | — |
| SteamMilk/Cream | — | — | — |
| PourMilk/Cream | ✓ (если first fill) | ✓ | ✓ (2.5 сек) |
| PourOver | ✓ (если first fill) | ✓ | — |
| AddSyrup | — | ✓ | — |
| AddTopping | — | ✓ | — |
| AddCacao | — | ✓ | — |
| AddMatcha | — | ✓ | — |
| Whisk | — | ✓ | — |
| SetupFilter | — | — | — |
| Deliver | — | — (отдельный результат-попап) | — |

«Cup→full» срабатывает только ОДИН РАЗ за заказ — на первый встретившийся fill-шаг. Контроллер держит флаг `_cupFilled`.

---

## Task 6: Фильтр и матча — скрытые объекты

`FilterRack` и `WhiskTool` (и можно `CacaoJar`/`MatchaJar`) **не нужны в большинстве заказов**. Чтобы они не «висели мертвыми», в начале готовки контроллер прячет ненужные.

В `Bind(Order order)` добавь блок:
```csharp
bool needsFilter  = order.recipe.family == RecipeFamily.Filter;
bool needsMatcha  = order.recipe.family == RecipeFamily.Matcha;
bool needsCacao   = order.recipe.family == RecipeFamily.Cacao;
bool needsSyrup   = order.syrup  != null;
bool needsTopping = order.topping != null;

SetObjectVisible(CookingStepType.SetupFilter, needsFilter);
SetObjectVisible(CookingStepType.Whisk,       needsMatcha);
SetObjectVisible(CookingStepType.AddMatcha,   needsMatcha);
SetObjectVisible(CookingStepType.AddCacao,    needsCacao);
SetObjectVisible(CookingStepType.AddSyrup,    needsSyrup);
SetObjectVisible(CookingStepType.AddTopping,  needsTopping);
```

```csharp
private void SetObjectVisible(CookingStepType type, bool visible)
{
    foreach (var ko in kitchenObjects)
        if (ko != null && ko.Handles(type))
            ko.gameObject.SetActive(visible);
}
```

(Если для шага несколько объектов — все включатся/выключатся.)

---

## Task 7: Onboarding + Phase 10

В Phase 10 уже есть шаг «Тапни по заказу и приготовь его!» (FirstOrderCompleted). Этого достаточно — никаких изменений в OnboardingController не требуется. Но если хочешь, добавь дополнительный шаг «Тапни кофемолку» как раз для нового экрана — это уже polish и в плане не обязательно.

---

## Task 8: Тестовый прогон

Прогони **по одному заказу каждого семейства**:

| Семейство | Ожидаемые тапы по объектам |
|---|---|
| Espresso | Cup → Grinder (M1) → Machine → Serve |
| Americano | Cup → Grinder (M1) → Machine → Machine (вода) → Serve |
| Cappuccino | Cup → Grinder (M1) → Machine → Frother (M2) → Serve |
| Latte | Cup → Grinder (M1) → Machine → Frother (M2) → Serve |
| Raf | Cup → Grinder (M1) → Machine → Frother (M2 cream) → Serve |
| Cacao | Cup → CacaoJar → Frother (M2) → Serve |
| Matcha | Cup → MatchaJar → Machine (вода) → Whisk (M4) → Frother (M2, если с молоком) → Serve |
| Filter | Cup → FilterRack → Grinder (M1) → PourOverKettle (M3) → Serve |
| +syrup | …→ SyrupBottle → Serve |
| +topping | …→ ToppingJar → Serve |

На каждом — проверь: HUD-подсказка совпадает с активной зоной, остальные объекты затемнены, на «неправильный» тап ничего не происходит.

---

## Common Pitfalls

1. **`KitchenObject` без Button или с Button в неактивном GameObject** → клики не регистрируются. Проверь иерархию.
2. **Подсветка `highlight` не привязана к нужному GameObject** → активация не видна визуально, но логика работает. Проверь поле `Highlight` на каждом KitchenObject.
3. **`isToGoCup` стоит одинаково на обоих стаканах** → любой стакан принимается. Проверь, что на `CupTakeaway` галка стоит, на `CupHere` — нет.
4. **`MiniGameOverlay` НЕ последний ребёнок в `CookingScreenPanel`** → мини-игра рисуется под другими элементами. Перенеси в самый низ списка детей.
5. **`autoStepDelay = 0`** → авто-шаги мгновенные, игрок не успевает увидеть подсказку. Минимум 0.2 сек.
6. **Двойной тап** → пока корутина `_stepInProgress = true` крутится, тапы игнорируются. Если кто-то добавит новый асинк-метод, не забудь выставить флаг.
7. **`ServeButton.interactable = true` забыли отключить на нон-Deliver шагах** → можно «выдать» сырой кофе. ShowCurrentStep должен выставлять interactable.
8. **Невидимые скрытые `KitchenObject`** оставлены в `kitchenObjects` списке — их `Handles()` тоже сработает, но `SetActive(false)` отключает их Button. ОК, ничего страшного.
9. **Cup overlay на машине не fade-in'ится** — alpha канал Image после первого заказа застрял на 0. В `Bind()` мы сбрасываем `c.a = 1` на оба cup-image'а, и при следующем тапе fade стартует с 0 заново. Если не сбрасывается — проверь, что ссылки `cupHereOnMachine` / `cupTakeawayOnMachine` указывают именно на тот Image, который ты ожидаешь.
10. **PouringMilk не показывается** — поле `pouringMilkImage` не назначено в инспекторе, контроллер тихо пропускает. Также: Image должен быть выключен по дефолту, иначе виден всегда. Контроллер сам включает/выключает через SetActive.
11. **UIBurster плюётся одной частицей и стопится** — частица-шаблон не выключена в Hierarchy, поэтому Instantiate делает копию активного объекта, который сразу попадает в `Update`, но клон уничтожается через `duration`. Visual эффект всё равно работает, но «оригинал» болтается. Выключи шаблон Image в Hierarchy.
12. **UIBurster частицы не видны** — Image-шаблон Color имеет alpha=0 (Unity default для List/inspector-color). Проверь, что у шаблона Color: 255,255,255,255.

---

## Self-Review

- [ ] Все 18 `CookingStepType` имеют хотя бы один обрабатывающий объект (кроме авто-шагов и Deliver/Whisk-исключений).
- [ ] `Deliver` шаг → `ServeButton.interactable = true`, на остальных = false.
- [ ] Авто-шаги (`TakeMilk/TakeCream/PourMilk/PourCream`) пропускаются за `autoStepDelay`.
- [ ] Стаканы: `CupHere` (isToGoCup=false), `CupTakeaway` (isToGoCup=true).
- [ ] Кофемашина обрабатывает Extract + AddHotWater (один объект, два типа в массиве).
- [ ] Молокозбивалка обрабатывает SteamMilk + SteamCream.
- [ ] Фильтр обрабатывает SetupFilter + PourOver.
- [ ] Скрытие лишних объектов в Bind() — FilterRack/WhiskTool/MatchaJar/CacaoJar/Syrup/Topping.
- [ ] MiniGameOverlay — самый нижний ребёнок CookingScreenPanel.
- [ ] CancelButton возвращает заказ через `ReinsertOrder()` (старая логика сохранена).
- [ ] **5a:** на главном экране → выбрать заказ → тапнуть правильный стакан → кружка появилась на машине с fade-in.
- [ ] **5a:** для эспрессо-«с собой» появляется именно `CupTakeAway`, для «тут» — `CupHere`.
- [ ] **5a:** после первого «налив»-шага (Extract / AddHotWater / PourMilk / PourCream / PourOver) — спрайт кружки переключился на `*Full` (если задан).
- [ ] **5b:** на капучино/латте/кацао — после M2 мини-игры видно `PouringMilk` 2.5 сек с лёгкой пульсацией.
- [ ] **5c:** после каждого «положил в стакан» шага — burst-частиц от кружки. На TakeCup, GrindCoffee, SteamMilk, SetupFilter — НЕТ частиц.
- [ ] **5c:** UIBurster.Particle template выключен в Hierarchy, Color alpha=255.

---

## Commit

Когда всё работает:
```bash
git add Assets/Scripts/Cooking/KitchenObject.cs \
        Assets/Scripts/Cooking/KitchenObject.cs.meta \
        Assets/Scripts/UI/CookingScreenController.cs \
        Assets/Scripts/UI/UIBurster.cs \
        Assets/Scripts/UI/UIBurster.cs.meta \
        Assets/Scenes/Main.unity
git commit -m "feat(cooking): tactile cooking screen — tap objects + visual feedback

Phase 13 — заменяем единый Advance-button на тапы по объектам кухни,
плюс 3 слоя визуального фидбэка:
- KitchenObject + handlesSteps[] + isToGoCup
- CookingScreenController с авто-шагами, мини-играми, ServeButton
- 5a: fade-in кружки на кофемашине после выбора + swap к Full после налива
- 5b: PouringMilk overlay 2.5 сек с лёгким пульсом на PourMilk/PourCream
- 5c: UIBurster — простые Image-частицы на 'положил в стакан' шагах"
```
