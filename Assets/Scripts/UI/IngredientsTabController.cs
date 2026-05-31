using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.UI
{
    public class IngredientsTabController : MonoBehaviour
    {
        public Transform listRoot;
        public IngredientRow rowPrefab;

        private readonly List<IngredientRow> _rows = new();
        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged += _ => RefreshAll();
            _gsm.Inventory.StockChanged += (_, __) => RefreshAll();
            _gsm.Recipes.RecipeUnlocked += _ => Rebuild(); // открытие нового рецепта может добавить новые продукты в магазин
            Rebuild();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Rebuild()
        {
            Clear();
            foreach (var product in _gsm.GameContent_Products())
            {
                if (!IsRelevant(product)) continue;
                var row = Instantiate(rowPrefab, listRoot);
                row.Bind(product, _gsm.Inventory, _gsm.Economy);
                row.OnBuyClicked += OnBuyClicked;
                _rows.Add(row);
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                // Сохраняем тот же продукт, но обновляем bind
                // (мы хранили продукт в самом row через _product, но не имеем доступа извне —
                //  rebuild через продукт по индексу)
            }
            // Простой подход: пересобрать всё.
            Rebuild();
        }

        private void OnBuyClicked(ProductDefinition product, int amount)
        {
            int unit = product.purchasePrice;
            int totalPrice = amount == 50 ? (int)(unit * 50 * 0.95f) : unit * amount;

            if (_gsm.Economy.TrySpend(totalPrice))
            {
                _gsm.Inventory.Add(product.id, amount);
                Debug.Log($"[Store] Купили {amount}x {product.id} за {totalPrice} ₽");
            }
            else
            {
                Debug.Log($"[Store] Не хватает на {amount}x {product.id}");
            }
        }

        private bool IsRelevant(ProductDefinition product)
        {
            // Показываем только те продукты, которые могут быть использованы в открытых рецептах
            foreach (var recipeId in _gsm.State.unlockedRecipeIds)
            {
                foreach (var recipe in _gsm.GameContent_Recipes())
                {
                    if (recipe.id != recipeId) continue;
                    if (IsProductUsedInRecipe(product, recipe)) return true;
                }
            }
            return false;
        }

        private bool IsProductUsedInRecipe(ProductDefinition product, RecipeDefinition recipe)
        {
            foreach (var ing in recipe.fixedIngredients)
                if (ing.product == product) return true;
            if (product.category == ProductCategory.Milk && recipe.needsMilk) return true;
            if (product.category == ProductCategory.Cream && recipe.needsCream) return true;
            if (product.category == ProductCategory.Syrup && recipe.canHaveSyrup) return true;
            if (product.category == ProductCategory.Topping
                && recipe.compatibleToppings != null
                && recipe.compatibleToppings.Contains(product)) return true;
            if (product.category == ProductCategory.Cup && recipe.canBeToGo) return true;
            return false;
        }

        private void Clear()
        {
            foreach (var row in _rows) if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
    }
}