using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Статический генератор последовательности шагов готовки по заказу.
    /// Логика switch'ит по recipe.family и подмешивает модификаторы.
    public static class CookingFlow
    {
        public static List<CookingStep> GenerateSteps(Order order)
        {
            var steps = new List<CookingStep>();
            if (order == null || order.recipe == null) return steps;

            // 1. Всегда первый шаг — взять стакан
            steps.Add(new CookingStep(
                CookingStepType.TakeCup,
                order.isToGo ? "Возьми стакан 'с собой'" : "Возьми стакан 'в кафе'"));

            // 2. Дальше — семейство-специфичные шаги
            switch (order.recipe.family)
            {
                case RecipeFamily.Espresso:
                    AddEspressoCore(steps);
                    break;
                case RecipeFamily.Americano:
                    AddEspressoCore(steps);
                    steps.Add(new CookingStep(CookingStepType.AddHotWater, "Налить горячую воду"));
                    break;
                case RecipeFamily.Cappuccino:
                case RecipeFamily.Latte:
                    AddEspressoCore(steps);
                    AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Raf:
                    AddEspressoCore(steps);
                    AddCreamSteamPour(steps, order.cream);
                    break;
                case RecipeFamily.Cacao:
                    steps.Add(new CookingStep(CookingStepType.AddCacao, "Насыпь какао"));
                    AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Matcha:
                    steps.Add(new CookingStep(CookingStepType.AddMatcha, "Насыпь матчу"));
                    steps.Add(new CookingStep(CookingStepType.AddHotWater, "Залей горячую воду"));
                    steps.Add(new CookingStep(CookingStepType.Whisk, "Взбей венчиком (M4)", isMiniGame: true));
                    if (order.milk != null) AddMilkSteamPour(steps, order.milk);
                    break;
                case RecipeFamily.Filter:
                    steps.Add(new CookingStep(CookingStepType.SetupFilter, "Поставь V60-воронку"));
                    steps.Add(new CookingStep(CookingStepType.GrindCoffee, "Намели кофе (M1)", isMiniGame: true));
                    steps.Add(new CookingStep(CookingStepType.PourOver, "Залей водой (M3)", isMiniGame: true));
                    break;
            }

            // 3. Модификаторы — сироп / топпинг
            if (order.syrup != null)
                steps.Add(new CookingStep(CookingStepType.AddSyrup, $"Добавь сироп: {order.syrup.displayName.ToLower()}", order.syrup));
            if (order.topping != null)
                steps.Add(new CookingStep(CookingStepType.AddTopping, $"Посыпь: {order.topping.displayName.ToLower()}", order.topping));

            // 4. Финальная выдача
            steps.Add(new CookingStep(CookingStepType.Deliver, "Тапни 'Выдать'"));

            return steps;
        }

        private static void AddEspressoCore(List<CookingStep> steps)
        {
            steps.Add(new CookingStep(CookingStepType.GrindCoffee, "Намели кофе (M1)", isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.Extract, "Запусти эспрессо-машину"));
        }

        private static void AddMilkSteamPour(List<CookingStep> steps, ProductDefinition milk)
        {
            string milkName = milk != null ? milk.displayName.ToLower() : "молоко";
            steps.Add(new CookingStep(CookingStepType.TakeMilk, $"Налей {milkName} в питчер", milk));
            steps.Add(new CookingStep(CookingStepType.SteamMilk, "Вспень молоко (M2)", milk, isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.PourMilk, "Налей в стакан", milk));
        }

        private static void AddCreamSteamPour(List<CookingStep> steps, ProductDefinition cream)
        {
            steps.Add(new CookingStep(CookingStepType.TakeCream, "Налей сливки в питчер", cream));
            steps.Add(new CookingStep(CookingStepType.SteamCream, "Вспень сливки (M2)", cream, isMiniGame: true));
            steps.Add(new CookingStep(CookingStepType.PourCream, "Налей в стакан", cream));
        }
    }
}