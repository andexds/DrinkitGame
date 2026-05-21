using System;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Пара "продукт + количество". Используется в фиксированных ингредиентах
    /// рецепта и в призах колеса.
    [Serializable]
    public struct IngredientAmount
    {
        [Tooltip("ScriptableObject продукта.")]
        public ProductDefinition product;

        [Tooltip("Количество единиц этого продукта.")]
        [Min(1)]
        public int amount;

        public IngredientAmount(ProductDefinition product, int amount)
        {
            this.product = product;
            this.amount = amount;
        }
    }
}