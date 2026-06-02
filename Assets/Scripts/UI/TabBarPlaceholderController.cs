using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Нижний таб-бар: Главная / Магазин. Висит на постоянном GameObject (Canvas root
    /// или TabBar-контейнере), который НЕ деактивируется при смене экранов.
    /// Подсвечивает активный таб по событию UIRouter.ScreenChanged.
    public class TabBarPlaceholderController : MonoBehaviour
    {
        [Header("Tab buttons")]
        public Button homeTab;
        public Button storeTab;

        [Header("Active highlight (optional)")]
        [Tooltip("Цвет Image-фона активного таба (того, экран которого открыт).")]
        public Color activeColor = new(0.353f, 0.553f, 0.863f); // 5A8DDC

        [Tooltip("Цвет Image-фона неактивного таба.")]
        public Color inactiveColor = new(0.710f, 0.780f, 0.898f); // B5C7E5

        [Header("Active label tint (optional)")]
        [Tooltip("Цвет TMP-лейбла активного таба.")]
        public Color activeLabelColor = Color.white;

        [Tooltip("Цвет TMP-лейбла неактивного таба.")]
        public Color inactiveLabelColor = new(0.30f, 0.34f, 0.45f);

        private void Start()
        {
            if (homeTab != null)
                homeTab.onClick.AddListener(() => UIRouter.Instance?.ShowMain());
            if (storeTab != null)
                storeTab.onClick.AddListener(() => UIRouter.Instance?.OpenStore());

            // Подписываемся на смену экрана и сразу подсвечиваем текущий.
            if (UIRouter.Instance != null)
            {
                UIRouter.Instance.ScreenChanged += OnScreenChanged;
                OnScreenChanged(UIRouter.Instance.CurrentScreen);
            }
        }

        private void OnDestroy()
        {
            if (UIRouter.Instance != null)
                UIRouter.Instance.ScreenChanged -= OnScreenChanged;
        }

        private void OnScreenChanged(Screen screen)
        {
            // Только две вкладки в таб-баре: Home и Store. На Cooking/Wheel ни одна
            // не подсвечивается как «активный таб» (это временные модальные экраны).
            bool homeActive = screen == Screen.Main;
            bool storeActive = screen == Screen.Store;

            SetTabActive(homeTab, homeActive);
            SetTabActive(storeTab, storeActive);
        }

        private void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;

            // Фон таба — Image на самой кнопке.
            var img = tab.GetComponent<Image>();
            if (img != null) img.color = active ? activeColor : inactiveColor;

            // Лейбл — первый TMP_Text в дочерних объектах.
            var label = tab.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? activeLabelColor : inactiveLabelColor;
        }
    }
}
