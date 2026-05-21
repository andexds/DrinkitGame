using System;
using UnityEngine;

namespace DrinkitGame.Data
{
    /// Один уровень кофемашины (T1/T2/T3).
    [CreateAssetMenu(
        fileName = "Machine_",
        menuName = "DrinkitGame/Machine Tier",
        order = 20)]
    public class MachineTierDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Индекс тира: 1, 2 или 3.")]
        [Min(1)]
        public int tierIndex;

        [Tooltip("Отображаемое название: 'Старенькая', 'Бариста', 'Профи'.")]
        public string displayName;

        [Tooltip("Спрайт кофемашины в UI. Можно временно null.")]
        public Sprite icon;

        [Header("Purchase")]
        [Tooltip("Стоимость покупки этого уровня (₽). 0 для стартового T1.")]
        [Min(0)]
        public int purchasePrice;

        [Tooltip(
            "Описание квеста для UI (например, 'Продай 10 американо'). "
            + "Пусто для стартового T1.")]
        public string questDescription;

        [Tooltip(
            "Какой рецепт нужно продавать для квеста на покупку этого тира. "
            + "null для T1.")]
        public RecipeDefinition questTargetRecipe1;

        [Tooltip("Сколько нужно продать quest target 1.")]
        [Min(0)]
        public int questTargetCount1;

        [Tooltip("Второй рецепт квеста (например 'продай 5 капучино + 5 латте'). null если не нужно.")]
        public RecipeDefinition questTargetRecipe2;

        [Tooltip("Сколько нужно продать quest target 2.")]
        [Min(0)]
        public int questTargetCount2;

        [Header("Gameplay parameters")]
        [Tooltip(
            "Ширина зелёной зоны для мини-игры помола (0..1, доля от полосы). "
            + "T1: узкая (0.15), T2: средняя (0.25), T3: широкая (0.4).")]
        [Range(0.05f, 0.5f)]
        public float grindingZoneWidth = 0.25f;

        [Tooltip("Ширина зелёной зоны для вспенивания молока (0..1).")]
        [Range(0.05f, 0.5f)]
        public float milkSteamingZoneWidth = 0.25f;

        [Tooltip("Ширина зелёной зоны для проливания фильтра (0..1).")]
        [Range(0.05f, 0.5f)]
        public float pourOverZoneWidth = 0.25f;

        [Tooltip(
            "Длительность авто-экстракции эспрессо, сек. "
            + "T1: 3.0, T2: 2.0, T3: 1.0.")]
        [Range(0.5f, 5f)]
        public float extractionTimeSeconds = 3f;

        [Tooltip(
            "Премиум-бонус: +N% к финальному чеку всех напитков. "
            + "T1: 0, T2: 0, T3: 10.")]
        [Range(0, 50)]
        public int checkBonusPercent;
    }
}