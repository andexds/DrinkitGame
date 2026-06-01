using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Одна строка в списке рецептов Магазина.
    public class RecipeRow : MonoBehaviour
    {
        public Image background;
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text statusLabel;
        public TMP_Text priceLabel;
        public Button buyButton;
        public TMP_Text buyButtonLabel;

        public System.Action<RecipeDefinition> OnBuyClicked;

        private RecipeDefinition _recipe;

        private void Awake()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_recipe));
        }

        public void Bind(RecipeDefinition recipe, RecipeService recipes, EconomyService economy)
        {
            _recipe = recipe;
            if (nameLabel != null) nameLabel.text = recipe.displayName;
            if (icon != null && recipe.icon != null) icon.sprite = recipe.icon;

            var availability = recipes.GetAvailability(recipe);
            string status;
            string buyText = "Купить";
            bool buyActive = false;

            switch (availability)
            {
                case PurchaseAvailability.AlreadyOwned:
                    status = "Открыт";
                    buyText = "—";
                    break;
                case PurchaseAvailability.NeedsHigherMachine:
                    status = $"Нужна машина T{recipe.requiredMachineTier.tierIndex}";
                    buyText = "Закрыто";
                    break;
                case PurchaseAvailability.NeedsMoreSales:
                    status = string.IsNullOrEmpty(recipe.unlockQuestDescription)
                        ? "Выполни условие"
                        : recipe.unlockQuestDescription;
                    buyText = "Закрыто";
                    break;
                case PurchaseAvailability.NotEnoughMoney:
                    status = "Не хватает денег";
                    buyText = "Купить";
                    break;
                default: // Available
                    status = "Можно купить";
                    buyText = "Купить";
                    buyActive = true;
                    break;
            }

            if (statusLabel != null) statusLabel.text = status;
            if (priceLabel != null)
                priceLabel.text = recipe.recipePurchasePrice > 0 ? $"{recipe.recipePurchasePrice} ₽" : "";
            if (buyButtonLabel != null) buyButtonLabel.text = buyText;
            if (buyButton != null) buyButton.interactable = buyActive;
        }
    }
}