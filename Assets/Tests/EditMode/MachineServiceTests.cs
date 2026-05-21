using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class MachineServiceTests
    {
        private GameContent _content;
        private MachineTierDefinition _t1, _t2, _t3;
        private RecipeDefinition _americano, _cappuccino, _latte;

        [SetUp]
        public void Setup()
        {
            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";

            _latte = ScriptableObject.CreateInstance<RecipeDefinition>();
            _latte.id = "latte";

            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;
            _t1.purchasePrice = 0;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;
            _t2.purchasePrice = 1500;
            _t2.questTargetRecipe1 = _americano;
            _t2.questTargetCount1 = 10;

            _t3 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t3.tierIndex = 3;
            _t3.purchasePrice = 5000;
            _t3.questTargetRecipe1 = _cappuccino;
            _t3.questTargetCount1 = 5;
            _t3.questTargetRecipe2 = _latte;
            _t3.questTargetCount2 = 5;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.machineTiers.AddRange(new[] { _t1, _t2, _t3 });
        }

        private MachineService MakeService(GameState state, out EconomyService eco, out QuestService quests)
        {
            eco = new EconomyService(state);
            quests = new QuestService(state);
            return new MachineService(state, _content, eco, quests);
        }

        [Test]
        public void CurrentTier_T1_ByDefault()
        {
            var state = new GameState();
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(1, service.CurrentTierIndex);
            Assert.AreSame(_t1, service.CurrentTier);
        }

        [Test]
        public void NextTier_T2_FromT1()
        {
            var state = new GameState();
            var service = MakeService(state, out _, out _);
            Assert.AreSame(_t2, service.NextTier);
        }

        [Test]
        public void NextTier_Null_AtMax()
        {
            var state = new GameState { currentMachineTierIndex = 3 };
            var service = MakeService(state, out _, out _);
            Assert.IsNull(service.NextTier);
            Assert.AreEqual(UpgradeAvailability.MaxTier, service.GetUpgradeAvailability());
        }

        [Test]
        public void GetUpgradeAvailability_NotEnoughMoney()
        {
            var state = new GameState { balance = 100 };
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(UpgradeAvailability.NotEnoughMoney, service.GetUpgradeAvailability());
        }

        [Test]
        public void GetUpgradeAvailability_QuestIncomplete()
        {
            var state = new GameState { balance = 5000 }; // достаточно денег
            var service = MakeService(state, out _, out _);
            Assert.AreEqual(UpgradeAvailability.QuestIncomplete, service.GetUpgradeAvailability());
        }

        [Test]
        public void TryUpgrade_ToT2_WhenAllConditionsMet()
        {
            var state = new GameState { balance = 2000 };
            var service = MakeService(state, out var eco, out var quests);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");

            Assert.AreEqual(UpgradeAvailability.Available, service.GetUpgradeAvailability());
            Assert.IsTrue(service.TryUpgrade());
            Assert.AreEqual(2, service.CurrentTierIndex);
            Assert.AreEqual(500, eco.Balance); // 2000 - 1500
        }

        [Test]
        public void TryUpgrade_FiresUpgradedEvent()
        {
            var state = new GameState { balance = 2000 };
            var service = MakeService(state, out _, out var quests);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");

            MachineTierDefinition upgraded = null;
            service.Upgraded += t => upgraded = t;
            service.TryUpgrade();
            Assert.AreSame(_t2, upgraded);
        }
    }
}