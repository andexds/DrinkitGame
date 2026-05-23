using System.Text;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Контроллер mock-cooking экрана: показывает детали заказа + кнопка "Выдать".
    /// В Phase 8 будет полноценный пошаговый flow.
    public class CookingScreenController : MonoBehaviour
    {
        [Header("Labels")]
        public TMP_Text recipeLabel;
        public TMP_Text modifiersLabel;
        public TMP_Text patienceLabel;

        [Header("Buttons")]
        public Button serveButton;
        public Button cancelButton;

        private Order _order;

        private void Awake()
        {
            if (serveButton != null) serveButton.onClick.AddListener(OnServe);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        }

        /// Привязать заказ к экрану (вызывается UIRouter при открытии).
        public void Bind(Order order)
        {
            _order = order;
            if (order == null) return;

            if (recipeLabel != null)
                recipeLabel.text = $"{order.recipe.displayName} · {(order.isToGo ? "с собой" : "тут")}";

            if (modifiersLabel != null)
                modifiersLabel.text = BuildModifiersString(order);

            if (patienceLabel != null)
                patienceLabel.text = $"Терпение: {FormatTime(order.remainingPatience)}";
        }

        private void Update()
        {
            // Обновляем таймер терпения, пока экран открыт (заказ ушёл из слота, но мы держим референс)
            if (_order != null && patienceLabel != null)
            {
                _order.remainingPatience -= Time.deltaTime;
                if (_order.remainingPatience < 0) _order.remainingPatience = 0;
                patienceLabel.text = $"Терпение: {FormatTime(_order.remainingPatience)}";
            }
        }

        private void OnServe()
        {
            if (_order == null) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            // Мок-выдача: quality = 100, elapsedSeconds = Patience - remainingPatience
            float elapsed = OrderService.Patience - _order.remainingPatience;
            var resolution = gsm.OrderResolution.Complete(_order, quality: 100f, elapsedSeconds: elapsed);

            UIRouter.Instance.ShowOrderResult(resolution);
            // Сразу возвращаемся в Main (поп-ап рендерится поверх)
            UIRouter.Instance.ShowMain();
            UIRouter.Instance.ShowOrderResult(resolution); // показываем поверх Main
            _order = null;
        }

        private void OnCancel()
        {
            if (_order == null)
            {
                UIRouter.Instance.ShowMain();
                return;
            }
            // Возвращаем заказ обратно в слот: создаём аналогичный заказ в OrderService.
            // Простой путь: дать ему освободить слот (мы уже забрали), и кладём обратно.
            var gsm = GameStateManager.Instance;
            gsm.Orders.ReinsertOrder(_order); // нужно добавить такой метод в OrderService

            UIRouter.Instance.ShowMain();
            _order = null;
        }

        private static string FormatTime(float seconds)
        {
            int t = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{t / 60}:{(t % 60):00}";
        }

        private static string BuildModifiersString(Order order)
        {
            var sb = new StringBuilder();
            if (order.milk != null) sb.Append("на ").Append(order.milk.displayName.ToLower()).Append(" · ");
            if (order.cream != null) sb.Append("со сливками · ");
            if (order.syrup != null) sb.Append(order.syrup.displayName.ToLower()).Append(" · ");
            if (order.topping != null) sb.Append(order.topping.displayName.ToLower()).Append(" · ");
            sb.Append(order.isToGo ? "с собой" : "тут");
            return sb.ToString();
        }
    }
}