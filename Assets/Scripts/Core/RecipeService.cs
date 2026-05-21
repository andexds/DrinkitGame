using System;
using System.Collections.Generic;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Управляет состоянием каталога рецептов: какие открыты, можно ли купить.
    public class RecipeService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;

        /// Стреляет когда новый рецепт был открыт. Параметр — RecipeDefinition.
        public event Action<RecipeDefinition> RecipeUnlocked;

        public RecipeService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
        }

        public bool IsUnlocked(string recipeId) => _state.unlockedRecipeIds.Contains(recipeId);

        /// Можно ли сейчас купить (выполнены все условия + хватает денег)?
        public PurchaseAvailability GetAvailability(RecipeDefinition recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (IsUnlocked(recipe.id)) return PurchaseAvailability.AlreadyOwned;
            if (recipe.requiredMachineTier != null
                && _state.currentMachineTierIndex < recipe.requiredMachineTier.tierIndex)
                return PurchaseAvailability.NeedsHigherMachine;
            if (recipe.unlockQuestTargetRecipe != null
                && _quests.GetSoldCount(recipe.unlockQuestTargetRecipe.id) < recipe.unlockQuestTargetCount)
                return PurchaseAvailability.NeedsMoreSales;
            int price = ApplyDiscountIfAny(recipe.recipePurchasePrice);
            if (_economy.Balance < price) return PurchaseAvailability.NotEnoughMoney;
            return PurchaseAvailability.Available;
        }

        /// Попытка купить рецепт. Возвращает true если успешно (деньги списаны, рецепт открыт).
        public bool TryPurchase(RecipeDefinition recipe)
        {
            if (GetAvailability(recipe) != PurchaseAvailability.Available) return false;
            int price = ApplyDiscountIfAny(recipe.recipePurchasePrice);
            if (!_economy.TrySpend(price)) return false; // защита от race
            if (_state.hasDiscountVoucher) _state.hasDiscountVoucher = false;
            _state.unlockedRecipeIds.Add(recipe.id);
            RecipeUnlocked?.Invoke(recipe);
            return true;
        }

        /// Стартовый набор рецептов: добавляет starterRecipe если ещё не открыт.
        public void EnsureStarterUnlocked()
        {
            if (_content.starterRecipe == null) return;
            if (!IsUnlocked(_content.starterRecipe.id))
                _state.unlockedRecipeIds.Add(_content.starterRecipe.id);
        }

        /// Список всех открытых рецептов как объекты.
        public IEnumerable<RecipeDefinition> EnumerateUnlocked()
        {
            foreach (var r in _content.recipes)
                if (IsUnlocked(r.id)) yield return r;
        }

        private int ApplyDiscountIfAny(int basePrice) =>
            _state.hasDiscountVoucher ? basePrice / 2 : basePrice;
    }

    public enum PurchaseAvailability
    {
        Available,
        AlreadyOwned,
        NeedsHigherMachine,
        NeedsMoreSales,
        NotEnoughMoney
    }
}