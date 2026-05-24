using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    public class IngredientRow : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text stockLabel;
        public TMP_Text priceLabel;
        public Button plus1Button;
        public Button plus10Button;
        public Button plus50Button;

        public System.Action<ProductDefinition, int> OnBuyClicked;

        private ProductDefinition _product;

        private void Awake()
        {
            if (plus1Button != null) plus1Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 1));
            if (plus10Button != null) plus10Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 10));
            if (plus50Button != null) plus50Button.onClick.AddListener(() => OnBuyClicked?.Invoke(_product, 50));
        }

        public void Bind(ProductDefinition product, InventoryService inventory, EconomyService economy)
        {
            _product = product;
            if (nameLabel != null) nameLabel.text = product.displayName;
            if (icon != null && product.icon != null) icon.sprite = product.icon;
            if (stockLabel != null) stockLabel.text = $"× {inventory.GetStock(product.id)}";
            if (priceLabel != null) priceLabel.text = $"{product.purchasePrice} ₽";

            // Активность кнопок — по бюджету
            if (plus1Button != null) plus1Button.interactable = economy.Balance >= product.purchasePrice;
            if (plus10Button != null) plus10Button.interactable = economy.Balance >= product.purchasePrice * 10;
            // +50 со скидкой 5%
            if (plus50Button != null) plus50Button.interactable = economy.Balance >= (int)(product.purchasePrice * 50 * 0.95f);
        }
    }
}