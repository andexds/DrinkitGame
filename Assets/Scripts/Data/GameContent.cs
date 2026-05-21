using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Корневой ассет со ссылками на весь игровой контент.
    /// Создаётся ОДИН ассет на проект; передаётся сервисам через инспектор.
    [CreateAssetMenu(
        fileName = "GameContent",
        menuName = "DrinkitGame/Game Content (root)",
        order = 0)]
    public class GameContent : ScriptableObject
    {
        [Header("Products (15)")]
        public List<ProductDefinition> products = new();

        [Header("Machine tiers (3)")]
        public List<MachineTierDefinition> machineTiers = new();

        [Header("Recipes (8)")]
        public List<RecipeDefinition> recipes = new();

        [Header("Wheel sectors (9)")]
        public List<WheelSectorDefinition> wheelSectors = new();

        [Header("Starter setup")]
        [Tooltip("Рецепт, открытый с самого начала игры (эспрессо).")]
        public RecipeDefinition starterRecipe;

        [Tooltip("Стартовый тир кофемашины (T1).")]
        public MachineTierDefinition starterMachineTier;

        [Tooltip("Стартовый баланс игрока в рублях.")]
        public int starterBalance = 0;

        [Tooltip(
            "Стартовый запас зерна (чтоб игрок мог сделать первые эспрессо). "
            + "0 = игрок должен купить с самого начала; обычно 5-10 для онбординга.")]
        public int starterBeansStock = 10;
    }
}