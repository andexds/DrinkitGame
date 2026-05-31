# Phase 8b — 4 Mini-Games Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить заглушку Quality=100 из Phase 8a на **реальные мини-игры**. Когда CookingFlow доходит до шага `isMiniGame = true`, показывается оверлей с одной из 4 мини-игр. Игрок проходит её → возвращается quality 0..100 → готовка продолжается. Итоговое Quality = среднее всех мини-игр в рецепте → влияет на чек.

**Architecture:**
- `IMiniGame` interface — общий API: `void Begin(MachineTierDefinition tier)`, `event Action<float> Completed`.
- 4 MonoBehaviour-классы реализуют интерфейс:
  - `M1GrindingMiniGame` — горизонтальная полоса с движущимся индикатором, тап-стоп в зелёной зоне
  - `M2MilkSteamingMiniGame` — вертикальный gauge, hold-to-fill, отпускай в зелёной зоне
  - `M3PourOverMiniGame` — long-tap нужной длительности (с зелёной зоной по времени)
  - `M4WhiskingMiniGame` — rapid-tap счётчик за 2 сек
- `MiniGameOverlay` — root GameObject, держит 4 sub-overlay'я (по одному на мини-игру), активирует нужный.
- `CookingScreenController` обновлён — на mini-game шагах запускает соответствующий оверлей и ждёт Completed.
- Тесты — pure-функции расчёта Quality (без Unity).

**Tech Stack:** uGUI · TMPro · MonoBehaviour Update для таймеров · NUnit (тесты pure-логики).

**Конец фазы:** Готовишь капучино → шаг "Намели кофе" → появляется оверлей с движущейся полосой → ловишь зелёную зону → quality 0..100 → next step "Запусти эспрессо-машину". Затем "Вспень молоко" → другой оверлей. И т.д. После последнего шага — OrderResolution с реальной средней Quality и бонусом ±20%.

---

## Task 1: `IMiniGame` интерфейс + базовая инфраструктура

**Files:**
- Create: `Assets/Scripts/Cooking/IMiniGame.cs`

- [ ] **Step 1: Создать `IMiniGame.cs`**

В `Assets/Scripts/Cooking/`:

```csharp
using System;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Общий интерфейс для всех мини-игр готовки.
    /// Реализации — MonoBehaviour, прикреплённые к UI-оверлеям.
    public interface IMiniGame
    {
        /// Запустить мини-игру с параметрами текущей машины (определяет ширину зелёной зоны).
        void Begin(MachineTierDefinition tier);

        /// Стреляет когда игрок завершил мини-игру. Параметр — quality 0..100.
        event Action<float> Completed;
    }
}
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Cooking/IMiniGame.cs Assets/Scripts/Cooking/IMiniGame.cs.meta && git commit -m "feat(cooking): IMiniGame interface for cooking skill challenges"
```

---

## Task 2: `M1GrindingMiniGame` — помол с движущимся индикатором

**Files:**
- Create: `Assets/Scripts/Cooking/M1GrindingMiniGame.cs`
- Create: `Assets/Scripts/Cooking/MiniGameQuality.cs` (общие pure-функции)
- Create: `Assets/Tests/EditMode/MiniGameQualityTests.cs`

- [ ] **Step 1: `MiniGameQuality.cs` (pure-логика расчёта)**

В `Assets/Scripts/Cooking/`:

```csharp
using UnityEngine;

namespace DrinkitGame.Cooking
{
    /// Чистые функции расчёта Quality для мини-игр. Без MonoBehaviour — тестируется.
    public static class MiniGameQuality
    {
        /// Quality = 100 если позиция в центре зелёной зоны; линейно падает к границам.
        /// position — где остановился индикатор (0..1).
        /// zoneCenter — центр зелёной зоны (0..1).
        /// zoneWidth — ширина зелёной зоны (0..1).
        public static float FromZoneHit(float position, float zoneCenter, float zoneWidth)
        {
            float halfWidth = zoneWidth * 0.5f;
            float distance = Mathf.Abs(position - zoneCenter);
            if (distance <= halfWidth)
            {
                // Внутри зоны: 100 в центре, линейно до 60 на краях
                float normalized = 1f - (distance / halfWidth);
                return 60f + normalized * 40f;
            }
            // Вне зоны: 60 на границе, линейно падает к 0
            float outside = distance - halfWidth;
            return Mathf.Max(0f, 60f - outside * 300f);
        }

        /// Quality для rapid-tap: количество тапов / целевое количество, кэп 100.
        public static float FromTapCount(int taps, int target)
        {
            if (target <= 0) return 0f;
            float ratio = (float)taps / target;
            return Mathf.Clamp(ratio * 100f, 0f, 100f);
        }
    }
}
```

- [ ] **Step 2: Тесты `MiniGameQualityTests.cs`**

В `Assets/Tests/EditMode/`:

```csharp
using DrinkitGame.Cooking;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class MiniGameQualityTests
    {
        [Test]
        public void FromZoneHit_DeadCenter_Returns100()
        {
            float q = MiniGameQuality.FromZoneHit(0.5f, 0.5f, 0.2f);
            Assert.AreEqual(100f, q, 0.01f);
        }

        [Test]
        public void FromZoneHit_EdgeOfZone_Returns60()
        {
            // позиция на правой границе зоны
            float q = MiniGameQuality.FromZoneHit(0.6f, 0.5f, 0.2f);
            Assert.That(q, Is.InRange(58f, 62f), $"На краю зоны должно быть ~60, было {q}");
        }

        [Test]
        public void FromZoneHit_OutsideZone_Penalizes()
        {
            float q = MiniGameQuality.FromZoneHit(0.0f, 0.5f, 0.2f);
            Assert.Less(q, 50f, "Сильно вне зоны должно быть < 50");
        }

        [Test]
        public void FromZoneHit_FarOutside_ReturnsZero()
        {
            float q = MiniGameQuality.FromZoneHit(0.0f, 1.0f, 0.05f);
            Assert.AreEqual(0f, q, 0.01f);
        }

        [Test]
        public void FromTapCount_OnTarget_Returns100()
        {
            Assert.AreEqual(100f, MiniGameQuality.FromTapCount(12, 12), 0.01f);
        }

        [Test]
        public void FromTapCount_Half_Returns50()
        {
            Assert.AreEqual(50f, MiniGameQuality.FromTapCount(6, 12), 0.01f);
        }

        [Test]
        public void FromTapCount_Above_CapsAt100()
        {
            Assert.AreEqual(100f, MiniGameQuality.FromTapCount(30, 12), 0.01f);
        }
    }
}
```

- [ ] **Step 3: Создать `M1GrindingMiniGame.cs`**

```csharp
using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M1: помол кофе.
    /// Индикатор движется по горизонтальной полосе. Тап → останавливается → quality по близости к центру зелёной зоны.
    public class M1GrindingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public RectTransform bar;            // горизонтальная полоса
        public RectTransform indicator;      // движущийся индикатор
        public RectTransform greenZone;      // подсветка зелёной зоны
        public Button stopButton;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float indicatorSpeed = 0.8f;  // полных проходов в секунду
        public float zoneCenter = 0.5f;      // центр зелёной зоны (0..1)

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _position;             // 0..1
        private int _direction = 1;
        private bool _running;

        private void Awake()
        {
            if (stopButton != null) stopButton.onClick.AddListener(OnStop);
        }

        public void Begin(MachineTierDefinition tier)
        {
            _zoneWidth = tier != null ? tier.grindingZoneWidth : 0.25f;
            _position = 0f;
            _direction = 1;
            _running = true;
            UpdateZoneVisuals();
            UpdateIndicator();
            if (instructionLabel != null) instructionLabel.text = "Тапни когда индикатор в зелёной зоне!";
        }

        private void Update()
        {
            if (!_running) return;
            _position += _direction * indicatorSpeed * Time.deltaTime;
            if (_position >= 1f) { _position = 1f; _direction = -1; }
            else if (_position <= 0f) { _position = 0f; _direction = 1; }
            UpdateIndicator();
        }

        private void OnStop()
        {
            if (!_running) return;
            _running = false;
            float quality = MiniGameQuality.FromZoneHit(_position, zoneCenter, _zoneWidth);
            Completed?.Invoke(quality);
        }

        private void UpdateIndicator()
        {
            if (bar == null || indicator == null) return;
            float barWidth = bar.rect.width;
            float x = -barWidth * 0.5f + _position * barWidth;
            indicator.anchoredPosition = new Vector2(x, indicator.anchoredPosition.y);
        }

        private void UpdateZoneVisuals()
        {
            if (bar == null || greenZone == null) return;
            float barWidth = bar.rect.width;
            float zoneW = _zoneWidth * barWidth;
            float x = -barWidth * 0.5f + zoneCenter * barWidth;
            greenZone.anchoredPosition = new Vector2(x, greenZone.anchoredPosition.y);
            greenZone.sizeDelta = new Vector2(zoneW, greenZone.sizeDelta.y);
        }
    }
}
```

- [ ] **Step 4: Run All — все зелёные**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Cooking Assets/Tests/EditMode/MiniGameQualityTests.cs Assets/Tests/EditMode/MiniGameQualityTests.cs.meta && git commit -m "feat(cooking): MiniGameQuality utils + M1 Grinding mini-game"
```

---

## Task 3: `M2MilkSteamingMiniGame` — вспенивание молока

**Files:**
- Create: `Assets/Scripts/Cooking/M2MilkSteamingMiniGame.cs`

- [ ] **Step 1: Создать класс**

```csharp
using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M2: hold-to-fill, отпускай в зелёной зоне.
    public class M2MilkSteamingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Image fillImage;              // image с Image Type=Filled, Vertical
        public RectTransform greenZoneOverlay;
        public Button holdButton;            // нужен EventTrigger для PointerDown/Up
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float fillSpeed = 1.5f;       // долей в секунду
        public float zoneCenter = 0.7f;      // 0..1
        public float maxFill = 1f;

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _fill;
        private bool _running;
        private bool _holding;

        private void Awake()
        {
            // Добавляем EventTrigger программно
            if (holdButton != null)
            {
                var trigger = holdButton.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = holdButton.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, _ => _holding = true);
                AddTrigger(trigger, EventTriggerType.PointerUp, _ => OnRelease());
                AddTrigger(trigger, EventTriggerType.PointerExit, _ => { if (_holding) OnRelease(); });
            }
        }

        public void Begin(MachineTierDefinition tier)
        {
            _zoneWidth = tier != null ? tier.milkSteamingZoneWidth : 0.25f;
            _fill = 0f;
            _running = true;
            _holding = false;
            UpdateZoneOverlay();
            UpdateFill();
            if (instructionLabel != null) instructionLabel.text = "Удерживай, отпусти в зелёной зоне";
        }

        private void Update()
        {
            if (!_running) return;
            if (_holding)
            {
                _fill = Mathf.Min(maxFill, _fill + fillSpeed * Time.deltaTime);
                UpdateFill();
                if (_fill >= maxFill) OnRelease(); // авто-стоп при переполнении
            }
        }

        private void OnRelease()
        {
            if (!_running) return;
            _holding = false;
            _running = false;
            float quality = MiniGameQuality.FromZoneHit(_fill, zoneCenter, _zoneWidth);
            Completed?.Invoke(quality);
        }

        private void UpdateFill()
        {
            if (fillImage != null) fillImage.fillAmount = _fill;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null || fillImage == null) return;
            var rect = fillImage.rectTransform.rect;
            float zoneH = _zoneWidth * rect.height;
            float yCenter = -rect.height * 0.5f + zoneCenter * rect.height;
            greenZoneOverlay.anchoredPosition = new Vector2(greenZoneOverlay.anchoredPosition.x, yCenter);
            greenZoneOverlay.sizeDelta = new Vector2(greenZoneOverlay.sizeDelta.x, zoneH);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => action(d));
            trigger.triggers.Add(entry);
        }
    }
}
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Cooking/M2MilkSteamingMiniGame.cs Assets/Scripts/Cooking/M2MilkSteamingMiniGame.cs.meta && git commit -m "feat(cooking): M2 Milk Steaming mini-game (hold-and-release)"
```

---

## Task 4: `M3PourOverMiniGame` — long-tap нужной длительности

**Files:**
- Create: `Assets/Scripts/Cooking/M3PourOverMiniGame.cs`

- [ ] **Step 1: Создать класс**

```csharp
using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M3: long-tap нужной длительности.
    /// Игрок удерживает кнопку. Прогресс растёт. Цель — отпустить в "зелёной зоне" длительности.
    public class M3PourOverMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Image progressFill;
        public RectTransform greenZoneOverlay;
        public Button holdButton;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float targetDuration = 3f;    // секунды
        public float maxDuration = 5f;

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _heldSeconds;
        private bool _running;
        private bool _holding;

        private void Awake()
        {
            if (holdButton != null)
            {
                var trigger = holdButton.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = holdButton.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, _ => _holding = true);
                AddTrigger(trigger, EventTriggerType.PointerUp, _ => OnRelease());
                AddTrigger(trigger, EventTriggerType.PointerExit, _ => { if (_holding) OnRelease(); });
            }
        }

        public void Begin(MachineTierDefinition tier)
        {
            _zoneWidth = tier != null ? tier.pourOverZoneWidth : 0.25f;
            _heldSeconds = 0f;
            _running = true;
            _holding = false;
            UpdateZoneOverlay();
            UpdateProgress();
            if (instructionLabel != null) instructionLabel.text = "Удерживай нужное время";
        }

        private void Update()
        {
            if (!_running) return;
            if (_holding)
            {
                _heldSeconds = Mathf.Min(maxDuration, _heldSeconds + Time.deltaTime);
                UpdateProgress();
                if (_heldSeconds >= maxDuration) OnRelease();
            }
        }

        private void OnRelease()
        {
            if (!_running) return;
            _holding = false;
            _running = false;
            float position = _heldSeconds / maxDuration; // 0..1
            float zoneCenter = targetDuration / maxDuration;
            float quality = MiniGameQuality.FromZoneHit(position, zoneCenter, _zoneWidth);
            Completed?.Invoke(quality);
        }

        private void UpdateProgress()
        {
            if (progressFill != null) progressFill.fillAmount = _heldSeconds / maxDuration;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null || progressFill == null) return;
            var rect = progressFill.rectTransform.rect;
            float w = _zoneWidth * rect.width;
            float zoneCenter = targetDuration / maxDuration;
            float x = -rect.width * 0.5f + zoneCenter * rect.width;
            greenZoneOverlay.anchoredPosition = new Vector2(x, greenZoneOverlay.anchoredPosition.y);
            greenZoneOverlay.sizeDelta = new Vector2(w, greenZoneOverlay.sizeDelta.y);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => action(d));
            trigger.triggers.Add(entry);
        }
    }
}
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Cooking/M3PourOverMiniGame.cs Assets/Scripts/Cooking/M3PourOverMiniGame.cs.meta && git commit -m "feat(cooking): M3 Pour-over mini-game (long-tap with target duration)"
```

---

## Task 5: `M4WhiskingMiniGame` — rapid-tap

**Files:**
- Create: `Assets/Scripts/Cooking/M4WhiskingMiniGame.cs`

- [ ] **Step 1: Создать класс**

```csharp
using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M4: тапай быстро 2 сек, цель — 12 тапов.
    public class M4WhiskingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Button tapButton;
        public TMP_Text counterLabel;
        public TMP_Text timerLabel;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float duration = 2f;
        public int targetTaps = 12;

        public event Action<float> Completed;

        private int _taps;
        private float _remaining;
        private bool _running;

        private void Awake()
        {
            if (tapButton != null) tapButton.onClick.AddListener(OnTap);
        }

        public void Begin(MachineTierDefinition tier)
        {
            _taps = 0;
            _remaining = duration;
            _running = true;
            UpdateLabels();
            if (instructionLabel != null) instructionLabel.text = "Тапай быстро!";
        }

        private void Update()
        {
            if (!_running) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                UpdateLabels();
                float quality = MiniGameQuality.FromTapCount(_taps, targetTaps);
                Completed?.Invoke(quality);
                return;
            }
            UpdateLabels();
        }

        private void OnTap()
        {
            if (!_running) return;
            _taps++;
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (counterLabel != null) counterLabel.text = $"Тапов: {_taps} / {targetTaps}";
            if (timerLabel != null) timerLabel.text = $"{_remaining:0.0} сек";
        }
    }
}
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Cooking/M4WhiskingMiniGame.cs Assets/Scripts/Cooking/M4WhiskingMiniGame.cs.meta && git commit -m "feat(cooking): M4 Whisking mini-game (rapid-tap counter)"
```

---

## Task 6: `MiniGameDispatcher` — выбирает нужную мини-игру по типу шага

**Files:**
- Create: `Assets/Scripts/Cooking/MiniGameDispatcher.cs`

- [ ] **Step 1: Создать класс**

```csharp
using System;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.Cooking
{
    /// Управляет 4 мини-играми: знает какую открыть для какого CookingStepType.
    /// Один MonoBehaviour висит на MiniGameOverlay GameObject; держит ссылки на 4 sub-overlay'я.
    public class MiniGameDispatcher : MonoBehaviour
    {
        [Header("Mini-game sub-overlays")]
        public GameObject m1Root;
        public M1GrindingMiniGame m1;

        public GameObject m2Root;
        public M2MilkSteamingMiniGame m2;

        public GameObject m3Root;
        public M3PourOverMiniGame m3;

        public GameObject m4Root;
        public M4WhiskingMiniGame m4;

        public event Action<float> Completed;

        private IMiniGame _active;

        /// Запустить мини-игру, соответствующую шагу. Возвращает true если запуск произошёл (шаг — мини-игра).
        public bool TryBegin(CookingStep step, MachineTierDefinition tier)
        {
            gameObject.SetActive(true);
            SetActive(m1Root, false);
            SetActive(m2Root, false);
            SetActive(m3Root, false);
            SetActive(m4Root, false);

            DetachCurrent();

            switch (step.type)
            {
                case CookingStepType.GrindCoffee:
                    SetActive(m1Root, true);
                    _active = m1;
                    break;
                case CookingStepType.SteamMilk:
                case CookingStepType.SteamCream:
                    SetActive(m2Root, true);
                    _active = m2;
                    break;
                case CookingStepType.PourOver:
                    SetActive(m3Root, true);
                    _active = m3;
                    break;
                case CookingStepType.Whisk:
                    SetActive(m4Root, true);
                    _active = m4;
                    break;
                default:
                    // Шаг не является мини-игрой — оверлей не нужен
                    gameObject.SetActive(false);
                    return false;
            }

            if (_active != null)
            {
                _active.Completed += OnCompleted;
                _active.Begin(tier);
                return true;
            }
            return false;
        }

        private void OnCompleted(float quality)
        {
            DetachCurrent();
            gameObject.SetActive(false);
            Completed?.Invoke(quality);
        }

        private void DetachCurrent()
        {
            if (_active != null)
            {
                _active.Completed -= OnCompleted;
                _active = null;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
```

- [ ] **Step 2: Compile, Commit**

```bash
git add Assets/Scripts/Cooking/MiniGameDispatcher.cs Assets/Scripts/Cooking/MiniGameDispatcher.cs.meta && git commit -m "feat(cooking): MiniGameDispatcher routes step type to mini-game overlay"
```

---

## Task 7: Собрать `MiniGameOverlay` в сцене

**Files:**
- Modify: `Assets/Scenes/Main.unity`

Это самая трудоёмкая часть Phase 8b — создать 4 sub-overlay'я с UI.

- [ ] **Step 1: Создать корневой `MiniGameOverlay`**

В Hierarchy → `Canvas` → правый клик → `UI → Panel`. Переименуй в `MiniGameOverlay`.

- RectTransform: stretch на весь Canvas, L/R/T/B = 0
- Image → Color: HEX `000000` Alpha `180` (полупрозрачное затемнение)
- **Деактивируй GameObject** (галочка слева от имени в Inspector).

- [ ] **Step 2: Sub-overlay M1 — Grinding**

Внутри `MiniGameOverlay` → `Create Empty` → `M1Root`.
- RectTransform: stretch (или anchor center с размером по контенту)

Внутри `M1Root`:

1. TMP `Instruction`: Text `Тапни когда индикатор в зелёной зоне!`, Size 18, Bold, белый, Center+Middle, anchored top, Y=-100

2. UI Image `Bar`:
   - RectTransform: anchor center, W=300, H=24
   - Image → Color HEX `444444`
   - Source Image: дефолтный sliced

3. UI Image `GreenZone` (дочерний к Bar, или соседний — лучше соседний, отдельный RectTransform):
   - Анкер middle/center
   - Image → Color HEX `2D9F4E` Alpha 180
   - W=60, H=24 (ширина обновится из кода)

4. UI Image `Indicator` (соседний с Bar):
   - Анкер middle/center
   - Image → Color белый
   - W=8, H=32

5. UI Button - TMP `StopButton`:
   - Анкер bottom/center, anchored Y=100, W=200, H=60
   - Image HEX `5A8DDC`
   - Text inside: `СТОП`, Size 24, Bold, белый

- [ ] **Step 3: Подключить компонент `M1GrindingMiniGame`**

`M1Root` → Add Component → `M1 Grinding Mini Game`. Заполни:
- Bar: `M1Root/Bar`
- Indicator: `M1Root/Indicator`
- Green Zone: `M1Root/GreenZone`
- Stop Button: `M1Root/StopButton`
- Instruction Label: `M1Root/Instruction`

- [ ] **Step 4: Sub-overlay M2 — Milk Steaming**

Внутри `MiniGameOverlay` → `Create Empty` → `M2Root`. Деактивируй галочкой.

Внутри `M2Root`:

1. TMP `Instruction`: Text `Удерживай, отпусти в зелёной зоне`, Size 18, Bold, белый, anchored top Y=-100

2. UI Image `BgGauge`: RectTransform anchored center W=80, H=300, color HEX `444444`

3. UI Image `FillImage` (внутри или соседний):
   - RectTransform: те же размеры, что у BgGauge
   - Image → Color: HEX `5A8DDC`
   - Image Type: `Filled`, Fill Method: `Vertical`, Fill Origin: `Bottom`
   - Fill Amount: 0

4. UI Image `GreenZoneOverlay`:
   - RectTransform: anchored center, W=80, H=60, color HEX `2D9F4E` Alpha 180

5. UI Button - TMP `HoldButton`:
   - Анкер bottom/center anchored Y=100, W=240, H=80
   - Image HEX `5A8DDC`
   - Text: `ДЕРЖИ`, Size 22, Bold, белый

- [ ] **Step 5: Подключить компонент `M2MilkSteamingMiniGame`**

`M2Root` → Add Component → `M2 Milk Steaming Mini Game`. Заполни:
- Fill Image: `M2Root/FillImage`
- Green Zone Overlay: `M2Root/GreenZoneOverlay`
- Hold Button: `M2Root/HoldButton`
- Instruction Label: `M2Root/Instruction`

- [ ] **Step 6: Sub-overlay M3 — Pour-Over**

Внутри `MiniGameOverlay` → `Create Empty` → `M3Root`. Деактивируй.

Внутри:

1. TMP `Instruction`: Text `Удерживай нужное время`, Size 18, Bold, белый
2. UI Image `BgBar`: anchored center, W=320, H=24, color HEX `444444`
3. UI Image `ProgressFill`:
   - Same size as BgBar
   - Image → Color HEX `5A8DDC`
   - Image Type: Filled, Fill Method: Horizontal, Origin: Left
4. UI Image `GreenZoneOverlay`: anchored center, W=80, H=24, HEX `2D9F4E` Alpha 180
5. UI Button `HoldButton`: anchored bottom Y=100, W=240, H=80, HEX `5A8DDC`, text `ДЕРЖИ`

- [ ] **Step 7: Подключить `M3PourOverMiniGame`**

Add Component, заполни:
- Progress Fill: `M3Root/ProgressFill`
- Green Zone Overlay: `M3Root/GreenZoneOverlay`
- Hold Button: `M3Root/HoldButton`
- Instruction Label: `M3Root/Instruction`

- [ ] **Step 8: Sub-overlay M4 — Whisking**

Внутри `MiniGameOverlay` → `Create Empty` → `M4Root`. Деактивируй.

Внутри:

1. TMP `Instruction`: Text `Тапай быстро!`, Size 20, Bold, белый
2. TMP `Counter`: Text `Тапов: 0 / 12`, Size 32, Bold, белый, anchored center Y=40
3. TMP `Timer`: Text `2.0 сек`, Size 24, белый, anchored center Y=0
4. UI Button `TapButton`:
   - anchored center Y=-100, W=280, H=120
   - Image HEX `5A8DDC`
   - Text: `ТАП!`, Size 36, Bold, белый

- [ ] **Step 9: Подключить `M4WhiskingMiniGame`**

Add Component, заполни:
- Tap Button: `M4Root/TapButton`
- Counter Label: `M4Root/Counter`
- Timer Label: `M4Root/Timer`
- Instruction Label: `M4Root/Instruction`

- [ ] **Step 10: Прицепить `MiniGameDispatcher` к корневому `MiniGameOverlay`**

`MiniGameOverlay` → Add Component → `Mini Game Dispatcher`. Заполни:
- M1 Root: `M1Root` (GameObject)
- M1: компонент M1GrindingMiniGame на M1Root
- M2 Root / M2 — аналогично
- M3 Root / M3 — аналогично
- M4 Root / M4 — аналогично

- [ ] **Step 11: Save, Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "feat(cooking): assemble MiniGameOverlay with 4 sub-overlays in scene"
```

---

## Task 8: Подключить `MiniGameDispatcher` в `CookingScreenController`

**Files:**
- Modify: `Assets/Scripts/UI/CookingScreenController.cs`

- [ ] **Step 1: Добавить ссылку и логику**

Открой файл. Добавь поле:

```csharp
        [Header("Mini-games")]
        public DrinkitGame.Cooking.MiniGameDispatcher miniGameDispatcher;
```

Замени метод `OnAdvance` на:

```csharp
        private void OnAdvance()
        {
            if (_steps == null || _currentIndex >= _steps.Count) return;
            var step = _steps[_currentIndex];

            if (step.isMiniGame && miniGameDispatcher != null)
            {
                var tier = GameStateManager.Instance.Machine.CurrentTier;
                bool started = miniGameDispatcher.TryBegin(step, tier);
                if (started)
                {
                    miniGameDispatcher.Completed += OnMiniGameDone;
                    return;
                }
                // если по какой-то причине не стартовало — fallback: Quality=100
                _qualitySum += 100f;
                _qualityCount += 1;
            }

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
```

- [ ] **Step 2: Подключить ссылку в инспекторе**

В Hierarchy → `CookingScreenPanel` → компонент `Cooking Screen Controller` → поле `Mini Game Dispatcher`: перетащи `MiniGameOverlay` (GameObject).

- [ ] **Step 3: Save, Play, лайв-тест**

1. Запусти Play. Купи рецепт американо.
2. Готовь американо → на шаге "Намели кофе (M1)" жми "Дальше"
3. Появится M1 оверлей с движущимся индикатором
4. Тапни СТОП → измерится quality
5. Возврат в Cooking-экран, следующий шаг
6. Дойди до выдачи → OrderResult покажет реальное Quality

Аналогично для капучино (M1 + M2), фильтра (M1 + M3), матчи (M4).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/CookingScreenController.cs Assets/Scenes/Main.unity && git commit -m "feat(cooking): CookingScreenController launches mini-games via dispatcher"
```

---

## Task 9: Финальная сверка Phase 8b

- [ ] **Step 1: Все тесты зелёные** — Run All. ~100 зелёных тестов.

- [ ] **Step 2: Лайв-сценарий**

Сделать капучино с попаданием в зелёные зоны:
- M1 quality ~95
- M2 quality ~90
- Среднее ~92.5 → +20% бонус качества
- Скорость <60 сек → +30%
- Итого 250 × (1 + 0.3 + 0.2) = 375 ₽ за капучино

Промахнуться:
- M1 quality 30
- M2 quality 40
- Среднее 35 → -10% штраф
- Итого 250 × (1 + 0.3 - 0.1) = 300 ₽

OrderResult должен показывать разные числа в зависимости от качества.

- [ ] **Step 3: git log проверка**

8 коммитов Phase 8b.

---

## Self-Review

После прохождения:
1. ✅ 4 мини-игры реализованы (M1/M2/M3/M4)
2. ✅ MiniGameQuality + 7 тестов
3. ✅ MiniGameDispatcher выбирает по типу шага
4. ✅ CookingScreenController запускает мини-игры
5. ✅ Реальное Quality влияет на чек

**Готово → Phase 9: Колесо удачи.**

---

## Common Pitfalls

**1. `EventTrigger` не реагирует на PointerDown/Up**
Причина: GameObject не имеет компонента `Image` с `Raycast Target = true` ИЛИ нет `EventSystem` в сцене. Проверь: Canvas/EventSystem должен существовать (он был с Phase 1).

**2. Indicator в M1 не движется**
Причина: `bar` или `indicator` RectTransform не назначены. Или `bar.rect.width = 0` (если bar свернулся в 0). Проверь что у Bar есть размер W=300.

**3. M2 fill не растёт**
Причина: на Image не выставлен `Image Type = Filled`. По дефолту он `Simple`. Поставь Filled + Vertical + Bottom origin.

**4. M3/M2 быстро отпускаются (Pointer Exit срабатывает)**
Это by-design — если палец сошёл с кнопки, релиз. Если бесит — можно убрать PointerExit-обработчик (просто закомментируй строку с него).

**5. Mini-game overlay блокирует тапы по cooking screen**
By-design — overlay должен блокировать. Если overlay не блокирует — добавь компонент `Image` с `Raycast Target = true` на корневой MiniGameOverlay.

**6. Quality всегда 100 после мини-игры**
Причина: CookingScreenController упал на ветку fallback (`started = false`). Скорее всего `miniGameDispatcher` не назначен в инспекторе.

**7. Несколько мини-игр накладываются**
Причина: `SetActive` не отключает другие. Проверь что в `MiniGameDispatcher.TryBegin` все 4 root'а гасятся перед включением нужного.

**8. `M3` бесконечно долго — индикатор переполняется**
В `Update` есть `if (_heldSeconds >= maxDuration) OnRelease()` — авто-релиз при переполнении. Если игнорится — проверь что MonoBehaviour.Update вызывается (объект активен).
