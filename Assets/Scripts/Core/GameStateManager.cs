using DrinkitGame.Data;
using DrinkitGame.Save;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Корневой MonoBehaviour. Создаёт GameState, сервисы, подписывает Save.
    /// Висит на GameObject 'GameRoot' в сцене Main.
    public class GameStateManager : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Корневой GameContent.asset со всеми SO-данными.")]
        public GameContent content;
        
        public static GameStateManager Instance { get; private set; }


        // Открытые ссылки на сервисы (UI будет их подписывать в Phase 4).
        public GameState State { get; private set; }
        public EconomyService Economy { get; private set; }
        public InventoryService Inventory { get; private set; }
        public ReputationService Reputation { get; private set; }
        public QuestService Quests { get; private set; }
        public RecipeService Recipes { get; private set; }
        public MachineService Machine { get; private set; }
        public GoalTrackerService GoalTracker { get; private set; }
        public SaveService Save { get; private set; }
        public OrderGenerator OrderGenerator { get; private set; }
        public OrderService Orders { get; private set; }
        public OrderResolutionService OrderResolution { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (content == null)
            {
                Debug.LogError("[GameStateManager] GameContent не назначен в инспекторе!");
                return;
            }

            Save = new SaveService();
            State = Save.Load() ?? CreateFreshState();

            Economy = new EconomyService(State);
            Inventory = new InventoryService(State);
            Reputation = new ReputationService(State);
            Quests = new QuestService(State);
            Recipes = new RecipeService(State, content, Economy, Quests);
            Machine = new MachineService(State, content, Economy, Quests);
            GoalTracker = new GoalTrackerService(State, content, Economy, Quests, Machine);
            OrderGenerator = new OrderGenerator(State, content, Inventory);
            Orders = new OrderService(OrderGenerator, Reputation);
            OrderResolution = new OrderResolutionService(State, Economy, Inventory, Quests, Machine);

            // Гарантируем что стартовый рецепт открыт (даже если в сейве пропал почему-то)
            Recipes.EnsureStarterUnlocked();

            // Подписываем сохранение на любые изменения
            Economy.BalanceChanged += _ => Save.Save(State);
            Inventory.StockChanged += (_, __) => Save.Save(State);
            Reputation.ReputationChanged += _ => Save.Save(State);
            Quests.CountChanged += (_, __) => Save.Save(State);
            Recipes.RecipeUnlocked += _ => Save.Save(State);
            Machine.Upgraded += _ => Save.Save(State);
            Orders.OrderSpawned += _ => Save.Save(State);
            Orders.OrderAbandoned += _ => Save.Save(State);

            Debug.Log(
                $"[GameStateManager] Start. " +
                $"Balance={Economy.Balance}, Reputation={Reputation.Reputation:F1}, " +
                $"MachineT={Machine.CurrentTierIndex}, " +
                $"UnlockedRecipes={string.Join(",", State.unlockedRecipeIds)}, " +
                $"Beans={Inventory.GetStock("beans")}");

            var goal = GoalTracker.CurrentGoal();
            Debug.Log($"[GoalTracker] {goal.Description} — {goal.ProgressLabel}");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private GameState CreateFreshState()
        {
            var state = new GameState
            {
                balance = content.starterBalance,
                reputation = 5f,
                currentMachineTierIndex = content.starterMachineTier?.tierIndex ?? 1
            };
            if (content.starterRecipe != null)
                state.unlockedRecipeIds.Add(content.starterRecipe.id);
            if (content.starterBeansStock > 0)
            {
                // Найдём продукт с id='beans' (или возьмём первый Beans-категории)
                foreach (var p in content.products)
                {
                    if (p.id == "beans")
                    {
                        state.inventory.Add(new InventorySlot(p.id, content.starterBeansStock));
                        break;
                    }
                }
            }
            return state;
        }

        /// Утилитный метод для сброса (вызывается из тестов и из debug-меню в будущем).
        public void ResetProgress()
        {
            Save.Clear();
            State = CreateFreshState();
            Debug.Log("[GameStateManager] Прогресс сброшен.");
        }
    }
}