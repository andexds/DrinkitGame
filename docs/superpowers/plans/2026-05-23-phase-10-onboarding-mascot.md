# Phase 10 — Onboarding + Mascot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Оживить Дринчика — он реагирует эмоциями на события (доволен от хорошего качества, грустит от ушедшего клиента и т.д.). Сделать **онбординг на первом запуске**: вступительная реплика → подсветка слотов заказов → ждать первый заказ → тап → готовка → выдача → подсветка магазина и колеса → бесплатный спин → свобода.

**Architecture:**
- `MascotEmotion` enum — 8 эмоций.
- `MascotController` — MonoBehaviour, переключает спрайт (плейсхолдер — цвет/символ) + показывает речевой пузырь.
- Подписки `MascotController` на события сервисов (Recipe.RecipeUnlocked → happy; Orders.OrderAbandoned → sad; OrderResolution.OrderCompleted с Quality>80 → excited; и т.д.).
- `OnboardingStep` — POCO с описанием шага (текст реплики, тип triger'а, целевой UI).
- `OnboardingController` — MonoBehaviour, проходит линейную последовательность шагов. Триггер перехода = клик "Дальше" или ивент сервиса.
- `OnboardingOverlay` — UI: dim фон + speech bubble + опциональный пойнтер + кнопка "Дальше".
- `GameState.onboardingCompleted` уже существует.

**Tech Stack:** uGUI · TMPro · MonoBehaviour.

**Конец фазы:** Запускаешь игру в первый раз → Дринчик приветствует → ведёт по шагам → после спина колеса исчезает → нормальный геймплей. На втором запуске онбординг не повторяется (флаг). Дринчик во время игры время от времени меняет эмоции в углу.

---

## Task 1: `MascotEmotion` enum + `MascotController`

**Files:**
- Create: `Assets/Scripts/Mascot/MascotEmotion.cs`
- Create: `Assets/Scripts/Mascot/MascotController.cs`

- [ ] **Step 1: `MascotEmotion.cs`**

В `Assets/Scripts/Mascot/`:

```csharp
namespace DrinkitGame.Mascot
{
    public enum MascotEmotion
    {
        Idle,
        Happy,
        Excited,
        Welcoming,
        Sad,
        Disappointed,
        Pointing,
        Sleeping
    }
}
```

- [ ] **Step 2: `MascotController.cs`**

```csharp
using System.Collections;
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Mascot
{
    /// Управляет визуалом и эмоциями маскота Дринчика.
    /// Висит на DrinchikPlaceholder GameObject (или его контейнере).
    /// Спрайты — плейсхолдеры: меняем цвет + textLabel вместо смены спрайта.
    public class MascotController : MonoBehaviour
    {
        [Header("Visual placeholders (until real art)")]
        public Image bodyImage;
        public TMP_Text bodyLabel;    // плейсхолдер: текст эмоции внутри квадрата

        [Header("Speech bubble")]
        public GameObject speechBubbleRoot;
        public TMP_Text speechText;
        public float bubbleVisibleSeconds = 3f;

        [Header("Emotion colors (placeholders)")]
        public Color idleColor = new(0.31f, 0.65f, 0.85f);     // 4FA7D9 голубой
        public Color happyColor = new(0.18f, 0.72f, 0.51f);    // зелёный
        public Color excitedColor = new(0.95f, 0.61f, 0.07f);  // оранжевый
        public Color welcomingColor = new(0.49f, 0.36f, 0.85f); // фиолетовый
        public Color sadColor = new(0.30f, 0.39f, 0.55f);      // серо-синий
        public Color disappointedColor = new(0.85f, 0.27f, 0.27f); // красноватый
        public Color pointingColor = new(0.18f, 0.55f, 0.85f); // ярко-синий
        public Color sleepingColor = new(0.51f, 0.51f, 0.51f); // серый

        private GameStateManager _gsm;
        private Coroutine _hideBubbleCoroutine;

        public MascotEmotion CurrentEmotion { get; private set; } = MascotEmotion.Idle;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            HideBubble();
            SetEmotion(MascotEmotion.Idle);

            if (_gsm == null) return;

            // Подписки на события — Дринчик реагирует
            _gsm.OrderResolution.OrderCompleted += OnOrderCompleted;
            _gsm.Orders.OrderAbandoned += OnOrderAbandoned;
            _gsm.Recipes.RecipeUnlocked += OnRecipeUnlocked;
            _gsm.Machine.Upgraded += OnMachineUpgraded;
            _gsm.Wheel.Spun += OnWheelSpun;
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.OrderResolution.OrderCompleted -= OnOrderCompleted;
            _gsm.Orders.OrderAbandoned -= OnOrderAbandoned;
            _gsm.Recipes.RecipeUnlocked -= OnRecipeUnlocked;
            _gsm.Machine.Upgraded -= OnMachineUpgraded;
            _gsm.Wheel.Spun -= OnWheelSpun;
        }

        public void SetEmotion(MascotEmotion emotion)
        {
            CurrentEmotion = emotion;
            if (bodyImage != null) bodyImage.color = ColorForEmotion(emotion);
            if (bodyLabel != null) bodyLabel.text = LabelForEmotion(emotion);
        }

        public void Say(string text, MascotEmotion emotion = MascotEmotion.Idle)
        {
            SetEmotion(emotion);
            if (speechBubbleRoot == null || speechText == null) return;
            speechText.text = text;
            speechBubbleRoot.SetActive(true);

            if (_hideBubbleCoroutine != null) StopCoroutine(_hideBubbleCoroutine);
            _hideBubbleCoroutine = StartCoroutine(HideBubbleAfter(bubbleVisibleSeconds));
        }

        public void HideBubble()
        {
            if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
        }

        private IEnumerator HideBubbleAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideBubble();
            SetEmotion(MascotEmotion.Idle);
            _hideBubbleCoroutine = null;
        }

        // === Реакции на события ===

        private void OnOrderCompleted(OrderResolution res)
        {
            if (res.qualityMultiplier >= 0.20f)
                Say("Топ! Качество — огонь!", MascotEmotion.Happy);
            else if (res.qualityMultiplier <= -0.10f)
                Say("Можем лучше...", MascotEmotion.Disappointed);
        }

        private void OnOrderAbandoned(Order order)
        {
            Say("Клиент ушёл :(", MascotEmotion.Sad);
        }

        private void OnRecipeUnlocked(RecipeDefinition recipe)
        {
            Say($"Открыли «{recipe.displayName}»!", MascotEmotion.Excited);
        }

        private void OnMachineUpgraded(MachineTierDefinition tier)
        {
            Say($"Кофемашина {tier.displayName}! Огонь!", MascotEmotion.Excited);
        }

        private void OnWheelSpun(WheelSectorDefinition sector)
        {
            if (sector.prizeType == WheelPrizeType.Nothing)
                Say("Эх, повезёт в следующий раз", MascotEmotion.Sad);
            else
                Say("Ура! Приз!", MascotEmotion.Excited);
        }

        // === Хелперы ===

        private Color ColorForEmotion(MascotEmotion e)
        {
            return e switch
            {
                MascotEmotion.Happy => happyColor,
                MascotEmotion.Excited => excitedColor,
                MascotEmotion.Welcoming => welcomingColor,
                MascotEmotion.Sad => sadColor,
                MascotEmotion.Disappointed => disappointedColor,
                MascotEmotion.Pointing => pointingColor,
                MascotEmotion.Sleeping => sleepingColor,
                _ => idleColor
            };
        }

        private static string LabelForEmotion(MascotEmotion e)
        {
            return e switch
            {
                MascotEmotion.Happy => "🐳 :)",
                MascotEmotion.Excited => "🐳 !",
                MascotEmotion.Welcoming => "🐳 ♥",
                MascotEmotion.Sad => "🐳 :(",
                MascotEmotion.Disappointed => "🐳 :|",
                MascotEmotion.Pointing => "🐳 →",
                MascotEmotion.Sleeping => "🐳 zZz",
                _ => "🐳"
            };
        }
    }
}
```

- [ ] **Step 3: Compile, Commit**

```bash
git add Assets/Scripts/Mascot && git commit -m "feat(mascot): MascotController with emotions + reactive bubbles"
```

---

## Task 2: Speech Bubble UI и привязка `MascotController`

**Files:**
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Добавить речевой пузырь над Дринчиком**

В Hierarchy → `Canvas/MainScreenPanel/DrinchikSection/DrinchikPlaceholder`. Внутри:

1. `UI → Image`, переименуй в `SpeechBubble`. Это контейнер пузыря.
   - Image → Color HEX `FFFFFF`
   - Source Image: дефолтный sliced
   - RectTransform: anchored top of DrinchikPlaceholder, anchored Y=120 относительно центра, W=260, H=80
   - Деактивируй галочкой по умолчанию

2. Внутри `SpeechBubble` → `UI → Text - TextMeshPro`, переименуй в `SpeechText`.
   - Text: `Привет!` (плейсхолдер)
   - Font Size: 16
   - Color: чёрный
   - Alignment: Center+Middle
   - RectTransform: stretch на родителя L/R=12, T/B=8

- [ ] **Step 2: TMP над Дринчиком уже есть (`Label`) — это станет body label**

`DrinchikPlaceholder/Label` уже содержит `🐳 Дринчик`. Это будет наш `bodyLabel` — MascotController будет туда писать эмодзи эмоции.

- [ ] **Step 3: Прицепить `MascotController` на `DrinchikPlaceholder`**

В Hierarchy → `DrinchikPlaceholder` → Add Component → `Mascot Controller`. Заполни:
- Body Image: сам `DrinchikPlaceholder` (Image)
- Body Label: `DrinchikPlaceholder/Label`
- Speech Bubble Root: `DrinchikPlaceholder/SpeechBubble`
- Speech Text: `DrinchikPlaceholder/SpeechBubble/SpeechText`
- (цвета оставь дефолтные)

- [ ] **Step 4: Save, Play тест**

1. Запусти Play. Дринчик в дефолтном голубом цвете.
2. Выдай заказ с высоким качеством → Дринчик зеленеет, пузырь "Топ! Качество — огонь!"
3. Через 3 сек пузырь исчезает, Дринчик возвращается в idle.
4. Дай заказу уйти (5 мин) → Дринчик сереет, пузырь "Клиент ушёл :("

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(mascot): speech bubble + Mascot wired on Drinchik placeholder"
```

---

## Task 3: `OnboardingOverlay` UI

**Files:**
- Modify: `Assets/Scenes/Main.unity`

- [ ] **Step 1: Создать корневой `OnboardingOverlay`**

В Hierarchy → `Canvas` → правый клик → `UI → Panel`. Переименуй в `OnboardingOverlay`.

- RectTransform: stretch на Canvas, L/R/T/B = 0
- Image → Color: HEX `000000` Alpha `120` (полупрозрачное затемнение)
- Деактивируй галочкой по умолчанию

Внутри:

1. `UI → Image`, переименуй в `BubbleBg`:
   - Image → Color HEX `FFFFFF`
   - Source Image: дефолтный sliced
   - RectTransform: anchor middle/center, anchored center, W=320, H=180

2. Внутри `BubbleBg`:
   - `UI → Text - TextMeshPro`, переименуй в `OnboardingText`:
     - Text: `Привет! Я Дринчик. Готов?` (плейсхолдер)
     - Font Size: 18
     - Color: чёрный
     - Alignment: Center+Middle
     - RectTransform: stretch на родителя L/R=16, T=20, Bottom=60
   - `UI → Button - TextMeshPro`, переименуй в `NextButton`:
     - Text: `Дальше`, Size 16, Bold, белый
     - Image HEX `5A8DDC`
     - RectTransform: bottom anchor of BubbleBg, Bottom=16, Left=80, Right=80, H=36

3. `UI → Image`, переименуй в `Pointer`:
   - Image → Color HEX `FFD93D` (жёлтый, как стрелка-указатель)
   - W=40, H=40
   - Деактивирован по умолчанию (включается когда нужно подсветить элемент)

- [ ] **Step 2: Save, продолжаем в Task 4**

---

## Task 4: `OnboardingController` + последовательность шагов

**Files:**
- Create: `Assets/Scripts/UI/OnboardingController.cs`

- [ ] **Step 1: Создать класс**

```csharp
using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Mascot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Простой 5-шаговый онбординг для первого запуска.
    /// Hash шагов — переход по кнопке "Дальше" ИЛИ по ивенту сервиса (например, первый OrderCompleted).
    public class OnboardingController : MonoBehaviour
    {
        public enum StepTrigger
        {
            NextButton,                 // ждём клик "Дальше"
            FirstOrderSpawned,          // ждём первый OrderSpawned
            FirstOrderCompleted         // ждём первый OrderCompleted
        }

        [System.Serializable]
        public class Step
        {
            public string text;
            public StepTrigger trigger;
            public MascotEmotion emotion = MascotEmotion.Welcoming;
        }

        [Header("Refs")]
        public TMP_Text textLabel;
        public Button nextButton;
        public MascotController mascot;

        [Header("Steps (in order)")]
        public List<Step> steps = new();

        private GameStateManager _gsm;
        private int _currentIndex = -1;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);

            // Шаги по умолчанию (если не задано в инспекторе)
            if (steps.Count == 0)
            {
                steps.Add(new Step { text = "Привет! Я Дринчик. Это твоя кофейня. Готов?",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Welcoming });
                steps.Add(new Step { text = "Сюда будут приходить заказы. Подожди первый клиент.",
                                      trigger = StepTrigger.FirstOrderSpawned,
                                      emotion = MascotEmotion.Pointing });
                steps.Add(new Step { text = "Тапни по заказу и приготовь его!",
                                      trigger = StepTrigger.FirstOrderCompleted,
                                      emotion = MascotEmotion.Pointing });
                steps.Add(new Step { text = "Отлично! За деньги покупай рецепты и прокачивай машину в «Магазине».",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Happy });
                steps.Add(new Step { text = "А ещё — крути колесо удачи! Держи бесплатный жетон.",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Excited });
            }
        }

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            if (_gsm.State.onboardingCompleted)
            {
                gameObject.SetActive(false);
                return;
            }

            // Подписки
            _gsm.Orders.OrderSpawned += OnOrderSpawned;
            _gsm.OrderResolution.OrderCompleted += OnOrderCompleted;

            BeginNextStep();
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Orders.OrderSpawned -= OnOrderSpawned;
            _gsm.OrderResolution.OrderCompleted -= OnOrderCompleted;
        }

        private void BeginNextStep()
        {
            _currentIndex++;
            if (_currentIndex >= steps.Count)
            {
                FinishOnboarding();
                return;
            }

            var step = steps[_currentIndex];
            if (textLabel != null) textLabel.text = step.text;
            if (mascot != null) mascot.Say(step.text, step.emotion);

            // Кнопка "Дальше" видна только если триггер NextButton
            if (nextButton != null) nextButton.gameObject.SetActive(step.trigger == StepTrigger.NextButton);

            gameObject.SetActive(true);
        }

        private void OnNext()
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.NextButton) return;

            // На предпоследнем шаге (выдача бесплатного жетона) — даём жетон
            if (_currentIndex == steps.Count - 1)
            {
                _gsm.Wheel.GrantStarterToken();
            }

            BeginNextStep();
        }

        private void OnOrderSpawned(Order order)
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.FirstOrderSpawned) return;
            BeginNextStep();
        }

        private void OnOrderCompleted(OrderResolution res)
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.FirstOrderCompleted) return;
            BeginNextStep();
        }

        private void FinishOnboarding()
        {
            if (_gsm != null)
            {
                _gsm.State.onboardingCompleted = true;
                _gsm.Save.Save(_gsm.State);
            }
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: Прицепить `OnboardingController` на `OnboardingOverlay`**

В Hierarchy → `OnboardingOverlay` → Add Component → `Onboarding Controller`. Заполни:
- Text Label: `BubbleBg/OnboardingText`
- Next Button: `BubbleBg/NextButton`
- Mascot: `DrinchikPlaceholder` (с компонентом MascotController)

(Steps оставь пустым — заполнится в Awake дефолтными.)

- [ ] **Step 3: Save, Play тест**

Чтобы проверить онбординг с нуля, нужно сбросить сейв:
- В компоненте GameStateManager в инспекторе во время Play вызови `Reset Progress()` через **правый клик на компонент → Reset Progress** ИЛИ временно удали ключ PlayerPrefs:
  - В Editor скрипте можно через меню, но проще: вручную в Awake'е GameStateManager поставить временно `Save.Clear();` перед `Load()`.
- ИЛИ удали ключ через `PlayerPrefs.DeleteAll()` (потеряешь все сейвы).

После сброса:
1. Запустил Play → онбординг сразу появляется
2. Жмёшь "Дальше" → следующий шаг
3. Ждёшь спавн заказа → автоматически шаг 3
4. Готовишь заказ → автоматически шаг 4
5. "Дальше" → шаг 5
6. "Дальше" → жетон выдан, онбординг закрылся
7. Перезапусти Play → онбординг НЕ должен повториться (флаг onboardingCompleted сохранён)

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/OnboardingController.cs Assets/Scenes/Main.unity && git commit -m "feat(ui): OnboardingController with 5-step tutorial flow"
```

---

## Task 5: Дать игроку выйти из онбординга через debug-меню

**Files:**
- Modify: `Assets/Scripts/Core/GameStateManager.cs`

Нужна возможность пересбросить онбординг (для отладки и плейтеста).

- [ ] **Step 1: Добавить `[ContextMenu]` методы**

В `GameStateManager.cs` под методом `ResetProgress` добавь:

```csharp
        [ContextMenu("Reset Onboarding Flag")]
        public void ResetOnboardingFlag()
        {
            if (State != null) State.onboardingCompleted = false;
            if (Save != null) Save.Save(State);
            Debug.Log("[GameStateManager] Онбординг сброшен. Перезапусти Play.");
        }

        [ContextMenu("Wipe Save")]
        public void WipeSave()
        {
            if (Save != null) Save.Clear();
            Debug.Log("[GameStateManager] Сейв стёрт. Перезапусти Play для чистой игры.");
        }
```

- [ ] **Step 2: Использование**

В Inspector у `GameRoot/GameStateManager` теперь правый клик по самому компоненту в Inspector → `Reset Onboarding Flag` или `Wipe Save`. Очень удобно для отладки.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs && git commit -m "chore(core): ContextMenu debug helpers for onboarding/save reset"
```

---

## Task 6: Финальная сверка Phase 10

- [ ] **Step 1: Все тесты зелёные**

Run All. ~110 тестов (без новых тестов — Mascot/Onboarding не покрыты unit-тестами, они визуальные).

- [ ] **Step 2: Лайв-сценарий с нуля**

1. Wipe Save → перезапуск Play
2. Онбординг ведёт через 5 шагов
3. После закрытия онбординга у тебя 1 жетон колеса (дан на последнем шаге)
4. Идёт обычная игра
5. Дринчик реагирует на события (см. список выше)

- [ ] **Step 3: git log проверка**

5 коммитов Phase 10.

---

## Self-Review

После прохождения:
1. ✅ `MascotEmotion` enum + 8 эмоций
2. ✅ `MascotController` переключает цвета и пузырь
3. ✅ Реакции на 5 событий сервисов
4. ✅ `OnboardingOverlay` UI
5. ✅ `OnboardingController` ведёт 5-шаговый туториал
6. ✅ Флаг `onboardingCompleted` сохраняется → на 2-м запуске нет повтора
7. ✅ Debug-меню для сброса

**Готово → Phase 11: Save persisted orders + пауза при выходе + балансировка.**

---

## Common Pitfalls

**1. Дринчик не меняет цвет**
Проверь что в инспекторе MascotController поле `Body Image` указывает на сам `DrinchikPlaceholder` (с компонентом Image). Если на другой объект — он и будет красить.

**2. Speech bubble не появляется**
Проверь `Speech Bubble Root` — должен быть deactivated по умолчанию (не активный в Hierarchy). MascotController при `Say` его включает.

**3. Onboarding всегда стартует, даже после прохождения**
Скорее всего флаг не сохраняется. Проверь что `_gsm.Save.Save(_gsm.State)` вызывается в `FinishOnboarding`. И что GameState.onboardingCompleted действительно `true` после.

**4. Onboarding не реагирует на тап по заказу**
Триггер `FirstOrderCompleted` ловит `OrderResolution.OrderCompleted` — но это происходит только после фактической выдачи. Сначала спавн → шаг 3 (FirstOrderSpawned). Потом игрок готовит → выдаёт → шаг 4 (FirstOrderCompleted). Если шаг 4 не срабатывает — проверь подписку на `_gsm.OrderResolution.OrderCompleted` в `Start()`.

**5. После закрытия онбординга интерфейс активен но Дринчик "застрял" с пузырём**
`MascotController.Say` стартует Coroutine который гасит пузырь через 3 сек. Если объект MascotController был неактивен в момент Say — Coroutine не запустится. Решение: убедись что DrinchikPlaceholder активен всё время (включая онбординг).

**6. ContextMenu методы не появляются в инспекторе**
ContextMenu работает только на сериализованных классах. Удостоверься что класс `: MonoBehaviour` и метод `public void` без параметров. Правый клик не на name, а на сам компонент (по нему в шапке).
