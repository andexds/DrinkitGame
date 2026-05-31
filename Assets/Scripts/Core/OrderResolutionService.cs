using System;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Атомарно завершает заказ: считает чек, списывает ингредиенты, начисляет деньги,
    /// записывает продажу в квесты.
    public class OrderResolutionService
    {
        public const float SpeedFastThreshold = 60f;
        public const float SpeedNormalThreshold = 180f;
        public const float QualityHighThreshold = 80f;
        public const float QualityLowThreshold = 50f;
        public const float SpeedBonusFast = 0.30f;
        public const float SpeedBonusSlow = -0.10f;
        public const float QualityBonusHigh = 0.20f;
        public const float QualityBonusLow = -0.10f;

        private readonly GameState _state;
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly QuestService _quests;
        private readonly MachineService _machine;

        public OrderResolutionService(
            GameState state,
            EconomyService economy,
            InventoryService inventory,
            QuestService quests,
            MachineService machine)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// Завершить выдачу заказа.
        /// quality — 0..100 (в Phase 6 всегда 100; в Phase 8 — среднее мини-игр).
        /// elapsedSeconds — сколько секунд клиент ждал (Patience - remainingPatience).
        public OrderResolution Complete(Order order, float quality, float elapsedSeconds)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            // 1. База
            int basePrice = order.recipe.basePrice;
            if (order.milk != null) basePrice += order.milk.sellMarkup;
            if (order.syrup != null) basePrice += order.syrup.sellMarkup;
            if (order.topping != null) basePrice += order.topping.sellMarkup;

            // 2. Бонусы
            (float speedMult, string speedLabel) = ComputeSpeed(elapsedSeconds);
            (float qualityMult, string qualityLabel) = ComputeQuality(quality);
            float tierMult = (_machine.CurrentTier?.checkBonusPercent ?? 0) / 100f;

            float totalMultiplier = 1f + speedMult + qualityMult + tierMult;
            int payout = Mathf.Max(0, Mathf.RoundToInt(basePrice * totalMultiplier));

            // 3. Буст "×2 на следующий заказ" если активен
            bool doubleApplied = _state.hasDoubleNextOrderBuff;
            if (doubleApplied)
            {
                payout *= 2;
                _state.hasDoubleNextOrderBuff = false;
            }

            // 4. Расход ингредиентов
            ConsumeIngredients(order);

            // 5. Деньги
            if (payout > 0) _economy.Earn(payout);

            // 6. Квест-счётчик
            _quests.RecordSale(order.recipe.id);

            return new OrderResolution
            {
                recipeId = order.recipe.id,
                recipeDisplayName = order.recipe.displayName,
                basePrice = basePrice,
                speedMultiplier = speedMult,
                qualityMultiplier = qualityMult,
                tierBonusMultiplier = tierMult,
                doubleApplied = doubleApplied,
                finalPayout = payout,
                speedLabel = speedLabel,
                qualityLabel = qualityLabel
            };
        }

        private static (float mult, string label) ComputeSpeed(float elapsed)
        {
            if (elapsed < SpeedFastThreshold) return (SpeedBonusFast, "быстро");
            if (elapsed < SpeedNormalThreshold) return (0f, "норм");
            return (SpeedBonusSlow, "медленно");
        }

        private static (float mult, string label) ComputeQuality(float quality)
        {
            if (quality > QualityHighThreshold) return (QualityBonusHigh, "отлично");
            if (quality < QualityLowThreshold) return (QualityBonusLow, "плохо");
            return (0f, "норм");
        }

        private void ConsumeIngredients(Order order)
        {
            foreach (var ing in order.recipe.fixedIngredients)
            {
                if (ing.product == null) continue;
                _inventory.TryConsume(ing.product.id, ing.amount);
            }
            if (order.milk != null) _inventory.TryConsume(order.milk.id, 1);
            if (order.cream != null) _inventory.TryConsume(order.cream.id, 1);
            if (order.syrup != null) _inventory.TryConsume(order.syrup.id, 1);
            if (order.topping != null) _inventory.TryConsume(order.topping.id, 1);
            // Стакан "с собой" — один фиксированный тип, списываем по строковому id.
            if (order.isToGo) _inventory.TryConsume("cup_takeaway", 1);
        }
    }
}