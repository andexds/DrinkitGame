using System;

namespace DrinkitGame.Core
{
    /// Управляет балансом игрока. Все транзакции с деньгами идут через этот сервис.
    public class EconomyService
    {
        private readonly GameState _state;

        /// Стреляет после каждого изменения баланса. Параметр — новый баланс.
        public event Action<int> BalanceChanged;

        public EconomyService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public int Balance => _state.balance;

        /// Зачислить N₽ на баланс. amount должен быть > 0.
        public void Earn(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            _state.balance += amount;
            BalanceChanged?.Invoke(_state.balance);
        }

        /// Списать N₽. Возвращает true если хватило денег, иначе false (баланс не меняется).
        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("amount must be positive", nameof(amount));
            if (_state.balance < amount) return false;
            _state.balance -= amount;
            BalanceChanged?.Invoke(_state.balance);
            return true;
        }
    }
}