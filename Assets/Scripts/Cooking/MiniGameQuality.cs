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
            if (distance > halfWidth) return Mathf.Max(0f, 100f - (distance - halfWidth) * 500f);
            float normalized = 1f - (distance / halfWidth);
            return 60f + normalized * 40f; // от 60 в краях зоны до 100 в центре
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