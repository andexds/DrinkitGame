using System;
using System.Collections.Generic;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Чистая логика генерации одного заказа: выбор рецепта по весам + модификаторы из доступных по складу.
    public class OrderGenerator
    {
        // Шансы накатить модификатор (если применим)
        private const double SyrupChance = 0.4;
        private const double ToppingChance = 0.3;
        private const double ToGoChance = 0.5;

        // Веса для приоритета "новых" рецептов в выдаче
        private const int WeightNewest = 4;
        private const int WeightSecondNewest = 2;
        private const int WeightOther = 1;

        // Сколько раз повторять попытку, если выбранный рецепт нельзя выполнить (нет ингредиентов)
        private const int MaxRetries = 12;

        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly InventoryService _inventory;
        private readonly Random _rng;

        public OrderGenerator(
            GameState state,
            GameContent content,
            InventoryService inventory,
            Random rng = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _rng = rng ?? new Random();
        }

        /// Сгенерировать заказ для указанного слота. Возвращает null если ничего нельзя приготовить
        /// (нет ингредиентов для всех открытых рецептов).
        public Order Generate(int slotIndex)
        {
            var unlockedRecipes = CollectUnlockedRecipes();
            if (unlockedRecipes.Count == 0) return null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                var recipe = PickRecipeByWeight(unlockedRecipes);
                var order = TryBuildOrder(recipe, slotIndex);
                if (order != null) return order;
            }
            return null;
        }

        private List<RecipeDefinition> CollectUnlockedRecipes()
        {
            var list = new List<RecipeDefinition>();
            foreach (var recipe in _content.recipes)
                if (_state.unlockedRecipeIds.Contains(recipe.id))
                    list.Add(recipe);
            return list;
        }

        private RecipeDefinition PickRecipeByWeight(List<RecipeDefinition> recipes)
        {
            // Считаем веса: последний открытый = 4, предпоследний = 2, остальные = 1
            int lastIndex = -1, prevIndex = -1;
            for (int i = _state.unlockedRecipeIds.Count - 1; i >= 0 && (lastIndex < 0 || prevIndex < 0); i--)
            {
                var id = _state.unlockedRecipeIds[i];
                for (int j = 0; j < recipes.Count; j++)
                {
                    if (recipes[j].id == id)
                    {
                        if (lastIndex < 0) lastIndex = j;
                        else if (prevIndex < 0) { prevIndex = j; }
                        break;
                    }
                }
            }

            int totalWeight = 0;
            var weights = new int[recipes.Count];
            for (int i = 0; i < recipes.Count; i++)
            {
                weights[i] = i == lastIndex ? WeightNewest
                           : i == prevIndex ? WeightSecondNewest
                           : WeightOther;
                totalWeight += weights[i];
            }

            int pick = _rng.Next(totalWeight);
            for (int i = 0; i < recipes.Count; i++)
            {
                if (pick < weights[i]) return recipes[i];
                pick -= weights[i];
            }
            return recipes[recipes.Count - 1]; // fallback (не должен срабатывать)
        }

        private Order TryBuildOrder(RecipeDefinition recipe, int slotIndex)
        {
            // 1. Фиксированные ингредиенты — должны быть в стоке
            foreach (var ing in recipe.fixedIngredients)
            {
                if (ing.product == null) continue;
                if (!_inventory.HasEnough(ing.product.id, ing.amount)) return null;
            }

            var order = new Order { recipe = recipe, slotIndex = slotIndex };

            // 2. Молоко — обязательное если needsMilk
            if (recipe.needsMilk)
            {
                var milkOptions = CollectInStock(ProductCategory.Milk);
                if (milkOptions.Count == 0) return null;
                order.milk = milkOptions[_rng.Next(milkOptions.Count)];
            }

            // 3. Сливки — обязательное для рафа
            if (recipe.needsCream)
            {
                var creamOptions = CollectInStock(ProductCategory.Cream);
                if (creamOptions.Count == 0) return null;
                order.cream = creamOptions[_rng.Next(creamOptions.Count)];
            }

            // 4. Сироп — опционально, 40% шанс если применим
            if (recipe.canHaveSyrup && _rng.NextDouble() < SyrupChance)
            {
                var syrupOptions = CollectInStock(ProductCategory.Syrup);
                if (syrupOptions.Count > 0)
                    order.syrup = syrupOptions[_rng.Next(syrupOptions.Count)];
            }

            // 5. Топпинг — опционально, 30% шанс из compatibleToppings
            if (recipe.compatibleToppings != null && recipe.compatibleToppings.Count > 0
                && _rng.NextDouble() < ToppingChance)
            {
                var toppingOptions = new List<ProductDefinition>();
                foreach (var t in recipe.compatibleToppings)
                    if (t != null && _inventory.HasEnough(t.id, 1))
                        toppingOptions.Add(t);
                if (toppingOptions.Count > 0)
                    order.topping = toppingOptions[_rng.Next(toppingOptions.Count)];
            }

            // 6. Тара — 50% to-go если рецепт это разрешает И есть стаканы
            if (recipe.canBeToGo && _rng.NextDouble() < ToGoChance)
            {
                if (HasTakeawayCupInStock()) order.isToGo = true;
            }

            return order;
        }

        private List<ProductDefinition> CollectInStock(ProductCategory category)
        {
            var result = new List<ProductDefinition>();
            foreach (var p in _content.products)
                if (p.category == category && _inventory.HasEnough(p.id, 1))
                    result.Add(p);
            return result;
        }

        private bool HasTakeawayCupInStock()
        {
            foreach (var p in _content.products)
                if (p.category == ProductCategory.Cup && _inventory.HasEnough(p.id, 1))
                    return true;
            return false;
        }
    }
}