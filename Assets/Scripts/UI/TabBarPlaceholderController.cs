using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Нижний таб-бар: Главная / Магазин. Магазин открывается через UIRouter.
    public class TabBarPlaceholderController : MonoBehaviour
    {
        public Button homeTab;
        public Button storeTab;

        private void Start()
        {
            if (homeTab != null)
                homeTab.onClick.AddListener(() => UIRouter.Instance?.ShowMain());
            if (storeTab != null)
                storeTab.onClick.AddListener(() => UIRouter.Instance?.OpenStore());
        }
    }
}