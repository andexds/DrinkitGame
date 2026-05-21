using System;

namespace DrinkitGame.Core
{
    /// Управляет остатками продуктов на складе.
    /// Хранение в виде List<InventorySlot>; поиск по productId — линейный (15 продуктов = норм).
    public class InventoryService
    {
        private readonly GameState _state;

        /// Стреляет после любого изменения остатка. Параметры: productId, новый остаток.
        public event Action<string, int> StockChanged;

        public InventoryService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// Текущий остаток продукта (0 если не в инвентаре).
        public int GetStock(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("productId is empty", nameof(productId));
            foreach (var slot in _state.inventory)
                if (slot.productId == productId) return slot.count;
            return 0;
        }

        /// Прибавить N единиц продукта.
        public void Add(string productId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            var slot = FindOrCreateSlot(productId);
            slot.count += amount;
            StockChanged?.Invoke(productId, slot.count);
        }

        /// Попытка списать N единиц. true если хватило.
        public bool TryConsume(string productId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            var slot = FindSlot(productId);
            if (slot == null || slot.count < amount) return false;
            slot.count -= amount;
            StockChanged?.Invoke(productId, slot.count);
            return true;
        }

        /// Достаточно ли единиц на складе для конкретной операции.
        public bool HasEnough(string productId, int amount)
        {
            return GetStock(productId) >= amount;
        }

        private InventorySlot FindSlot(string productId)
        {
            foreach (var slot in _state.inventory)
                if (slot.productId == productId) return slot;
            return null;
        }

        private InventorySlot FindOrCreateSlot(string productId)
        {
            var slot = FindSlot(productId);
            if (slot != null) return slot;
            slot = new InventorySlot(productId, 0);
            _state.inventory.Add(slot);
            return slot;
        }
    }
}