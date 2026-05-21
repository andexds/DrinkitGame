using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Временный плейсхолдер: пока колесо не реализовано, кнопка просто пишет в Console.
    [RequireComponent(typeof(Button))]
    public class WheelButtonPlaceholderController : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
                Debug.Log("[WheelButton] Колесо ещё не реализовано (Phase 9)."));
        }
    }
}