using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class WheelServiceTests
    {
        private GameContent _content;
        private GameState _state;
        private EconomyService _economy;
        private InventoryService _inventory;
        private WheelService _wheel;

        private WheelSectorDefinition _coins50, _coins200, _voucher, _nothing;
        private ProductDefinition _beans;

        [SetUp]
        public void Setup()
        {
            _beans = ScriptableObject.CreateInstance<ProductDefinition>();
            _beans.id = "beans"; _beans.category = ProductCategory.Beans;

            _coins50 = MakeSector(WheelPrizeType.Coins, "50 ₽", 50, 25);
            _coins50.coinsAmount = 50;

            _coins200 = MakeSector(WheelPrizeType.Coins, "200 ₽", 200, 25);
            _coins200.coinsAmount = 200;

            _voucher = MakeSector(WheelPrizeType.DiscountVoucher, "-50%", 0, 25);
            _nothing = MakeSector(WheelPrizeType.Nothing, "пусто", 0, 25);

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.wheelSectors.AddRange(new[] { _coins50, _coins200, _voucher, _nothing });
            _content.products.Add(_beans);

            _state = new GameState();
            _economy = new EconomyService(_state);
            _inventory = new InventoryService(_state);
            _wheel = new WheelService(_state, _content, _economy, _inventory, new System.Random(1));
        }

        private WheelSectorDefinition MakeSector(WheelPrizeType type, string label, int coins, int prob)
        {
            var s = ScriptableObject.CreateInstance<WheelSectorDefinition>();
            s.prizeType = type;
            s.displayLabel = label;
            s.coinsAmount = coins;
            s.probabilityPercent = prob;
            return s;
        }

        [Test]
        public void OnOrderCompleted_GrantsToken_Every5thOrder()
        {
            for (int i = 1; i <= 4; i++)
            {
                _state.totalOrdersCompleted = i;
                _wheel.OnOrderCompleted();
            }
            Assert.AreEqual(0, _wheel.Tokens, "До 5-го заказа жетонов нет");

            _state.totalOrdersCompleted = 5;
            _wheel.OnOrderCompleted();
            Assert.AreEqual(1, _wheel.Tokens, "На 5-м должен быть 1 жетон");

            _state.totalOrdersCompleted = 10;
            _wheel.OnOrderCompleted();
            Assert.AreEqual(2, _wheel.Tokens, "На 10-м — 2 жетона");
        }

        [Test]
        public void TrySpin_ReturnsNull_WhenNoTokens()
        {
            Assert.IsNull(_wheel.TrySpin());
        }

        [Test]
        public void TrySpin_ConsumesToken_AndReturnsSector()
        {
            _wheel.GrantStarterToken();
            Assert.AreEqual(1, _wheel.Tokens);

            var sector = _wheel.TrySpin();
            Assert.IsNotNull(sector);
            Assert.AreEqual(0, _wheel.Tokens);
        }

        [Test]
        public void TrySpin_CoinsPrize_IncreasesBalance()
        {
            // Создадим колесо где только сектор Coins (100%)
            var onlyCoins = ScriptableObject.CreateInstance<WheelSectorDefinition>();
            onlyCoins.prizeType = WheelPrizeType.Coins;
            onlyCoins.coinsAmount = 1000;
            onlyCoins.probabilityPercent = 100;

            _content.wheelSectors.Clear();
            _content.wheelSectors.Add(onlyCoins);
            _state.wheelTokens = 1;

            int balanceBefore = _economy.Balance;
            _wheel.TrySpin();
            Assert.AreEqual(balanceBefore + 1000, _economy.Balance);
        }

        [Test]
        public void TrySpin_VoucherPrize_SetsFlag()
        {
            var only = ScriptableObject.CreateInstance<WheelSectorDefinition>();
            only.prizeType = WheelPrizeType.DiscountVoucher;
            only.probabilityPercent = 100;
            _content.wheelSectors.Clear();
            _content.wheelSectors.Add(only);
            _state.wheelTokens = 1;

            _wheel.TrySpin();
            Assert.IsTrue(_state.hasDiscountVoucher);
        }

        [Test]
        public void TrySpin_DoubleOrderPrize_SetsBuff()
        {
            var only = ScriptableObject.CreateInstance<WheelSectorDefinition>();
            only.prizeType = WheelPrizeType.DoubleNextOrder;
            only.probabilityPercent = 100;
            _content.wheelSectors.Clear();
            _content.wheelSectors.Add(only);
            _state.wheelTokens = 1;

            _wheel.TrySpin();
            Assert.IsTrue(_state.hasDoubleNextOrderBuff);
        }

        [Test]
        public void TrySpin_IngredientPack_AddsToInventory()
        {
            var only = ScriptableObject.CreateInstance<WheelSectorDefinition>();
            only.prizeType = WheelPrizeType.IngredientPack;
            only.packProduct = _beans;
            only.packQuantity = 20;
            only.probabilityPercent = 100;
            _content.wheelSectors.Clear();
            _content.wheelSectors.Add(only);
            _state.wheelTokens = 1;

            _wheel.TrySpin();
            Assert.AreEqual(20, _inventory.GetStock("beans"));
        }
    }
}