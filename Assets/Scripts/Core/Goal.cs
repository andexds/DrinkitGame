namespace DrinkitGame.Core
{
    /// Что игроку нужно сделать дальше (текстовая цель сверху главного экрана).
    public readonly struct Goal
    {
        public readonly string Description;       // напр. "Купи рецепт американо"
        public readonly string ProgressLabel;     // напр. "100 / 100 ₽" или "7 / 10"
        public readonly bool IsFinal;             // true если игрок открыл всё

        public Goal(string description, string progressLabel, bool isFinal = false)
        {
            Description = description;
            ProgressLabel = progressLabel;
            IsFinal = isFinal;
        }

        public static Goal Final =>
            new("Все рецепты открыты! Просто играй.", string.Empty, true);
    }
}