using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Показывает сводку склада рядом с кофемашиной: «Зерно», «Молоко», «Добавки» и т.п.
    /// Каждая секция настраивается в инспекторе: какие категории продуктов суммируем,
    /// какой порог считаем «мало» и какие цвета использовать для подсветки.
    /// Подписывается на InventoryService.StockChanged и обновляется автоматически.
    public class InventorySummaryController : MonoBehaviour
    {
        /// Одна секция сводки (например, «Зерно»).
        [System.Serializable]
        public class Section
        {
            [Tooltip("Только для удобства в инспекторе — игре не виден.")]
            public string sectionName = "Section";

            [Tooltip("Лейбл, в который пишем число (например, '12').")]
            public TMP_Text countLabel;

            [Tooltip("Опциональный лейбл-подпись (например, 'Зерно'). " +
                     "Не обязателен — обычно текст подписи статичный в Hierarchy.")]
            public TMP_Text titleLabel;

            [Tooltip("Опциональный фон секции — подкрашиваем при нехватке.")]
            public Image background;

            [Tooltip("Опциональная иконка — подкрашиваем при нехватке.")]
            public Image icon;

            [Tooltip("Какие категории продуктов суммируем в эту секцию. " +
                     "Зерно: [Beans]. Молоко: [Milk]. Добавки: [Cream, Syrup, Topping, Powder].")]
            public List<ProductCategory> categories = new();

            [Tooltip("Если сумма ≤ этого числа — секция подсвечивается как «мало».")]
            public int lowThreshold = 3;

            [Tooltip("Цвет в нормальном состоянии (countLabel + icon + background при наличии).")]
            public Color normalColor = Color.white;

            [Tooltip("Цвет при нехватке. Стандартно — мягкий красный.")]
            public Color lowColor = new Color(0.92f, 0.35f, 0.30f);

            [Tooltip("Опционально красить фон тоже. Если выключено — только текст и иконка.")]
            public bool tintBackgroundOnLow = false;
        }

        [Header("Секции")]
        public List<Section> sections = new();

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Inventory.StockChanged += OnStockChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Inventory.StockChanged -= OnStockChanged;
        }

        private void OnStockChanged(string productId, int newCount) => Refresh();

        private void Refresh()
        {
            if (_gsm == null || _gsm.content == null) return;

            foreach (var section in sections)
            {
                if (section == null) continue;

                int total = ComputeTotal(section.categories);
                if (section.countLabel != null) section.countLabel.text = total.ToString();

                bool low = total <= section.lowThreshold;
                Color tint = low ? section.lowColor : section.normalColor;

                if (section.countLabel != null) section.countLabel.color = tint;
                if (section.titleLabel != null) section.titleLabel.color = tint;
                if (section.icon != null) section.icon.color = tint;
                if (section.background != null && section.tintBackgroundOnLow)
                    section.background.color = tint;
            }
        }

        private int ComputeTotal(List<ProductCategory> categories)
        {
            if (categories == null || categories.Count == 0 || _gsm.content == null) return 0;
            int total = 0;
            foreach (var product in _gsm.content.products)
            {
                if (product == null) continue;
                if (!categories.Contains(product.category)) continue;
                total += _gsm.Inventory.GetStock(product.id);
            }
            return total;
        }
    }
}
