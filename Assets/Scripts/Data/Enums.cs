namespace DrinkitGame.Data
{
    /// Категория продукта на складе.
    public enum ProductCategory
    {
        Beans,         // Зерно
        Milk,          // Молоко (любого типа)
        Cream,         // Сливки
        Powder,        // Порошок (матча, какао)
        Syrup,         // Сироп
        Topping,       // Топпинг (корица, какао-посыпка, зефирки)
        Cup            // Стакан "с собой"
    }

    /// Семейство рецепта — определяет схему готовки и какие мини-игры запускаются.
    public enum RecipeFamily
    {
        Espresso,      // эспрессо: помол + экстракция
        Americano,     // эспрессо + горячая вода
        Cappuccino,    // эспрессо + молоко взбитое
        Latte,         // как cappuccino, но другие пропорции
        Raf,           // эспрессо + сливки взбитые
        Filter,        // помол + проливание через V60
        Matcha,        // матча + венчик + (опционально) молоко
        Cacao          // какао + молоко взбитое
    }

    /// Типы мини-игр (используются в Phase 8 для готовки).
    public enum MiniGameType
    {
        None,
        Grinding,      // M1 — помол
        MilkSteaming,  // M2 — вспенивание молока/сливок
        PourOver,      // M3 — проливание (long-tap)
        Whisking       // M4 — взбивание венчиком
    }

    /// Категории модификаторов в заказе (что клиент может попросить сверху).
    public enum ModifierCategory
    {
        MilkType,      // какое молоко (для рецептов с молоком)
        Syrup,         // какой сироп
        Topping,       // какой топпинг
        Container      // тут или с собой
    }

    /// Что выдаёт сектор колеса.
    public enum WheelPrizeType
    {
        Coins,                // деньги
        IngredientPack,       // пачка ингредиента (например молоко x10)
        DiscountVoucher,      // -50% на следующий рецепт
        DoubleNextOrder,      // следующий заказ платит x2
        Nothing,              // "не повезло"
        MilkyWay              // реальная шоколадка Milky Way (физический приз)
    }
}