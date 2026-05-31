using UnityEngine;

namespace DrinkitGame.Core
{
    /// MonoBehaviour-обёртка вокруг OrderService.Tick().
    /// Не тикает когда приложение в фоне (модель "пауза при выходе").
    public class OrderServiceTicker : MonoBehaviour
    {
        private bool _paused;

        private void Update()
        {
            if (_paused) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.Orders == null) return;
            gsm.Orders.Tick(Time.deltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _paused = !hasFocus;
        }

        private void OnApplicationPause(bool pause)
        {
            _paused = pause;
        }
    }
}