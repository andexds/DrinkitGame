using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Управляет 3 слотами заказов: спавн через 1–3 сек когда свободен слот, тик терпения, уход клиентов.
    public class OrderService
    {
        public const int SlotCount = 3;
        public const float SpawnDelayMin = 1f;
        public const float SpawnDelayMax = 3f;

        /// Стартовое терпение клиента в секундах (когда totalOrdersCompleted=0).
        /// Тоже служит дефолтом для Order.initialPatience.
        public const float Patience = 120f;

        /// Минимум терпения (с прокачкой не падает ниже).
        public const float MinPatience = 60f;

        /// Сколько секунд снимается с базы за каждый успешно выданный заказ.
        public const float PatienceReductionPerOrder = 1f;

        public const float ReputationLossOnAbandon = 0.1f;

        /// Сколько секунд НЕ спавнить новые заказы после успешной выдачи (между
        /// «выдал» и «прилетел новый» дать игроку передых). Можно увеличить/уменьшить.
        public const float PostDeliveryCooldown = 5f;

        private readonly Order[] _slots = new Order[SlotCount];
        private readonly OrderGenerator _generator;
        private readonly ReputationService _reputation;
        private readonly Random _rng;
        private readonly GameState _state;

        private float _spawnTimer;
        private bool _spawnTimerActive;
        private float _postDeliveryCooldownRemaining;

        /// Когда false — новые заказы не появляются (например, во время онбординга
        /// до шага «жди клиента» или пока игрок на экране готовки).
        /// Тик терпения для уже существующих заказов продолжается.
        public bool SpawnEnabled = true;

        public event Action<Order> OrderSpawned;
        public event Action<Order> OrderAbandoned;
        public event Action<int> OrderRemoved;    // slot освобождён по любой причине
        public event Action<int, float> SlotPatienceTick;

        /// Стреляет когда есть свободный слот и подошло время спавна, но сгенерировать заказ
        /// не удалось (обычно: кончились зёрна/расходники). UI показывает подсказку «купи в магазине».
        public event Action CannotSpawnNoIngredients;

        public OrderService(
            OrderGenerator generator,
            ReputationService reputation,
            GameState state,
            Random rng = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rng = rng ?? new System.Random();
        }

        /// Вычисляет терпение для нового заказа исходя из прокачки игрока.
        /// Стартует с Patience, снижается линейно по totalOrdersCompleted, пол — MinPatience.
        public static float ComputePatience(int totalOrdersCompleted)
        {
            float p = Patience - totalOrdersCompleted * PatienceReductionPerOrder;
            if (p < MinPatience) p = MinPatience;
            return p;
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

            // 2. Cooldown после выдачи — не спавним, но таймер кулдауна тикает
            if (_postDeliveryCooldownRemaining > 0)
            {
                _postDeliveryCooldownRemaining -= deltaTime;
                _spawnTimerActive = false;
                return;
            }

            // 3. Спавн нового, если есть свободный слот
            if (!SpawnEnabled)
            {
                _spawnTimerActive = false;
                return;
            }

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
                float patience = ComputePatience(_state.totalOrdersCompleted);
                newOrder.initialPatience = patience;
                newOrder.remainingPatience = patience;
                _slots[freeIndex] = newOrder;
                OrderSpawned?.Invoke(newOrder);
            }
            else
            {
                // Нечего готовить (нет ингредиентов) — сигналим UI, чтобы подсказал купить.
                CannotSpawnNoIngredients?.Invoke();
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

        /// Попросить заспавнить заказ как можно скорее (на ближайшем тике, если есть свободный слот
        /// и SpawnEnabled). Используется онбордингом, чтобы первый клиент пришёл сразу.
        public void RequestImmediateSpawn()
        {
            _spawnTimer = 0f;
            _spawnTimerActive = true;
            _postDeliveryCooldownRemaining = 0f; // immediate spawn перебивает cooldown
        }

        /// Вызывается после успешной выдачи заказа. Включает PostDeliveryCooldown,
        /// чтобы следующий клиент пришёл не сразу.
        public void NotifyOrderCompleted()
        {
            _postDeliveryCooldownRemaining = PostDeliveryCooldown;
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
        /// Записать текущие слоты в GameState.persistedOrders (стирая старый список).
        public void SerializeToState(GameState state)
        {
            state.persistedOrders.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                var order = _slots[i];
                if (order == null) continue;
                state.persistedOrders.Add(new PersistedOrder
                {
                    recipeId = order.recipe != null ? order.recipe.id : null,
                    milkId = order.milk != null ? order.milk.id : null,
                    creamId = order.cream != null ? order.cream.id : null,
                    syrupId = order.syrup != null ? order.syrup.id : null,
                    toppingId = order.topping != null ? order.topping.id : null,
                    isToGo = order.isToGo,
                    remainingPatience = order.remainingPatience,
                    initialPatience = order.initialPatience,
                    slotIndex = order.slotIndex
                });
            }
        }

        /// Восстановить заказы из снимка в GameState. Вызывается один раз после загрузки сейва.
        public void RestoreFromState(GameState state, DrinkitGame.Data.GameContent content)
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = null;
            if (state.persistedOrders == null) return;

            foreach (var p in state.persistedOrders)
            {
                if (string.IsNullOrEmpty(p.recipeId)) continue;
                if (p.slotIndex < 0 || p.slotIndex >= SlotCount) continue;
                // Защита от «грязного» сейва: не восстанавливаем заказ на рецепт,
                // который сейчас НЕ открыт (иначе появляется matcha/капуч после сброса).
                if (state.unlockedRecipeIds != null && !state.unlockedRecipeIds.Contains(p.recipeId))
                    continue;

                var order = new Order
                {
                    recipe = FindRecipe(content, p.recipeId),
                    milk = FindProduct(content, p.milkId),
                    cream = FindProduct(content, p.creamId),
                    syrup = FindProduct(content, p.syrupId),
                    topping = FindProduct(content, p.toppingId),
                    isToGo = p.isToGo,
                    remainingPatience = p.remainingPatience,
                    // Бэкап для старых сейвов где initialPatience=0 → подставляем актуальную базу.
                    initialPatience = p.initialPatience > 0 ? p.initialPatience : ComputePatience(state.totalOrdersCompleted),
                    slotIndex = p.slotIndex
                };
                if (order.recipe == null) continue; // рецепт удалили — пропустим
                _slots[p.slotIndex] = order;
                OrderSpawned?.Invoke(order);
            }
        }

        private static DrinkitGame.Data.RecipeDefinition FindRecipe(DrinkitGame.Data.GameContent content, string id)
        {
            if (string.IsNullOrEmpty(id) || content == null) return null;
            foreach (var r in content.recipes) if (r != null && r.id == id) return r;
            return null;
        }

        private static DrinkitGame.Data.ProductDefinition FindProduct(DrinkitGame.Data.GameContent content, string id)
        {
            if (string.IsNullOrEmpty(id) || content == null) return null;
            foreach (var p in content.products) if (p != null && p.id == id) return p;
            return null;
        }
    }
}
