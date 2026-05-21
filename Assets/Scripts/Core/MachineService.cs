using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Текущий тир кофемашины и логика прокачки.
    public class MachineService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;

        /// Стреляет после успешной прокачки. Параметр — новый тир.
        public event Action<MachineTierDefinition> Upgraded;

        public MachineService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
        }

        public int CurrentTierIndex => _state.currentMachineTierIndex;

        public MachineTierDefinition CurrentTier
        {
            get
            {
                foreach (var t in _content.machineTiers)
                    if (t.tierIndex == _state.currentMachineTierIndex) return t;
                return null;
            }
        }

        /// Следующий тир (или null если уже максимальный).
        public MachineTierDefinition NextTier
        {
            get
            {
                int next = _state.currentMachineTierIndex + 1;
                foreach (var t in _content.machineTiers)
                    if (t.tierIndex == next) return t;
                return null;
            }
        }

        /// Доступна ли прокачка прямо сейчас.
        public UpgradeAvailability GetUpgradeAvailability()
        {
            var next = NextTier;
            if (next == null) return UpgradeAvailability.MaxTier;
            if (_economy.Balance < next.purchasePrice) return UpgradeAvailability.NotEnoughMoney;
            if (next.questTargetRecipe1 != null
                && _quests.GetSoldCount(next.questTargetRecipe1.id) < next.questTargetCount1)
                return UpgradeAvailability.QuestIncomplete;
            if (next.questTargetRecipe2 != null
                && _quests.GetSoldCount(next.questTargetRecipe2.id) < next.questTargetCount2)
                return UpgradeAvailability.QuestIncomplete;
            return UpgradeAvailability.Available;
        }

        /// Прокачать машину на следующий тир.
        public bool TryUpgrade()
        {
            if (GetUpgradeAvailability() != UpgradeAvailability.Available) return false;
            var next = NextTier;
            if (!_economy.TrySpend(next.purchasePrice)) return false;
            _state.currentMachineTierIndex = next.tierIndex;
            Upgraded?.Invoke(next);
            return true;
        }
    }

    public enum UpgradeAvailability
    {
        Available,
        MaxTier,
        NotEnoughMoney,
        QuestIncomplete
    }
}