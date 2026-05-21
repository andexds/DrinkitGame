using DrinkitGame.Core;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class ReputationServiceTests
    {
        [Test]
        public void DefaultReputation_Is5()
        {
            var service = new ReputationService(new GameState());
            Assert.AreEqual(5f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_DecreasesReputation()
        {
            var service = new ReputationService(new GameState());
            service.Adjust(-0.1f);
            Assert.AreEqual(4.9f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_ClampedAtZero()
        {
            var service = new ReputationService(new GameState { reputation = 0.05f });
            service.Adjust(-1f);
            Assert.AreEqual(0f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_ClampedAtFive()
        {
            var service = new ReputationService(new GameState { reputation = 4.95f });
            service.Adjust(1f);
            Assert.AreEqual(5f, service.Reputation, 0.0001f);
        }

        [Test]
        public void Adjust_FiresChangedEvent()
        {
            var service = new ReputationService(new GameState());
            float notified = -1f;
            service.ReputationChanged += r => notified = r;
            service.Adjust(-0.1f);
            Assert.AreEqual(4.9f, notified, 0.0001f);
        }

        [Test]
        public void Adjust_NoChange_DoesNotFireEvent()
        {
            var service = new ReputationService(new GameState { reputation = 5f });
            bool fired = false;
            service.ReputationChanged += _ => fired = true;
            service.Adjust(1f); // уже на максимуме
            Assert.IsFalse(fired);
        }
    }
}