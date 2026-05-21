using DrinkitGame.Core;
using DrinkitGame.Data;
using NUnit.Framework;
using UnityEngine;

namespace DrinkitGame.Tests.EditMode
{
    public class GoalTrackerServiceTests
    {
        private GameContent _content;
        private MachineTierDefinition _t1, _t2;
        private RecipeDefinition _espresso, _americano, _cappuccino;

        [SetUp]
        public void Setup()
        {
            _t1 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t1.tierIndex = 1;

            _t2 = ScriptableObject.CreateInstance<MachineTierDefinition>();
            _t2.tierIndex = 2;
            _t2.purchasePrice = 1500;
            _t2.displayName = "Бариста";
            _t2.questDescription = "Продай 10 американо";
            _t2.questTargetCount1 = 10;

            _espresso = ScriptableObject.CreateInstance<RecipeDefinition>();
            _espresso.id = "espresso";
            _espresso.requiredMachineTier = _t1;

            _americano = ScriptableObject.CreateInstance<RecipeDefinition>();
            _americano.id = "americano";
            _americano.displayName = "Американо";
            _americano.recipePurchasePrice = 100;
            _americano.requiredMachineTier = _t1;

            _cappuccino = ScriptableObject.CreateInstance<RecipeDefinition>();
            _cappuccino.id = "cappuccino";
            _cappuccino.displayName = "Капучино";
            _cappuccino.recipePurchasePrice = 500;
            _cappuccino.requiredMachineTier = _t2;

            _t2.questTargetRecipe1 = _americano;

            _content = ScriptableObject.CreateInstance<GameContent>();
            _content.machineTiers.AddRange(new[] { _t1, _t2 });
            _content.recipes.AddRange(new[] { _espresso, _americano, _cappuccino });
        }

        private GoalTrackerService MakeService(GameState state)
        {
            var eco = new EconomyService(state);
            var quests = new QuestService(state);
            var machine = new MachineService(state, _content, eco, quests);
            return new GoalTrackerService(state, _content, eco, quests, machine);
        }

        [Test]
        public void FirstGoal_IsBuyAmericano()
        {
            var state = new GameState();
            state.unlockedRecipeIds.Add("espresso");
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("Американо", goal.Description);
        }

        [Test]
        public void AfterAmericano_GoalIsT2Quest()
        {
            var state = new GameState();
            state.unlockedRecipeIds.AddRange(new[] { "espresso", "americano" });
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("10 американо", goal.Description);
        }

        [Test]
        public void AfterQuestSatisfied_GoalIsBuyT2()
        {
            var state = new GameState { balance = 0 };
            state.unlockedRecipeIds.AddRange(new[] { "espresso", "americano" });
            var quests = new QuestService(state);
            for (int i = 0; i < 10; i++) quests.RecordSale("americano");
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            StringAssert.Contains("Бариста", goal.Description);
            StringAssert.Contains("/ 1500 ₽", goal.ProgressLabel);
        }

        [Test]
        public void AllRecipesUnlocked_GoalIsFinal()
        {
            var state = new GameState { currentMachineTierIndex = 2 };
            state.unlockedRecipeIds.AddRange(new[]
            {
                "espresso", "americano", "cappuccino"
            });
            // ВНИМАНИЕ: в setUp у нас только эти три. Для полноты тестируем что
            // когда все из orderedIds, что есть в content.recipes, открыты — Final.
            var service = MakeService(state);
            var goal = service.CurrentGoal();
            Assert.IsTrue(goal.IsFinal);
        }
    }
}