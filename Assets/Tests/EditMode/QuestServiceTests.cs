using DrinkitGame.Core;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class QuestServiceTests
    {
        [Test]
        public void GetSoldCount_ReturnsZero_ForUnseenRecipe()
        {
            var service = new QuestService(new GameState());
            Assert.AreEqual(0, service.GetSoldCount("americano"));
        }

        [Test]
        public void RecordSale_IncrementsCount()
        {
            var service = new QuestService(new GameState());
            service.RecordSale("americano");
            service.RecordSale("americano");
            service.RecordSale("espresso");
            Assert.AreEqual(2, service.GetSoldCount("americano"));
            Assert.AreEqual(1, service.GetSoldCount("espresso"));
        }

        [Test]
        public void RecordSale_FiresCountChanged()
        {
            var service = new QuestService(new GameState());
            string id = null;
            int n = -1;
            service.CountChanged += (i, c) => { id = i; n = c; };
            service.RecordSale("latte");
            Assert.AreEqual("latte", id);
            Assert.AreEqual(1, n);
        }
    }
}