using DrinkitGame.Core;
using NUnit.Framework;
using System;

namespace DrinkitGame.Tests.EditMode
{
    public class InventoryServiceTests
    {
        [Test]
        public void GetStock_ReturnsZero_WhenProductNotInInventory()
        {
            var service = new InventoryService(new GameState());
            Assert.AreEqual(0, service.GetStock("beans"));
        }

        [Test]
        public void Add_IncreasesStock()
        {
            var service = new InventoryService(new GameState());
            service.Add("beans", 5);
            Assert.AreEqual(5, service.GetStock("beans"));
            service.Add("beans", 3);
            Assert.AreEqual(8, service.GetStock("beans"));
        }

        [Test]
        public void TryConsume_Succeeds_WhenEnough()
        {
            var service = new InventoryService(new GameState());
            service.Add("milk_cow", 10);
            Assert.IsTrue(service.TryConsume("milk_cow", 3));
            Assert.AreEqual(7, service.GetStock("milk_cow"));
        }

        [Test]
        public void TryConsume_Fails_WhenInsufficient()
        {
            var service = new InventoryService(new GameState());
            service.Add("syrup_vanilla", 2);
            Assert.IsFalse(service.TryConsume("syrup_vanilla", 5));
            Assert.AreEqual(2, service.GetStock("syrup_vanilla"));
        }

        [Test]
        public void TryConsume_ProductNotInInventory_ReturnsFalse()
        {
            var service = new InventoryService(new GameState());
            Assert.IsFalse(service.TryConsume("matcha_powder", 1));
        }

        [Test]
        public void HasEnough_ReturnsCorrectAnswer()
        {
            var service = new InventoryService(new GameState());
            service.Add("beans", 5);
            Assert.IsTrue(service.HasEnough("beans", 5));
            Assert.IsTrue(service.HasEnough("beans", 1));
            Assert.IsFalse(service.HasEnough("beans", 6));
            Assert.IsFalse(service.HasEnough("cream", 1));
        }

        [Test]
        public void Add_FiresStockChanged()
        {
            var service = new InventoryService(new GameState());
            string changedId = null;
            int changedCount = -1;
            service.StockChanged += (id, n) => { changedId = id; changedCount = n; };
            service.Add("beans", 7);
            Assert.AreEqual("beans", changedId);
            Assert.AreEqual(7, changedCount);
        }

        [Test]
        public void Add_NegativeOrZero_Throws()
        {
            var service = new InventoryService(new GameState());
            Assert.Throws<ArgumentException>(() => service.Add("beans", 0));
            Assert.Throws<ArgumentException>(() => service.Add("beans", -1));
        }
    }
}