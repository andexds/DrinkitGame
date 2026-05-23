using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Управляет 3 слотами заказов: спавн через 5–15 сек когда свободен слот, тик терпения, уход клиентов.
    public class OrderService
    {
        public const int SlotCount = 3;
        public const float SpawnDelayMin = 5f;
        public const float SpawnDelayMax = 15f;
        public const float Patience = 300f;        // 5 минут
        public const float ReputationLossOnAbandon = 0.1f;

        private readonly Order[] _slots = new Order[SlotCount];
        private readonly OrderGenerator _generator;
        private readonly ReputationService _reputation;
        private readonly Random _rng;

        private float _spawnTimer;
        private bool _spawnTimerActive;

        public event Action<Order> OrderSpawned;
        public event Action<Order> OrderAbandoned;
        public event Action<int> OrderRemoved;    // slot освобождён по любой причине
        public event Action<int, float> SlotPatienceTick;

        public OrderService(
            OrderGenerator generator,
            ReputationService reputation,
            Random rng = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
            _rng = rng ?? new System.Random();
        }

        public Order GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return null;
            return _slots[index];
        }

        /// Снять заказ со слота (например когда игрок начал готовить или мы отменили).
        public Order TakeFromSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return null;
            var order = _slots[index];
            if (order != null)
            {
                _slots[index] = null;
                OrderRemoved?.Invoke(index);
            }
            return order;
        }

        /// Tick — вызывается каждый кадр из MonoBehaviour-обёртки.
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0) return;

            // 1. Тик терпения для всех непустых слотов
            for (int i = 0; i < SlotCount; i++)
            {
                var order = _slots[i];
                if (order == null) continue;
                order.remainingPatience -= deltaTime;
                if (order.remainingPatience <= 0)
                {
                    _slots[i] = null;
                    _reputation.Adjust(-ReputationLossOnAbandon);
                    OrderAbandoned?.Invoke(order);
                    OrderRemoved?.Invoke(i);
                }
                else
                {
                    SlotPatienceTick?.Invoke(i, order.remainingPatience);
                }
            }

            // 2. Спавн нового, если есть свободный слот
            int freeIndex = FindFreeSlot();
            if (freeIndex < 0)
            {
                _spawnTimerActive = false;
                return;
            }

            if (!_spawnTimerActive)
            {
                _spawnTimer = NextSpawnDelay();
                _spawnTimerActive = true;
                return;
            }

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0) return;

            var newOrder = _generator.Generate(freeIndex);
            if (newOrder != null)
            {
                newOrder.remainingPatience = Patience;
                _slots[freeIndex] = newOrder;
                OrderSpawned?.Invoke(newOrder);
            }

            // Сбрасываем таймер вне зависимости от того, удалось ли сгенерить.
            _spawnTimer = NextSpawnDelay();
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < SlotCount; i++)
                if (_slots[i] == null) return i;
            return -1;
        }

        private float NextSpawnDelay()
        {
            return (float)(_rng.NextDouble() * (SpawnDelayMax - SpawnDelayMin) + SpawnDelayMin);
        }

        /// Положить заказ обратно в слот (например, если игрок отменил готовку).
        /// Возвращает true если получилось (слот свободен).
        public bool ReinsertOrder(Order order)
        {
            if (order == null) return false;
            if (order.slotIndex < 0 || order.slotIndex >= SlotCount) return false;
            if (_slots[order.slotIndex] != null) return false;
            _slots[order.slotIndex] = order;
            OrderSpawned?.Invoke(order);
            return true;
        }
    }
}