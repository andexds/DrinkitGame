using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Описание одного рецепта (один напиток в каталоге).
    [CreateAssetMenu(
        fileName = "Recipe_",
        menuName = "DrinkitGame/Recipe",
        order = 30)]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Уникальный технический ID, например 'espresso', 'cappuccino'.")]
        public string id;

        [Tooltip("Отображаемое название для UI.")]
        public string displayName;

        [Tooltip("Иконка напитка для UI заказа и каталога.")]
        public Sprite icon;

        [Header("Recipe family")]
        [Tooltip("Семейство — определяет схему готовки в Phase 8.")]
        public RecipeFamily family;

        [Header("Economy")]
        [Tooltip("Базовая цена продажи (₽), без модификаторов и бонусов.")]
        [Min(0)]
        public int basePrice;

        [Tooltip("Стоимость покупки самого рецепта в Магазине (₽). 0 для стартового эспрессо.")]
        [Min(0)]
        public int recipePurchasePrice;

        [Header("Machine gating")]
        [Tooltip("Минимальный тир кофемашины, который нужен для приготовления.")]
        public MachineTierDefinition requiredMachineTier;

        [Header("Ingredients")]
        [Tooltip(
            "Фиксированные ингредиенты — продукты, которые всегда нужны "
            + "(кроме категории, выбираемой заказом). "
            + "Например для эспрессо: [Beans x1]. Для матчи: [Matcha x1]. "
            + "Для капучино: [Beans x1] — молоко выбирается заказом.")]
        public List<IngredientAmount> fixedIngredients = new();

        [Tooltip(
            "Нужно ли молоко (любого типа). Если true — у заказа всегда "
            + "будет указан тип молока (коровье/овсяное/кокос/миндаль).")]
        public bool needsMilk;

        [Tooltip("Нужны ли сливки (только raf).")]
        public bool needsCream;

        [Header("Optional modifiers")]
        [Tooltip("Может ли заказ просить сироп.")]
        public bool canHaveSyrup;

        [Tooltip("Какие топпинги допустимы для этого рецепта (если есть).")]
        public List<ProductDefinition> compatibleToppings = new();

        [Tooltip("Может ли быть 'с собой' (бумажный стакан).")]
        public bool canBeToGo = true;

        [Header("Unlock condition")]
        [Tooltip(
            "Какой рецепт нужно продавать для разблокировки этого. "
            + "null = нет квеста, доступен сразу при наличии денег и машины.")]
        public RecipeDefinition unlockQuestTargetRecipe;

        [Tooltip("Сколько нужно продать для разблокировки.")]
        [Min(0)]
        public int unlockQuestTargetCount;

        [Tooltip("Описание квеста для UI.")]
        public string unlockQuestDescription;
    }
}