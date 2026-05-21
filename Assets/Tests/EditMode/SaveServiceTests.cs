using DrinkitGame.Core;
using DrinkitGame.Save;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class SaveServiceTests
    {
        private SaveService _save;

        [SetUp]
        public void Setup()
        {
            _save = new SaveService();
            _save.Clear(); // чистим перед каждым тестом
        }

        [TearDown]
        public void TearDown()
        {
            _save.Clear();
        }

        [Test]
        public void Load_ReturnsNull_WhenNoSave()
        {
            Assert.IsNull(_save.Load());
        }

        [Test]
        public void Save_ThenLoad_RoundTripsBalance()
        {
            var state = new GameState { balance = 1234, reputation = 4.2f };
            _save.Save(state);
            var loaded = _save.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1234, loaded.balance);
            Assert.AreEqual(4.2f, loaded.reputation, 0.0001f);
        }

        [Test]
        public void Save_PreservesInventoryAndUnlockedRecipes()
        {
            var state = new GameState();
            state.balance = 500;
            state.inventory.Add(new InventorySlot("beans", 25));
            state.inventory.Add(new InventorySlot("milk_oat", 8));
            state.unlockedRecipeIds.Add("espresso");
            state.unlockedRecipeIds.Add("americano");
            _save.Save(state);

            var loaded = _save.Load();
            Assert.AreEqual(2, loaded.inventory.Count);
            Assert.AreEqual("beans", loaded.inventory[0].productId);
            Assert.AreEqual(25, loaded.inventory[0].count);
            CollectionAssert.AreEqual(
                new[] { "espresso", "americano" },
                loaded.unlockedRecipeIds);
        }

        [Test]
        public void Clear_RemovesSave()
        {
            _save.Save(new GameState { balance = 100 });
            _save.Clear();
            Assert.IsNull(_save.Load());
        }
    }
}