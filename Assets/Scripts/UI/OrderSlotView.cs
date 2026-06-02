using System.Text;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// UI одного слота заказа. Показывает напиток + модификаторы + таймер.
    /// Button делает Click → событие, которое ловит OrderSlotsController.
    public class OrderSlotView : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Индекс слота 0..2")]
        public int slotIndex;

        [Header("UI parts")]
        public GameObject contentRoot;       // показываем когда заказ есть
        public GameObject emptyRoot;         // показываем когда заказ пустой
        public TMP_Text recipeNameLabel;
        public TMP_Text modifiersLabel;
        public TMP_Text timerLabel;
        public Button clickButton;

        /// Событие — игрок тапнул по слоту. Передаётся индекс.
        public event System.Action<int> Tapped;

        private Order _current;

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(() =>
                {
                    if (_current != null) Tapped?.Invoke(slotIndex);
                });
        }

        /// Поставить заказ в слот (заменяет существующий) или null чтобы очистить.
        public void Bind(Order order)
        {
            _current = order;
            if (contentRoot != null) contentRoot.SetActive(order != null);
            if (emptyRoot != null) emptyRoot.SetActive(order == null);
            if (order == null) return;

            if (recipeNameLabel != null)
                recipeNameLabel.text = order.recipe.displayName;
            if (modifiersLabel != null)
                modifiersLabel.text = BuildModifiersString(order);
            UpdateTimer(order.remainingPatience);
        }

        /// Обновить только таймер (вызывается из контроллера на тик).
        public void UpdateTimer(float remainingSeconds)
        {
            if (timerLabel == null) return;
            int t = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            int m = t / 60;
            int s = t % 60;
            timerLabel.text = $"{m}:{s:00}";
        }

        private static string BuildModifiersString(Order order)
        {
            var sb = new StringBuilder();
            if (order.milk != null) sb.Append(order.milk.displayName).Append(" · ");
            if (order.cream != null) sb.Append("сливки · ");
            if (order.syrup != null) sb.Append("сироп · ");
            if (order.topping != null) sb.Append(order.topping.displayName).Append(" · ");
            sb.Append(order.isToGo ? "с собой" : "в кафе");
            return sb.ToString();
        }
    }
}