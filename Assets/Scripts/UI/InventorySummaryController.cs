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

        [Header("Клик → магазин")]
        [Tooltip("Опционально: Button, при тапе на который открывается магазин " +
                 "на вкладке «Ингредиенты». Обычно вешается на корневой объект всей сводки.")]
        public Button openStoreButton;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Inventory.StockChanged += OnStockChanged;
            if (openStoreButton != null)
                openStoreButton.onClick.AddListener(OnOpenStoreClicked);
            Refresh();
        }

        private void OnOpenStoreClicked()
        {
            if (UIRouter.Instance != null)
                UIRouter.Instance.OpenStoreOnTab(StoreTab.Ingredients);
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Inventory.StockChanged -= OnStockChanged;
            if (openStoreButton != null)
                openStoreButton.onClick.RemoveListener(OnOpenStoreClicked);
        }

        private void OnStockChanged(string productId, int newCount) => Refresh();

#if UNITY_EDITOR
        /// Unity не применяет C#-инициализаторы к элементам List<>, добавленным через
        /// инспектор кнопкой «+» — все Color-поля заполняются (0,0,0,0). Тут чиним альфу,
        /// чтобы новые секции были сразу видимыми. Если ты СОЗНАТЕЛЬНО хочешь прозрачный
        /// цвет — переопредели в инспекторе (тогда α уже не равно 0 и мы ничего не трогаем).
        private void OnValidate()
        {
            if (sections == null) return;
            var defaultLow = new Color(0.92f, 0.35f, 0.30f, 1f);
            foreach (var s in sections)
            {
                if (s == null) continue;
                if (s.normalColor.a == 0f) s.normalColor = Color.white;
                if (s.lowColor.a == 0f) s.lowColor = defaultLow;
            }
        }
#endif

        private void Refresh()
        {
            if (_gsm == null || _gsm.content == null) return;

            foreach (var section in sections)
            {
                if (section == null) continue;

                int total = ComputeTotal(section.categories);
                if (section.countLabel != null) section.countLabel.text = total.ToString();

                bool low = total <= section.lowThreshold;
                Color lowTint = low ? section.lowColor : section.normalColor;

                // Текст НЕ красим — всегда в normalColor (белый). Запрос игрока:
                // «закончились ингредиенты — не красим в красный, оставляем белым».
                if (section.countLabel != null) section.countLabel.color = section.normalColor;
                if (section.titleLabel != null) section.titleLabel.color = section.normalColor;

                // Иконка/фон по-прежнему меняются — даёт визуальный hint без агрессивности.
                if (section.icon != null) section.icon.color = lowTint;
                if (section.background != null && section.tintBackgroundOnLow)
                    section.background.color = lowTint;
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
