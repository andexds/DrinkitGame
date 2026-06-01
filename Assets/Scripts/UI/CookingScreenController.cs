using System.Collections;
using System.Collections.Generic;
using System.Text;
using DrinkitGame.Cooking;
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Тактильный экран готовки: игрок тапает на объекты кухни (KitchenObject).
    /// Каждый объект декларирует, какие шаги CookingFlow он закрывает.
    /// Дополнительно отвечает за фидбэк: fade-in кружки на кофемашине, налив
    /// молока с пульсом, частицы-успех на «положил в стакан» шагах.
    public class CookingScreenController : MonoBehaviour
    {
        [Header("HUD")]
        public TMP_Text orderSummaryLabel;
        public TMP_Text hintLabel;
        public TMP_Text progressLabel;
        public TMP_Text patienceLabel;
        public Button cancelButton;

        [Header("Kitchen objects (любые KitchenObject со сцены — порядок не важен)")]
        [Tooltip("Перетащи все KitchenObject со сцены сюда. Контроллер сам ищет, " +
                 "какие закрывают текущий шаг (через KitchenObject.Handles).")]
        public List<KitchenObject> kitchenObjects = new();

        [Header("Serve (Deliver step)")]
        public Button serveButton;

        [Header("Mini-games")]
        public MiniGameDispatcher miniGameDispatcher;

        // ===================== Визуальный фидбэк =====================

        [Header("Чашки на кофемашине (fade-in при выборе)")]
        [Tooltip("Image 'тут'-чашки, размещённая на CoffeeMachine. По дефолту выключена. " +
                 "Включается с fade-in при тапе на CupHere.")]
        public Image cupHereOnMachine;
        public Sprite cupHereEmpty;
        [Tooltip("Опционально: спрайт 'полной' чашки 'тут' — подменяется после первой налив-шаги.")]
        public Sprite cupHereFull;

        [Tooltip("Image 'с собой'-чашки на CoffeeMachine. По дефолту выключена.")]
        public Image cupTakeawayOnMachine;
        public Sprite cupTakeawayEmpty;
        [Tooltip("Опционально: 'полная' takeaway-чашка. Если null — sprite не меняется.")]
        public Sprite cupTakeawayFull;

        [Tooltip("Длительность fade-in кружки при появлении на машине.")]
        [Range(0.05f, 1f)]
        public float cupFadeInDuration = 0.3f;

        [Header("Налив молока (PourMilk / PourCream)")]
        [Tooltip("Image 'наливающегося молока'. По дефолту выключена. Появляется на milkPourDuration сек.")]
        public Image pouringMilkImage;
        [Tooltip("Сколько секунд показывать наливающееся молоко.")]
        [Range(0.5f, 5f)]
        public float milkPourDuration = 2.5f;
        [Tooltip("Амплитуда пульсации в долях (0.01 = ±1%). Частота 8 Гц зашита.")]
        [Range(0f, 0.1f)]
        public float pourPulseAmplitude = 0.01f;

        [Header("Частицы 'DONE' (после каждого 'положил в стакан' шага)")]
        [Tooltip("UIBurster компонент рядом с кружкой. Контроллер вызывает Burst() " +
                 "после Extract / AddHotWater / PourMilk / PourCream / PourOver / " +
                 "AddSyrup / AddTopping / AddCacao / AddMatcha / Whisk.")]
        public UIBurster successBurster;

        // ===================== Тайминги обычных шагов =====================

        [Header("Timings")]
        [Tooltip("Задержка перед автоматическим переходом дальше для шагов без UI " +
                 "(TakeMilk, TakeCream — НЕ PourMilk/PourCream, у тех своя длительность).")]
        [Range(0.1f, 2f)]
        public float autoStepDelay = 0.3f;

        [Tooltip("Задержка-анимация после тапа на шаг без мини-игры " +
                 "(Extract, AddSyrup, AddTopping, AddCacao, AddMatcha, SetupFilter, AddHotWater).")]
        [Range(0.2f, 3f)]
        public float tapActionDelay = 0.6f;

        // ===================== Состояние =====================

        private Order _order;
        private List<CookingStep> _steps;
        private int _currentIndex;
        private float _qualitySum;
        private int _qualityCount;
        private bool _stepInProgress; // защита от двойного тапа
        private bool _cupFilled;      // флаг для swap к "full" sprite

        private void Awake()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            if (serveButton != null) serveButton.onClick.AddListener(OnServe);

            // Каждый KitchenObject шлёт Tapped когда игрок по нему щёлкнул.
            // Локальная копия ссылки нужна чтобы лямбда не захватывала переменную цикла.
            foreach (var ko in kitchenObjects)
            {
                if (ko == null) continue;
                var local = ko;
                local.Tapped += () => OnObjectTapped(local);
            }
        }

        /// Вызывается из UIRouter.OpenCooking(order) при входе на экран.
        public void Bind(Order order)
        {
            _order = order;
            _steps = CookingFlow.GenerateSteps(order);
            _currentIndex = 0;
            _qualitySum = 0f;
            _qualityCount = 0;
            _stepInProgress = false;
            _cupFilled = false;

            if (orderSummaryLabel != null)
                orderSummaryLabel.text = BuildSummary(order);

            // Прячем тап-зоны, не нужные для рецепта.
            ConfigureVisibilityFor(order);

            // Сбрасываем визуал-фидбэк к стартовому состоянию.
            ResetCupOverlays();
            if (pouringMilkImage != null) pouringMilkImage.gameObject.SetActive(false);

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

        // === Видимость тап-зон ===

        private void ConfigureVisibilityFor(Order order)
        {
            var neededTypes = new HashSet<CookingStepType>();
            foreach (var step in _steps) neededTypes.Add(step.type);

            foreach (var ko in kitchenObjects)
            {
                if (ko == null) continue;
                bool needed = false;
                if (ko.handlesSteps != null)
                {
                    foreach (var type in ko.handlesSteps)
                    {
                        if (neededTypes.Contains(type)) { needed = true; break; }
                    }
                }
                ko.gameObject.SetActive(needed);
            }
        }

        private void ShowCurrentStep()
        {
            if (_steps == null || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];

            // HUD
            if (hintLabel != null) hintLabel.text = step.hint;
            if (progressLabel != null) progressLabel.text = $"Шаг {_currentIndex + 1} из {_steps.Count}";

            // ServeButton — только на Deliver
            if (serveButton != null)
                serveButton.interactable = step.type == CookingStepType.Deliver;

            // Активируем тап-зоны
            foreach (var ko in kitchenObjects)
            {
                if (ko == null) continue;
                bool active = ko.gameObject.activeSelf && ko.Handles(step.type);
                ko.SetActive(active);
            }

            // PourMilk / PourCream — своя удлинённая анимация
            if (step.type == CookingStepType.PourMilk || step.type == CookingStepType.PourCream)
            {
                StartCoroutine(PourMilkSequence());
            }
            // Остальные авто-шаги (TakeMilk / TakeCream) — короткая пауза и дальше
            else if (IsAutoSkip(step.type))
            {
                StartCoroutine(AutoAdvanceAfter(autoStepDelay));
            }
        }

        private static bool IsAutoSkip(CookingStepType t)
        {
            return t == CookingStepType.TakeMilk || t == CookingStepType.TakeCream;
        }

        private IEnumerator AutoAdvanceAfter(float seconds)
        {
            _stepInProgress = true;
            yield return new WaitForSeconds(seconds);
            _stepInProgress = false;
            AdvanceStep();
        }

        // === Обработка тапа на объект кухни ===

        private void OnObjectTapped(KitchenObject ko)
        {
            if (_stepInProgress) return;
            if (_steps == null || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];
            if (!ko.Handles(step.type)) return; // защита

            // TakeCup: проверяем что выбран правильный стакан + запускаем fade-in кружки на машине
            if (step.type == CookingStepType.TakeCup)
            {
                if (ko.isToGoCup != _order.isToGo) return;
                StartCoroutine(TakeCupSequence());
                return;
            }

            // Мини-игры (M1/M2/M3/M4)
            if (step.isMiniGame && miniGameDispatcher != null)
            {
                var tier = GameStateManager.Instance.Machine.CurrentTier;
                bool started = miniGameDispatcher.TryBegin(step, tier);
                if (started)
                {
                    miniGameDispatcher.Completed += OnMiniGameDone;
                    _stepInProgress = true;
                    return;
                }
                _qualitySum += 100f; _qualityCount += 1;
                AdvanceStep();
                return;
            }

            // Обычный тап-шаг — короткая «анимация», потом advance
            StartCoroutine(TapActionThenAdvance(tapActionDelay));
        }

        // === Анимации/последовательности ===

        private IEnumerator TakeCupSequence()
        {
            _stepInProgress = true;
            Image target = _order.isToGo ? cupTakeawayOnMachine : cupHereOnMachine;
            if (target != null)
            {
                target.gameObject.SetActive(true);
                Color c = target.color;
                c.a = 0f; target.color = c;
                float t = 0f;
                while (t < cupFadeInDuration)
                {
                    t += Time.deltaTime;
                    c.a = Mathf.Clamp01(t / cupFadeInDuration);
                    target.color = c;
                    yield return null;
                }
                c.a = 1f; target.color = c;
            }
            else
            {
                yield return new WaitForSeconds(tapActionDelay);
            }
            _stepInProgress = false;
            AdvanceStep();
        }

        private IEnumerator PourMilkSequence()
        {
            _stepInProgress = true;
            if (pouringMilkImage != null)
            {
                pouringMilkImage.gameObject.SetActive(true);
                StartCoroutine(PulseScale(pouringMilkImage.rectTransform, milkPourDuration));
            }
            yield return new WaitForSeconds(milkPourDuration);
            if (pouringMilkImage != null) pouringMilkImage.gameObject.SetActive(false);
            _stepInProgress = false;
            AdvanceStep();
        }

        private IEnumerator PulseScale(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            Vector3 baseScale = rt.localScale;
            float t = 0f;
            while (t < duration && rt != null && rt.gameObject.activeSelf)
            {
                t += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(t * 8f) * pourPulseAmplitude;
                rt.localScale = baseScale * pulse;
                yield return null;
            }
            if (rt != null) rt.localScale = baseScale;
        }

        private IEnumerator TapActionThenAdvance(float seconds)
        {
            _stepInProgress = true;
            yield return new WaitForSeconds(seconds);
            _stepInProgress = false;
            AdvanceStep();
        }

        private void OnMiniGameDone(float quality)
        {
            if (miniGameDispatcher != null) miniGameDispatcher.Completed -= OnMiniGameDone;
            _qualitySum += quality;
            _qualityCount += 1;
            _stepInProgress = false;
            AdvanceStep();
        }

        // === Завершение шага: эффекты и переход ===

        private void AdvanceStep()
        {
            // Эффекты ПОСЛЕ только что завершённого шага (до инкремента).
            if (_steps != null && _currentIndex < _steps.Count)
                OnStepCompletedEffects(_steps[_currentIndex]);

            _currentIndex++;
            if (_currentIndex >= _steps.Count) CompleteOrder();
            else ShowCurrentStep();
        }

        /// Срабатывает после успешного завершения шага: подменяет стакан на «full»
        /// после первой наливки, кидает успех-частицы на «положил в стакан» шагах.
        private void OnStepCompletedEffects(CookingStep step)
        {
            // 1. Первый «налив» → swap стакана на full-спрайт
            if (!_cupFilled && IsFillStep(step.type))
            {
                SwapCupToFull();
                _cupFilled = true;
            }

            // 2. Частицы успеха на «положил в стакан» шагах
            if (IsAddToCupStep(step.type) && successBurster != null)
                successBurster.Burst();
        }

        private static bool IsFillStep(CookingStepType t)
        {
            return t == CookingStepType.Extract
                || t == CookingStepType.AddHotWater
                || t == CookingStepType.PourMilk
                || t == CookingStepType.PourCream
                || t == CookingStepType.PourOver;
        }

        private static bool IsAddToCupStep(CookingStepType t)
        {
            return t == CookingStepType.Extract
                || t == CookingStepType.AddHotWater
                || t == CookingStepType.PourMilk
                || t == CookingStepType.PourCream
                || t == CookingStepType.PourOver
                || t == CookingStepType.AddSyrup
                || t == CookingStepType.AddTopping
                || t == CookingStepType.AddCacao
                || t == CookingStepType.AddMatcha
                || t == CookingStepType.Whisk;
        }

        private void SwapCupToFull()
        {
            if (_order == null) return;
            if (_order.isToGo)
            {
                if (cupTakeawayOnMachine != null && cupTakeawayFull != null)
                    cupTakeawayOnMachine.sprite = cupTakeawayFull;
            }
            else
            {
                if (cupHereOnMachine != null && cupHereFull != null)
                    cupHereOnMachine.sprite = cupHereFull;
            }
        }

        private void ResetCupOverlays()
        {
            if (cupHereOnMachine != null)
            {
                cupHereOnMachine.gameObject.SetActive(false);
                if (cupHereEmpty != null) cupHereOnMachine.sprite = cupHereEmpty;
                var c = cupHereOnMachine.color; c.a = 1f; cupHereOnMachine.color = c;
            }
            if (cupTakeawayOnMachine != null)
            {
                cupTakeawayOnMachine.gameObject.SetActive(false);
                if (cupTakeawayEmpty != null) cupTakeawayOnMachine.sprite = cupTakeawayEmpty;
                var c = cupTakeawayOnMachine.color; c.a = 1f; cupTakeawayOnMachine.color = c;
            }
        }

        // === Глобальные кнопки ===

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

        // === Хелперы ===

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
