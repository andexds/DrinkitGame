using UnityEngine;

namespace DrinkitGame.Cooking
{
    /// Чистые функции расчёта Quality для мини-игр. Без MonoBehaviour — тестируется.
    public static class MiniGameQuality
    {
        /// Quality = 100 если позиция в центре зелёной зоны; линейно падает к границам.
        /// position — где остановился индикатор (0..1).
        /// zoneCenter — центр зелёной зоны (0..1).
        /// zoneWidth — ширина зелёной зоны (0..1).
        public static float FromZoneHit(float position, float zoneCenter, float zoneWidth)
        {
            float halfWidth = zoneWidth * 0.5f;
            float distance = Mathf.Abs(position - zoneCenter);
            if (distance <= halfWidth)
            {
                // Внутри зоны: 100 в центре, линейно до 60 на краях
                float normalized = 1f - (distance / halfWidth);
                return 60f + normalized * 40f;
            }
            // Вне зоны: 60 на границе, линейно падает к 0
            float outside = distance - halfWidth;
            return Mathf.Max(0f, 60f - outside * 300f);
        }

        /// Quality для rapid-tap: количество тапов / целевое количество, кэп 100.
        public static float FromTapCount(int taps, int target)
        {
            if (target <= 0) return 0f;
            float ratio = (float)taps / target;
            return Mathf.Clamp(ratio * 100f, 0f, 100f);
        }
    }
}