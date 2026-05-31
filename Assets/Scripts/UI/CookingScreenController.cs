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