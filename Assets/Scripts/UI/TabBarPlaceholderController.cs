using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Временные обработчики табов — лог в Console.
    /// "Магазин" будет реализован в Phase 7 (Store Screen).
    public class TabBarPlaceholderController : MonoBehaviour
    {
        public Button homeTab;
        public Button storeTab;

        private void Start()
        {
            if (homeTab != null)
                homeTab.onClick.AddListener(() =>
                    Debug.Log("[TabBar] Главная — мы уже тут."));
            if (storeTab != null)
                storeTab.onClick.AddListener(() =>
                    Debug.Log("[TabBar] Магазин ещё не реализован (Phase 7)."));
        }
    }
}