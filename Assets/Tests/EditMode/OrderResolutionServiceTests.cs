using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class OrderResolutionServiceTests
    {
        private GameContent _content;
        private GameState _state;
        private EconomyService _economy;
        private InventoryService _inventory;
        private QuestService _quests;
        private MachineService _machine;
        private OrderResolutionService _resolver;

        private ProductDefinition _beans, _milkCow, _milkOat, _syrupVanilla, _cupTakeaway;
        private RecipeDefinition _cappuccino;
        private MachineTierDefinition _t1, _t3;

        [SetUp]
        public void Setup()
        {
            _beans = Make("beans", ProductCategory.Beans, 15, 0);
            _milkCow = Make("milk_cow", ProductCategory.Milk, 25, 0);
            _milkOat = Make("milk_oat", ProductCategory.Milk, 60, 60);
            _syrupVanilla = Make("syrup_vanilla", ProductCategory.Syrup, 30, 40);
            _cupTakeaway = Make("cup_takeaway", ProductCategory.Cup, 15, 0);

            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1; _t1.checkBonusPercent = 0;

            _t3 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t3.tierIndex = 3; _t3.checkBonusPercent = 10;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.displayName = "Капучино";
            _cappuccino.basePrice = 250;
            _cappuccino.requiredMachineTier = _t1;
            _cappuccino.fixedIngredients.Add(new IngredientAmount(_beans, 1));

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.products.AddRange(new[] { _beans, _milkCow, _milkOat, _syrupVanilla, _cupTakeaway });
            _content.machineTiers.AddRange(new[] { _t1, _t3 });
            _content.recipes.Add(_cappuccino);

            _state = new GameState { balance = 0, currentMachineTierIndex = 1 };
            _state.unlockedRecipeIds.Add("cappuccino");
            _economy = new EconomyService(_state);
            _inventory = new InventoryService(_state);
            _quests = new QuestService(_state);
            _machine = new MachineService(_state, _content, _economy, _quests);
            _resolver = new OrderResolutionService(_state, _economy, _inventory, _quests, _machine);

            _inventory.Add("beans", 5);
            _inventory.Add("milk_oat", 5);
            _inventory.Add("syrup_vanilla", 5);
            _inventory.Add("cup_takeaway", 5);
        }

        private ProductDefinition Make(string id, ProductCategory cat, int price, int markup)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.id = id; p.category = cat; p.purchasePrice = price; p.sellMarkup = markup;
            return p;
        }

        private Order MakeOrder(ProductDefinition milk = null, ProductDefinition syrup = null, bool toGo = false)
        {
            return new Order
            {
                recipe = _cappuccino,
                milk = milk,
                syrup = syrup,
                isToGo = toGo,
                remainingPatience = OrderService.Patience,
                slotIndex = 0
            };
        }

        [Test]
        public void Complete_BasePrice_FromRecipeAndModifiers()
        {
            var order = MakeOrder(milk: _milkOat, syrup: _syrupVanilla);
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            // 250 + 60 (oat) + 40 (syrup) = 350
            Assert.AreEqual(350, res.basePrice);
        }

        [Test]
        public void Complete_FastDelivery_GivesSpeedBonus()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            // < 60 сек → +0.3
            Assert.AreEqual(0.30f, res.speedMultiplier, 0.0001f);
            Assert.AreEqual("быстро", res.speedLabel);
        }

        [Test]
        public void Complete_SlowDelivery_GivesPenalty()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 240f);
            // > 180 сек → -0.1
            Assert.AreEqual(-0.10f, res.speedMultiplier, 0.0001f);
            Assert.AreEqual("медленно", res.speedLabel);
        }

        [Test]
        public void Complete_HighQuality_GivesBonus()
        {
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 95f, elapsedSeconds: 90f);
            Assert.AreEqual(0.20f, res.qualityMultiplier, 0.0001f);
        }

        [Test]
        public void Complete_T3_AddsTierBonus()
        {
            _state.currentMachineTierIndex = 3;
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 30f);
            Assert.AreEqual(0.10f, res.tierBonusMultiplier, 0.0001f);
        }

        [Test]
        public void Complete_FullBonusStack_FinalPayout_Calculated()
        {
            _state.currentMachineTierIndex = 3;
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 95f, elapsedSeconds: 30f);
            // base=250, mult=1+0.3+0.2+0.1=1.6, payout=400
            Assert.AreEqual(250, res.basePrice);
            Assert.AreEqual(400, res.finalPayout);
        }

        [Test]
        public void Complete_DoubleOrderBuff_DoublesPayoutAndClearsBuff()
        {
            _state.hasDoubleNextOrderBuff = true;
            var order = MakeOrder();
            var res = _resolver.Complete(order, quality: 100f, elapsedSeconds: 90f);
            // base=250, mult=1.0, payout=250 × 2 = 500
            Assert.IsTrue(res.doubleApplied);
            Assert.AreEqual(500, res.finalPayout);
            Assert.IsFalse(_state.hasDoubleNextOrderBuff, "Буст должен быть потрачен");
        }

        [Test]
        public void Complete_ConsumesAllIngredients()
        {
            var order = MakeOrder(milk: _milkOat, syrup: _syrupVanilla, toGo: true);
            _resolver.Complete(order, 100f, 30f);
            Assert.AreEqual(4, _inventory.GetStock("beans"), "beans: 5 - 1 = 4");
            Assert.AreEqual(4, _inventory.GetStock("milk_oat"));
            Assert.AreEqual(4, _inventory.GetStock("syrup_vanilla"));
            Assert.AreEqual(4, _inventory.GetStock("cup_takeaway"));
        }

        [Test]
        public void Complete_AddsBalanceAndRecordsSale()
        {
            var order = MakeOrder();
            int balanceBefore = _economy.Balance;
            _resolver.Complete(order, 100f, 30f);
            Assert.Greater(_economy.Balance, balanceBefore);
            Assert.AreEqual(1, _quests.GetSoldCount("cappuccino"));
        }
    }
}