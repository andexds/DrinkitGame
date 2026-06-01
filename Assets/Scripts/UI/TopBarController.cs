using DrinkitGame.Core;
using TMPro;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Отображает рейтинг, баланс и текущий goal — обновляется на событиях сервисов.
    public class TopBarController : MonoBehaviour
    {
        [Header("Labels (TMP) inside pills")]
        public TMP_Text ratingLabel;
        public TMP_Text balanceLabel;
        public TMP_Text goalLabel;

        private GameStateManager _gsm;

        // true, когда заказы не спавнятся из-за нехватки ингредиентов — тогда в строке
        // статуса показываем предупреждение вместо обычной цели.
        private bool _noIngredients;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null)
            {
                Debug.LogError("[TopBar] GameStateManager.Instance == null. Убедись что GameStateManager на GameRoot и сцена корректна.");
                return;
            }

            // Подписки
            _gsm.Economy.BalanceChanged += OnBalanceChanged;
            _gsm.Reputation.ReputationChanged += OnReputationChanged;

            // Goal не имеет своего события — пересчитываем когда что-то меняется
            _gsm.Economy.BalanceChanged += _ => RefreshGoal();
            _gsm.Quests.CountChanged += (_, __) => RefreshGoal();
            _gsm.Recipes.RecipeUnlocked += _ => RefreshGoal();
            _gsm.Machine.Upgraded += _ => RefreshGoal();

            // Нехватка ингредиентов: ставим предупреждение; пополнение склада снимает его.
            _gsm.Orders.CannotSpawnNoIngredients += OnCannotSpawn;
            _gsm.Inventory.StockChanged += OnStockChanged;

            // Первый рендер
            OnBalanceChanged(_gsm.Economy.Balance);
            OnReputationChanged(_gsm.Reputation.Reputation);
            RefreshGoal();
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged -= OnBalanceChanged;
            _gsm.Reputation.ReputationChanged -= OnReputationChanged;
            _gsm.Orders.CannotSpawnNoIngredients -= OnCannotSpawn;
            _gsm.Inventory.StockChanged -= OnStockChanged;
            // Лямбды не отписываются по делегату — для прототипа допустимо: GSM умирает вместе со сценой.
        }

        private void OnCannotSpawn()
        {
            if (_noIngredients) return;
            _noIngredients = true;
            RefreshGoal();
        }

        private void OnStockChanged(string productId, int newCount)
        {
            // Любое пополнение склада — снимаем предупреждение и пробуем спавнить снова.
            if (!_noIngredients) return;
            _noIngredients = false;
            RefreshGoal();
        }

        private void OnBalanceChanged(int newBalance)
        {
            if (balanceLabel != null) balanceLabel.text = $"{newBalance} ₽";
        }

        private void OnReputationChanged(float newRep)
        {
            if (ratingLabel != null) ratingLabel.text = $"Рейтинг {newRep:F1}";
        }

        private void RefreshGoal()
        {
            if (goalLabel == null || _gsm == null) return;

            // Приоритет — предупреждение о нехватке ингредиентов.
            if (_noIngredients)
            {
                goalLabel.text = "Кончились зёрна! Купи в магазине";
                return;
            }

            var goal = _gsm.GoalTracker.CurrentGoal();
            goalLabel.text = string.IsNullOrEmpty(goal.ProgressLabel)
                ? goal.Description
                : $"{goal.Description} — {goal.ProgressLabel}";
        }
    }
}