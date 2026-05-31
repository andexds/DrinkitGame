using System;
using DrinkitGame.Data;

namespace DrinkitGame.Cooking
{
    /// Общий интерфейс для всех мини-игр готовки.
    /// Реализации — MonoBehaviour, прикреплённые к UI-оверлеям.
    public interface IMiniGame
    {
        /// Запустить мини-игру с параметрами текущей машины (определяет ширину зелёной зоны).
        void Begin(MachineTierDefinition tier);

        /// Стреляет когда игрок завершил мини-игру. Параметр — quality 0..100.
        event Action<float> Completed;
    }
}