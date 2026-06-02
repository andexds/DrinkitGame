// Унити вызывает эти функции через [DllImport("__Internal")].
// JS-сторона дёргает Telegram.WebApp.HapticFeedback (https://core.telegram.org/bots/webapps#hapticfeedback).
// На десктопе/вне Telegram — try-catch проглатывает ошибки, ничего не падает.
mergeInto(LibraryManager.library, {

  TgHapticImpact: function(stylePtr) {
    try {
      var style = UTF8ToString(stylePtr);
      var tg = window.Telegram && window.Telegram.WebApp;
      if (tg && tg.HapticFeedback) {
        tg.HapticFeedback.impactOccurred(style);
      }
    } catch (e) { /* silently ignore */ }
  },

  TgHapticNotification: function(typePtr) {
    try {
      var type = UTF8ToString(typePtr);
      var tg = window.Telegram && window.Telegram.WebApp;
      if (tg && tg.HapticFeedback) {
        tg.HapticFeedback.notificationOccurred(type);
      }
    } catch (e) { /* silently ignore */ }
  },

  TgHapticSelection: function() {
    try {
      var tg = window.Telegram && window.Telegram.WebApp;
      if (tg && tg.HapticFeedback) {
        tg.HapticFeedback.selectionChanged();
      }
    } catch (e) { /* silently ignore */ }
  }

});
