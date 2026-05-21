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
            // Лямбды не отписываются по делегату — для прототипа допустимо: GSM умирает вместе со сценой.
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
            var goal = _gsm.GoalTracker.CurrentGoal();
            goalLabel.text = string.IsNullOrEmpty(goal.ProgressLabel)
                ? goal.Description
                : $"{goal.Description} — {goal.ProgressLabel}";
        }
    }
}