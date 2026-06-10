using System;
using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Колесо удачи: жетоны и спин по вероятностям.
    public class WheelService
    {
        public const int OrdersPerToken = 5;

        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly Random _rng;

        public event Action<int> TokensChanged;       // новый счёт жетонов
        public event Action<WheelSectorDefinition> Spun; // выпавший сектор

        public WheelService(
            GameState state,
            GameContent content,
            EconomyService economy,
            InventoryService inventory,
            Random rng = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _rng = rng ?? new System.Random();
        }

        public int Tokens => _state.wheelTokens;

        /// Вызывается после каждого OrderCompleted: накапливает 1 жетон каждые N заказов.
        public void OnOrderCompleted()
        {
            if (_state.totalOrdersCompleted % OrdersPerToken == 0
                && _state.totalOrdersCompleted > 0)
            {
                _state.wheelTokens += 1;
                TokensChanged?.Invoke(_state.wheelTokens);
            }
        }

        /// Дать бесплатный стартовый жетон (один раз в онбординге).
        public void GrantStarterToken()
        {
            _state.wheelTokens += 1;
            TokensChanged?.Invoke(_state.wheelTokens);
        }

        /// Попытка крутить колесо. Возвращает выпавший сектор или null если жетонов нет.
        public WheelSectorDefinition TrySpin()
        {
            if (_state.wheelTokens <= 0) return null;
            _state.wheelTokens -= 1;
            TokensChanged?.Invoke(_state.wheelTokens);

            var sector = PickSector();
            ApplyPrize(sector);
            Spun?.Invoke(sector);
            return sector;
        }

        private WheelSectorDefinition PickSector()
        {
            int total = 0;
            foreach (var s in _content.wheelSectors) total += s.probabilityPercent;
            if (total <= 0) return null;

            int roll = _rng.Next(total);
            foreach (var s in _content.wheelSectors)
            {
                if (roll < s.probabilityPercent) return s;
                roll -= s.probabilityPercent;
            }
            return _content.wheelSectors[_content.wheelSectors.Count - 1];
        }

        private void ApplyPrize(WheelSectorDefinition sector)
        {
            if (sector == null) return;
            switch (sector.prizeType)
            {
                case WheelPrizeType.Coins:
                    if (sector.coinsAmount > 0) _economy.Earn(sector.coinsAmount);
                    break;
                case WheelPrizeType.IngredientPack:
                    if (sector.packProduct != null && sector.packQuantity > 0)
                        _inventory.Add(sector.packProduct.id, sector.packQuantity);
                    break;
                case WheelPrizeType.DiscountVoucher:
                    _state.hasDiscountVoucher = true;
                    break;
                case WheelPrizeType.DoubleNextOrder:
                    _state.hasDoubleNextOrderBuff = true;
                    break;
                case WheelPrizeType.MilkyWay:
                    // Физический приз — в игре ничего не делаем, игрок получит шоколадку IRL.
                    break;
                case WheelPrizeType.Nothing:
                    // ничего
                    break;
            }
        }
    }
}