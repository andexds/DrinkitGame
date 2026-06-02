using System.Runtime.InteropServices;
using UnityEngine;

namespace DrinkitGame.Telegram
{
    /// Обёртка над Telegram.WebApp.HapticFeedback API.
    /// Работает только в WebGL-сборке внутри Telegram (iOS/Android клиент).
    /// На десктопе Telegram и в Unity Editor — no-op (просто Debug.Log).
    /// JS-плагин лежит в Assets/Plugins/WebGL/TelegramBridge.jslib.
    public static class TelegramHaptics
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void TgHapticImpact(string style);
        [DllImport("__Internal")] private static extern void TgHapticNotification(string type);
        [DllImport("__Internal")] private static extern void TgHapticSelection();
#endif

        /// Тычок. Стили: "light" | "medium" | "heavy" | "rigid" | "soft".
        public static void Impact(string style = "medium")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TgHapticImpact(style);
#else
            // В редакторе тихо логируем — удобно отладить.
            // Debug.Log($"[Haptic] impact({style})");
#endif
        }

        /// Уведомление с паттерном. Типы: "error" | "success" | "warning".
        public static void Notification(string type)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TgHapticNotification(type);
#else
            // Debug.Log($"[Haptic] notification({type})");
#endif
        }

        /// Лёгкий «клик» выбора.
        public static void Selection()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TgHapticSelection();
#else
            // Debug.Log($"[Haptic] selection()");
#endif
        }

        // === Удобные шорткаты ===

        public static void Light()  => Impact("light");
        public static void Medium() => Impact("medium");
        public static void Heavy()  => Impact("heavy");
        public static void Soft()   => Impact("soft");
        public static void Rigid()  => Impact("rigid");

        public static void Success() => Notification("success");
        public static void Error()   => Notification("error");
        public static void Warning() => Notification("warning");

        /// Конвертит прогресс 0..1 в стиль impact с нарастанием.
        /// Используется в hold-мини-играх (M2 молоко, M3 пролив).
        public static void ImpactByProgress(float progress01)
        {
            if (progress01 < 0.33f) Impact("light");
            else if (progress01 < 0.66f) Impact("medium");
            else Impact("heavy");
        }
    }
}
