using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.Save
{
    /// Сохраняет/загружает GameState в PlayerPrefs как JSON.
    /// В WebGL PlayerPrefs мапится на IndexedDB браузера (Telegram-клиент кэширует).
    public class SaveService
    {
        public const string Key = "DrinkitGame.Save.v1";

        /// Сохранить состояние в PlayerPrefs (синхронно).
        public void Save(GameState state)
        {
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        /// Загрузить состояние. null если сейв ещё не сделан.
        public GameState Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            string json = PlayerPrefs.GetString(Key);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<GameState>(json);
        }

        /// Удалить сейв (полезно для тестов или сброса прогресса).
        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}