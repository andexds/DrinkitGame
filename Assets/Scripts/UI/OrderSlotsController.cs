using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Координирует 3 OrderSlotView и подписывается на события OrderService.
    /// Висит на родительском объекте OrdersSection.
    public class OrderSlotsController : MonoBehaviour
    {
        [Tooltip("Три OrderSlotView в порядке 0, 1, 2.")]
        public OrderSlotView[] slots = new OrderSlotView[3];

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            // Подписки на сервис
            _gsm.Orders.OrderSpawned += OnOrderSpawned;
            _gsm.Orders.OrderRemoved += OnOrderRemoved;
            _gsm.Orders.SlotPatienceTick += OnPatienceTick;

            // Индекс слота назначаем ПО ПОЗИЦИИ В МАССИВЕ — авторитетно, чтобы не зависеть
            // от ручной настройки slotIndex в инспекторе (частый источник багов: все были 0).
            // Здесь же: подписка на тап + изначальный Bind.
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].slotIndex = i;
                slots[i].Tapped += OnSlotTapped;
                slots[i].Bind(_gsm.Orders.GetSlot(i));
            }
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Orders.OrderSpawned -= OnOrderSpawned;
            _gsm.Orders.OrderRemoved -= OnOrderRemoved;
            _gsm.Orders.SlotPatienceTick -= OnPatienceTick;
            foreach (var slot in slots)
                if (slot != null) slot.Tapped -= OnSlotTapped;
        }

        private void OnOrderSpawned(Order order)
        {
            if (order.slotIndex >= 0 && order.slotIndex < slots.Length && slots[order.slotIndex] != null)
                slots[order.slotIndex].Bind(order);
        }

        private void OnOrderRemoved(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null)
                slots[slotIndex].Bind(null);
        }

        private void OnPatienceTick(int slotIndex, float remaining)
        {
            if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null)
                slots[slotIndex].UpdateTimer(remaining);
        }

        private void OnSlotTapped(int slotIndex)
        {
            var order = _gsm.Orders.TakeFromSlot(slotIndex);
            if (order == null) return;

            Debug.Log($"[Order tapped] Открываем cooking: {order.recipe.id}");
            UIRouter.Instance.OpenCooking(order);
        }
    }
}