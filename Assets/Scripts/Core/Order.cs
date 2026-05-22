using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Одна единица заказа клиента: рецепт + конкретные модификаторы + таймер терпения.
    /// Создаётся OrderGenerator'ом, лежит в OrderService слоте, отображается OrderSlotView.
    [Serializable]
    public class Order
    {
        public string id;                          // GUID для отладки/логов
        public RecipeDefinition recipe;            // что готовим
        public ProductDefinition milk;             // null если рецепт не требует молока
        public ProductDefinition cream;            // null если не раф
        public ProductDefinition syrup;            // null если нет сиропа
        public ProductDefinition topping;          // null если нет топпинга
        public bool isToGo;                        // тут (false) или с собой (true)
        public float remainingPatience;            // сек, тикает вниз; 300 при спавне
        public int slotIndex;                      // 0, 1 или 2

        public Order()
        {
            id = Guid.NewGuid().ToString();
        }
    }

    /// Состояние слота заказа.
    public enum OrderState
    {
        Empty,
        Waiting,      // ждёт игрока
        InProgress,   // игрок начал готовить (Phase 6)
    }
}