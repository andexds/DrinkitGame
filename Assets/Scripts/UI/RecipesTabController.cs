using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Строит и обновляет список рецептов.
    public class RecipesTabController : MonoBehaviour
    {
        [Tooltip("Содержимое ScrollView (RecipesScroll/Viewport/Content)")]
        public Transform listRoot;
        [Tooltip("Prefab строки рецепта")]
        public RecipeRow recipeRowPrefab;

        private readonly List<RecipeRow> _rows = new();
        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            Subscribe();
            Rebuild();
        }

        private void OnDisable()
        {
            Unsubscribe();
            Clear();
        }

        private void Subscribe()
        {
            _gsm.Economy.BalanceChanged += OnAnyChange;
            _gsm.Quests.CountChanged += OnQuestsChanged;
            _gsm.Recipes.RecipeUnlocked += OnRecipeUnlocked;
            _gsm.Machine.Upgraded += OnMachineUpgraded;
        }

        private void Unsubscribe()
        {
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged -= OnAnyChange;
            _gsm.Quests.CountChanged -= OnQuestsChanged;
            _gsm.Recipes.RecipeUnlocked -= OnRecipeUnlocked;
            _gsm.Machine.Upgraded -= OnMachineUpgraded;
        }

        private void OnAnyChange(int _) => RefreshRows();
        private void OnQuestsChanged(string _, int __) => RefreshRows();
        private void OnRecipeUnlocked(RecipeDefinition _) => RefreshRows();
        private void OnMachineUpgraded(MachineTierDefinition _) => RefreshRows();

        private void Rebuild()
        {
            Clear();
            foreach (var recipe in _gsm.GameContent_Recipes())
            {
                var row = Instantiate(recipeRowPrefab, listRoot);
                row.Bind(recipe, _gsm.Recipes, _gsm.Economy);
                row.OnBuyClicked += OnBuyClicked;
                _rows.Add(row);
            }
        }

        private void RefreshRows()
        {
            int i = 0;
            foreach (var recipe in _gsm.GameContent_Recipes())
            {
                if (i >= _rows.Count) break;
                _rows[i].Bind(recipe, _gsm.Recipes, _gsm.Economy);
                i++;
            }
        }

        private void OnBuyClicked(RecipeDefinition recipe)
        {
            if (recipe == null) return;
            bool ok = _gsm.Recipes.TryPurchase(recipe);
            Debug.Log($"[Store] Купить {recipe.id}: {(ok ? "успех" : "неудача")}");
            RefreshRows();
        }

        private void Clear()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
    }
}