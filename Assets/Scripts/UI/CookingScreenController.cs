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
    /// Контроллер активирует подходящие объекты на каждом шаге, авто-пропускает
    /// шаги без UI (TakeMilk/TakeCream/PourMilk/PourCream) и включает ServeButton
    /// только на Deliver-шаге.
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

        [Header("Timings")]
        [Tooltip("Задержка перед автоматическим переходом дальше для шагов без UI " +
                 "(TakeMilk, TakeCream, PourMilk, PourCream).")]
        [Range(0.1f, 2f)]
        public float autoStepDelay = 0.3f;

        [Tooltip("Задержка-анимация после тапа на шаг без мини-игры " +
                 "(Extract, AddSyrup, AddTopping, AddCacao, AddMatcha, SetupFilter, AddHotWater).")]
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

            // Каждый KitchenObject шлёт нам Tapped когда игрок по нему щёлкнул.
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

            if (orderSummaryLabel != null)
                orderSummaryLabel.text = BuildSummary(order);

            // Прячем тап-зоны, которые не нужны для этого рецепта.
            ConfigureVisibilityFor(order);

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

        // === Видимость и активация объектов ===

        /// Прячем тап-зоны, которые не нужны для текущего рецепта (например, фильтр
        /// для эспрессо или матча-банка для капучино). Контроллер ищет KitchenObject
        /// по типу шага: если рецепт этот тип никогда не вызовет — объект скрывается.
        private void ConfigureVisibilityFor(Order order)
        {
            var neededTypes = new HashSet<CookingStepType>();
            foreach (var step in _steps) neededTypes.Add(step.type);

            foreach (var ko in kitchenObjects)
            {
                if (ko == null) continue;
                // Объект нужен, если хотя бы один из его handlesSteps встречается в шагах рецепта.
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

            // Активируем подходящие объекты
            foreach (var ko in kitchenObjects)
            {
                if (ko == null) continue;
                bool active = ko.gameObject.activeSelf && ko.Handles(step.type);
                ko.SetActive(active);
            }

            // Авто-шаги (без UI) — пропускаем с задержкой
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

        // === Обработка тапа на объект кухни ===

        private void OnObjectTapped(KitchenObject ko)
        {
            if (_stepInProgress) return;
            if (_steps == null || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];
            if (!ko.Handles(step.type)) return; // защита, хотя SetActive(false) уже отрубил кнопку

            // Особый случай: TakeCup — нужный стакан должен совпасть с order.isToGo
            if (step.type == CookingStepType.TakeCup && ko.isToGoCup != _order.isToGo)
                return;

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
                // Не удалось запустить — fallback quality=100
                _qualitySum += 100f; _qualityCount += 1;
                AdvanceStep();
                return;
            }

            // Обычный тап-шаг — короткая «анимация», потом advance
            StartCoroutine(TapActionThenAdvance(tapActionDelay));
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

        private void AdvanceStep()
        {
            _currentIndex++;
            if (_currentIndex >= _steps.Count) CompleteOrder();
            else ShowCurrentStep();
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
