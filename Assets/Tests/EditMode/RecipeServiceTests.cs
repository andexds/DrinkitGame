using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class RecipeServiceTests
    {
        private GameContent _content;
        private RecipeDefinition _espresso;
        private RecipeDefinition _americano;
        private RecipeDefinition _cappuccino;
        private MachineTierDefinition _t1;
        private MachineTierDefinition _t2;

        [SetUp]
        public void Setup()
        {
            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;

            _espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            _espresso.id = "espresso";
            _espresso.recipePurchasePrice = 0;
            _espresso.requiredMachineTier = _t1;

            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";
            _americano.recipePurchasePrice = 100;
            _americano.requiredMachineTier = _t1;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.recipePurchasePrice = 500;
            _cappuccino.requiredMachineTier = _t2;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.recipes.AddRange(new[] { _espresso, _americano, _cappuccino });
            _content.starterRecipe = _espresso;
        }

        private RecipeService MakeService(GameState state, out EconomyService eco)
        {
            eco = new EconomyService(state);
            var quests = new QuestService(state);
            return new RecipeService(state, _content, eco, quests);
        }

        [Test]
        public void EnsureStarterUnlocked_AddsStarterRecipe()
        {
            var state = new GameState();
            var service = MakeService(state, out _);
            service.EnsureStarterUnlocked();
            Assert.IsTrue(service.IsUnlocked("espresso"));
        }

        [Test]
        public void TryPurchase_Americano_SpendsMoneyAndUnlocks()
        {
            var state = new GameState { balance = 200 };
            var service = MakeService(state, out var eco);
            Assert.IsTrue(service.TryPurchase(_americano));
            Assert.IsTrue(service.IsUnlocked("americano"));
            Assert.AreEqual(100, eco.Balance);
        }

        [Test]
        public void TryPurchase_Cappuccino_FailsWithoutT2()
        {
            var state = new GameState { balance = 1000, currentMachineTierIndex = 1 };
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.NeedsHigherMachine,
                service.GetAvailability(_cappuccino));
            Assert.IsFalse(service.TryPurchase(_cappuccino));
        }

        [Test]
        public void TryPurchase_NotEnoughMoney_Fails()
        {
            var state = new GameState { balance = 50 };
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.NotEnoughMoney,
                service.GetAvailability(_americano));
            Assert.IsFalse(service.TryPurchase(_americano));
            Assert.AreEqual(50, state.balance);
        }

        [Test]
        public void TryPurchase_AlreadyOwned_Fails()
        {
            var state = new GameState { balance = 200 };
            state.unlockedRecipeIds.Add("americano");
            var service = MakeService(state, out _);
            Assert.AreEqual(PurchaseAvailability.AlreadyOwned,
                service.GetAvailability(_americano));
            Assert.IsFalse(service.TryPurchase(_americano));
        }

        [Test]
        public void TryPurchase_AppliesDiscountVoucher()
        {
            var state = new GameState { balance = 60, hasDiscountVoucher = true };
            var service = MakeService(state, out var eco);
            Assert.IsTrue(service.TryPurchase(_americano)); // 100 * 0.5 = 50
            Assert.AreEqual(10, eco.Balance);
            Assert.IsFalse(state.hasDiscountVoucher); // ваучер потрачен
        }

        [Test]
        public void TryPurchase_RequiresQuestComplete()
        {
            // делаем латте с квестом 'продать 15 капучино'
            var latte = ScriptableObject.CreateInstance<RecipeDefinition>();
            latte.id = "latte";
            latte.recipePurchasePrice = 600;
            latte.requiredMachineTier = _t2;
            latte.unlockQuestTargetRecipe = _cappuccino;
            latte.unlockQuestTargetCount = 15;

            var state = new GameState
            {
                balance = 1000,
                currentMachineTierIndex = 2
            };
            var service = MakeService(state, out _);

            Assert.AreEqual(PurchaseAvailability.NeedsMoreSales,
                service.GetAvailability(latte));
        }

        [Test]
        public void RecipeUnlocked_EventFires_OnSuccessfulPurchase()
        {
            var state = new GameState { balance = 200 };
            var service = MakeService(state, out _);
            RecipeDefinition unlocked = null;
            service.RecipeUnlocked += r => unlocked = r;
            service.TryPurchase(_americano);
            Assert.AreSame(_americano, unlocked);
        }
    }
}