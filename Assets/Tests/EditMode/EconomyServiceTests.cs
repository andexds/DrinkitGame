using DrinkitGame.Core;
using NUnit.Framework;
using System;

namespace DrinkitGame.Tests.EditMode
{
    public class EconomyServiceTests
    {
        [Test]
        public void Earn_AddsToBalance()
        {
            var state = new GameState { balance = 100 };
            var service = new EconomyService(state);
            service.Earn(50);
            Assert.AreEqual(150, service.Balance);
        }

        [Test]
        public void Earn_NegativeOrZero_Throws()
        {
            var service = new EconomyService(new GameState());
            Assert.Throws<ArgumentException>(() => service.Earn(0));
            Assert.Throws<ArgumentException>(() => service.Earn(-10));
        }

        [Test]
        public void TrySpend_Succeeds_WhenEnough()
        {
            var state = new GameState { balance = 100 };
            var service = new EconomyService(state);
            Assert.IsTrue(service.TrySpend(60));
            Assert.AreEqual(40, service.Balance);
        }

        [Test]
        public void TrySpend_Fails_WhenInsufficient()
        {
            var state = new GameState { balance = 30 };
            var service = new EconomyService(state);
            Assert.IsFalse(service.TrySpend(50));
            Assert.AreEqual(30, service.Balance);
        }

        [Test]
        public void Earn_FiresBalanceChangedEvent()
        {
            var service = new EconomyService(new GameState { balance = 0 });
            int notified = -1;
            service.BalanceChanged += b => notified = b;
            service.Earn(100);
            Assert.AreEqual(100, notified);
        }

        [Test]
        public void TrySpend_DoesNotFireEvent_WhenInsufficient()
        {
            var service = new EconomyService(new GameState { balance = 10 });
            bool fired = false;
            service.BalanceChanged += _ => fired = true;
            service.TrySpend(100);
            Assert.IsFalse(fired);
        }
    }
}