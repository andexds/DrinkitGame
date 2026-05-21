using UnityEngine;

namespace DrinkitGame.Data
{
    /// Один сектор колеса удачи.
    [CreateAssetMenu(
        fileName = "Wheel_",
        menuName = "DrinkitGame/Wheel Sector",
        order = 40)]
    public class WheelSectorDefinition : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Короткое описание приза для UI: '50 ₽', 'Молоко x10', и т.д.")]
        public string displayLabel;

        [Tooltip("Иконка приза для UI колеса (может быть null временно).")]
        public Sprite icon;

        [Header("Probability")]
        [Tooltip("Шанс выпадения в процентах (0..100). Сумма всех секторов должна быть = 100.")]
        [Range(0, 100)]
        public int probabilityPercent;

        [Header("Prize")]
        [Tooltip("Что выдаёт этот сектор.")]
        public WheelPrizeType prizeType;

        [Tooltip("Количество монет (только для Coins).")]
        [Min(0)]
        public int coinsAmount;

        [Tooltip("Какой продукт выдать (только для IngredientPack).")]
        public ProductDefinition packProduct;

        [Tooltip("Сколько единиц этого продукта (только для IngredientPack).")]
        [Min(0)]
        public int packQuantity;
    }
}