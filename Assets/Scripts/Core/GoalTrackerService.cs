using DrinkitGame.Data;

namespace DrinkitGame.Core
{
    /// Считает текущую "первую невыполненную цель" в линейной прогрессии.
    /// Логика приоритета (см. спек, §11.2):
    ///   1. Купить рецепт американо
    ///   2. Купить машину T2 (квест + цена)
    ///   3. Купить капучино, латте, какао, раф (в порядке)
    ///   4. Купить машину T3
    ///   5. Купить фильтр, матчу
    ///   6. Финал
    public class GoalTrackerService
    {
        private readonly GameState _state;
        private readonly GameContent _content;
        private readonly EconomyService _economy;
        private readonly QuestService _quests;
        private readonly MachineService _machine;

        public GoalTrackerService(
            GameState state,
            GameContent content,
            EconomyService economy,
            QuestService quests,
            MachineService machine)
        {
            _state = state;
            _content = content;
            _economy = economy;
            _quests = quests;
            _machine = machine;
        }

        public Goal CurrentGoal()
        {
            // Линейный обход в порядке, который мы хотим:
            // 1. Сначала идём по рецептам в порядке id: americano → cappuccino → latte → cacao → raf → filter → matcha
            // 2. Между ними проверяем апгрейды машины (когда они уже доступны и нужны)
            string[] orderedRecipeIds =
            {
                "americano", "cappuccino", "latte", "cacao", "raf", "filter", "matcha"
            };

            // Сначала проверяем: если до cappuccino дойти, нужна машина T2
            // Проверим прогрессию через машину тоже:
            var nextRecipe = NextUnlockTarget(orderedRecipeIds);
            if (nextRecipe == null)
                return Goal.Final;

            // Если для следующего рецепта нужна машина, цель = "купи машину"
            if (nextRecipe.requiredMachineTier != null
                && _state.currentMachineTierIndex < nextRecipe.requiredMachineTier.tierIndex)
            {
                return MakeMachineUpgradeGoal();
            }

            // Иначе цель — купить сам рецепт
            return MakeRecipePurchaseGoal(nextRecipe);
        }

        private RecipeDefinition NextUnlockTarget(string[] orderedIds)
        {
            foreach (var id in orderedIds)
            {
                if (_state.unlockedRecipeIds.Contains(id)) continue;
                foreach (var r in _content.recipes)
                    if (r.id == id) return r;
            }
            return null;
        }

        private Goal MakeRecipePurchaseGoal(RecipeDefinition recipe)
        {
            // Если есть квест-условие — показываем его прогресс
            if (recipe.unlockQuestTargetRecipe != null && recipe.unlockQuestTargetCount > 0)
            {
                int sold = _quests.GetSoldCount(recipe.unlockQuestTargetRecipe.id);
                int target = recipe.unlockQuestTargetCount;
                if (sold < target)
                {
                    return new Goal(
                        recipe.unlockQuestDescription,
                        $"{sold} / {target}");
                }
            }
            int price = _state.hasDiscountVoucher ? recipe.recipePurchasePrice / 2 : recipe.recipePurchasePrice;
            return new Goal(
                $"Купи рецепт «{recipe.displayName}»",
                $"{_economy.Balance} / {price} ₽");
        }

        private Goal MakeMachineUpgradeGoal()
        {
            var next = _machine.NextTier;
            if (next == null) return Goal.Final;

            // Проверим квест машины
            if (next.questTargetRecipe1 != null && next.questTargetCount1 > 0)
            {
                int sold = _quests.GetSoldCount(next.questTargetRecipe1.id);
                if (sold < next.questTargetCount1)
                {
                    return new Goal(
                        next.questDescription,
                        $"{sold} / {next.questTargetCount1}");
                }
            }
            if (next.questTargetRecipe2 != null && next.questTargetCount2 > 0)
            {
                int sold = _quests.GetSoldCount(next.questTargetRecipe2.id);
                if (sold < next.questTargetCount2)
                {
                    return new Goal(
                        next.questDescription,
                        $"{sold} / {next.questTargetCount2}");
                }
            }
            return new Goal(
                $"Купи кофемашину «{next.displayName}»",
                $"{_economy.Balance} / {next.purchasePrice} ₽");
        }
    }
}