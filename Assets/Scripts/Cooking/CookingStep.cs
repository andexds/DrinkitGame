using System;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Типы шагов готовки. Используется в CookingFlow и CookingScreenController.
    public enum CookingStepType
    {
        TakeCup,           // "Тапни стакан 'тут'" или "Тапни стакан 'с собой'"
        GrindCoffee,       // M1 mini-game (заглушка в 8a)
        Extract,           // авто-экстракция эспрессо (просто прогрессбар)
        AddHotWater,       // налить воду из чайника (для американо)
        TakeMilk,          // взять питчер с молоком (нужный тип)
        SteamMilk,         // M2 — вспенивание (заглушка в 8a)
        PourMilk,          // налить молоко в стакан
        TakeCream,         // взять сливки (для рафа)
        SteamCream,        // M2 — взбивание сливок (заглушка в 8a)
        PourCream,         // налить сливки в стакан
        AddMatcha,         // насыпать матча
        SetupFilter,       // поставить V60-воронку
        PourOver,          // M3 — проливание (заглушка в 8a)
        AddCacao,          // насыпать какао
        Whisk,             // M4 — взбивание венчиком (заглушка в 8a)
        AddSyrup,          // добавить сироп
        AddTopping,        // посыпать топпинг
        Deliver            // финальный — "Тапни Выдать"
    }

    /// Один шаг готовки: что показать игроку и что произойдёт по тапу.
    [Serializable]
    public class CookingStep
    {
        public CookingStepType type;
        public string hint;                  // "Тапни кофемолку" / "Возьми стакан"
        public ProductDefinition product;    // null если не связан с конкретным продуктом
        public bool isMiniGame;              // M1/M2/M3/M4 шаги — в 8b будут запускать мини-игру

        public CookingStep(CookingStepType type, string hint, ProductDefinition product = null, bool isMiniGame = false)
        {
            this.type = type;
            this.hint = hint;
            this.product = product;
            this.isMiniGame = isMiniGame;
        }
    }
}