using DrinkitGame.Cooking;
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class MiniGameQualityTests
    {
        [Test]
        public void FromZoneHit_DeadCenter_Returns100()
        {
            float q = MiniGameQuality.FromZoneHit(0.5f, 0.5f, 0.2f);
            Assert.AreEqual(100f, q, 0.01f);
        }

        [Test]
        public void FromZoneHit_EdgeOfZone_Returns60()
        {
            // позиция на правой границе зоны
            float q = MiniGameQuality.FromZoneHit(0.6f, 0.5f, 0.2f);
            Assert.That(q, Is.InRange(58f, 62f), $"На краю зоны должно быть ~60, было {q}");
        }

        [Test]
        public void FromZoneHit_OutsideZone_Penalizes()
        {
            float q = MiniGameQuality.FromZoneHit(0.0f, 0.5f, 0.2f);
            Assert.Less(q, 50f, "Сильно вне зоны должно быть < 50");
        }

        [Test]
        public void FromZoneHit_FarOutside_ReturnsZero()
        {
            float q = MiniGameQuality.FromZoneHit(0.0f, 1.0f, 0.05f);
            Assert.AreEqual(0f, q, 0.01f);
        }

        [Test]
        public void FromTapCount_OnTarget_Returns100()
        {
            Assert.AreEqual(100f, MiniGameQuality.FromTapCount(12, 12), 0.01f);
        }

        [Test]
        public void FromTapCount_Half_Returns50()
        {
            Assert.AreEqual(50f, MiniGameQuality.FromTapCount(6, 12), 0.01f);
        }

        [Test]
        public void FromTapCount_Above_CapsAt100()
        {
            Assert.AreEqual(100f, MiniGameQuality.FromTapCount(30, 12), 0.01f);
        }
    }
}