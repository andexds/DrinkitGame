using System;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Управляет репутацией (float 0.0–5.0). Информативная — ни на что в прототипе не влияет.
    public class ReputationService
    {
        public const float Min = 0f;
        public const float Max = 5f;

        private readonly GameState _state;

        /// Стреляет после любого изменения. Параметр — новое значение.
        public event Action<float> ReputationChanged;

        public ReputationService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public float Reputation => _state.reputation;

        /// Изменить репутацию на дельту (может быть отрицательной). Зажимается в [Min, Max].
        public void Adjust(float delta)
        {
            float next = Mathf.Clamp(_state.reputation + delta, Min, Max);
            if (Mathf.Approximately(next, _state.reputation)) return;
            _state.reputation = next;
            ReputationChanged?.Invoke(_state.reputation);
        }
    }
}