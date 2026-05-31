using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrinkitGame.Core
{
    /// Вся мутабельная информация одной игровой сессии.
    /// Это POCO — никакого Unity-API внутри. Сервисы мутируют поля напрямую.
    /// Сериализуется в JSON через JsonUtility и сохраняется в PlayerPrefs.
    [Serializable]
    public class GameState
    {
        [Tooltip("Текущий баланс в рублях.")]
        public int balance;

        [Tooltip("Репутация 0.0–5.0 (информативно).")]
        public float reputation = 5f;

        [Tooltip("Индекс текущего тира кофемашины (1, 2 или 3).")]
        public int currentMachineTierIndex = 1;

        [Tooltip("ID рецептов, которые уже куплены/открыты.")]
        public List<string> unlockedRecipeIds = new();

        [Tooltip("Слоты инвентаря (1 запись на каждый купленный продукт).")]
        public List<InventorySlot> inventory = new();

        [Tooltip("Счётчики проданных напитков для квестов.")]
        public List<RecipeSoldCount> recipeSoldCounts = new();

        [Tooltip("Сколько жетонов колеса удачи накоплено.")]
        public int wheelTokens;

        [Tooltip("Есть ли активный ваучер скидки -50% на след. рецепт.")]
        public bool hasDiscountVoucher;

        [Tooltip("Активен ли буст 'следующий заказ ×2'.")]
        public bool hasDoubleNextOrderBuff;

        [Tooltip("Прошёл ли игрок онбординг (хотя бы один раз).")]
        public bool onboardingCompleted;

        [Tooltip("Сколько заказов всего успешно выдано (для накопления жетонов колеса).")]
        public int totalOrdersCompleted;

        [Tooltip("Активные заказы в слотах — снимок для сохранения.")]
        public List<PersistedOrder> persistedOrders = new();
    }

    /// Пара "продукт → остаток" в инвентаре.
    [Serializable]
    public class InventorySlot
    {
        public string productId;
        public int count;

        public InventorySlot() { }
        public InventorySlot(string productId, int count)
        {
            this.productId = productId;
            this.count = count;
        }
    }

    /// Пара "рецепт → сколько продано" для квестов.
    [Serializable]
    public class RecipeSoldCount
    {
        public string recipeId;
        public int count;

        public RecipeSoldCount() { }
        public RecipeSoldCount(string recipeId, int count)
        {
            this.recipeId = recipeId;
            this.count = count;
        }
    }
    /// Снимок активного заказа для сохранения между сессиями (по id-шкам, без SO-ссылок).
    [Serializable]
    public class PersistedOrder
    {
        public string recipeId;
        public string milkId;
        public string creamId;
        public string syrupId;
        public string toppingId;
        public bool isToGo;
        public float remainingPatience;
        public int slotIndex;
    }
}