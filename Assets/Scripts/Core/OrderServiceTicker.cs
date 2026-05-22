using UnityEngine;

namespace DrinkitGame.Core
{
    /// MonoBehaviour-обёртка вокруг OrderService.Tick() — гонит таймер каждый кадр.
    /// Висит на GameRoot рядом с GameStateManager.
    public class OrderServiceTicker : MonoBehaviour
    {
        private void Update()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.Orders == null) return;
            gsm.Orders.Tick(Time.deltaTime);
        }
    }
}