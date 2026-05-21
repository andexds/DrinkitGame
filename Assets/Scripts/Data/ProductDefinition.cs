using UnityEngine;

namespace DrinkitGame.Data
{
    /// Описание одного продукта (зерно, молоко, сироп и т.д.).
    /// Один ассет = один SKU на складе.
    [CreateAssetMenu(
        fileName = "Product_",
        menuName = "DrinkitGame/Product",
        order = 10)]
    public class ProductDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Уникальный технический ID, например 'beans', 'milk_oat'.")]
        public string id;

        [Tooltip("Отображаемое название для UI.")]
        public string displayName;

        [Tooltip("Иконка в UI инвентаря и заказов. Можно временно null (плейсхолдер).")]
        public Sprite icon;

        [Header("Category")]
        [Tooltip("Категория продукта — для логики генерации заказов и UI.")]
        public ProductCategory category;

        [Tooltip(
            "Только для категории Milk. true означает 'премиум' молоко "
            + "(овсяное/кокосовое/миндальное), за которое клиент платит надбавку.")]
        public bool isPremiumMilk;

        [Header("Economy")]
        [Tooltip("Закупочная цена за 1 единицу (₽).")]
        [Min(0)]
        public int purchasePrice;

        [Tooltip(
            "Надбавка к чеку, если этот продукт используется как модификатор заказа. "
            + "60 для премиум-молока, 40 для сиропов, 30 для топпингов, "
            + "0 для базовых (зерно, коровье молоко, стакан с собой).")]
        [Min(0)]
        public int sellMarkup;
    }
}