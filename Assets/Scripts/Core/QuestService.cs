using System;

namespace DrinkitGame.Core
{
    /// Считает сколько каких рецептов было успешно продано (для квестов на разблокировку).
    public class QuestService
    {
        private readonly GameState _state;

        /// Стреляет после увеличения счётчика. Параметры: recipeId, новое значение.
        public event Action<string, int> CountChanged;

        public QuestService(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// Сколько раз продан указанный рецепт (0 если ни разу).
        public int GetSoldCount(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                throw new ArgumentException("recipeId is empty", nameof(recipeId));
            foreach (var entry in _state.recipeSoldCounts)
                if (entry.recipeId == recipeId) return entry.count;
            return 0;
        }

        /// Увеличить счётчик продаж для рецепта на 1.
        public void RecordSale(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                throw new ArgumentException("recipeId is empty", nameof(recipeId));
            var entry = FindOrCreate(recipeId);
            entry.count += 1;
            CountChanged?.Invoke(recipeId, entry.count);
        }

        private RecipeSoldCount FindOrCreate(string recipeId)
        {
            foreach (var entry in _state.recipeSoldCounts)
                if (entry.recipeId == recipeId) return entry;
            var fresh = new RecipeSoldCount(recipeId, 0);
            _state.recipeSoldCounts.Add(fresh);
            return fresh;
        }
    }
}