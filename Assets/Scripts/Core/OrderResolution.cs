using System;

namespace DrinkitGame.Core
{
    /// Результат выдачи заказа: что заплатили и почему.
    /// Используется OrderResultPopup для визуализации.
    [Serializable]
    public class OrderResolution
    {
        public string recipeId;
        public string recipeDisplayName;
        public int basePrice;              // База: цена напитка + надбавки модификаторов
        public float speedMultiplier;      // +0.3 / 0 / -0.1
        public float qualityMultiplier;    // +0.2 / 0 / -0.1
        public float tierBonusMultiplier;  // 0 или 0.1 (только T3)
        public bool doubleApplied;         // был ли использован буст "следующий заказ ×2"
        public int finalPayout;            // итоговая выплата

        // Категория скорости и качества — для UI текста
        public string speedLabel;          // "быстро" / "норм" / "медленно"
        public string qualityLabel;        // "отлично" / "норм" / "плохо"
    }
}