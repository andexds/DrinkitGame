using System;
using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Текущий «верхний» экран — для подсветки активного таба в TabBar.
    public enum Screen
    {
        Main,
        Cooking,
        Store,
        Wheel,
    }

    /// Простой роутер между UI-панелями. Висит на Canvas или GameRoot.
    /// Singleton — UI компоненты находят его через Instance.
    public class UIRouter : MonoBehaviour
    {
        public static UIRouter Instance { get; private set; }

        /// Стреляет после каждой смены экрана. TabBarPlaceholderController подписывается
        /// и обновляет подсветку. Параметр — какой экран теперь активный.
        public event Action<Screen> ScreenChanged;

        /// Какой экран сейчас активен. Доступно сразу после ShowMain/OpenStore/etc.
        public Screen CurrentScreen { get; private set; } = Screen.Main;

        [Header("Panels (root GameObjects)")]
        public GameObject mainScreenPanel;
        public GameObject cookingScreenPanel;
        public GameObject orderResultPopup;
        public GameObject storeScreenPanel;
        public GameObject wheelScreenPanel;

        [Header("Optional cooking controller")]
        public CookingScreenController cookingController;
        public OrderResultPopupController resultPopupController;

        

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ShowMain();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowMain()
        {
            SetActive(mainScreenPanel, true);
            SetActive(cookingScreenPanel, false);
            SetActive(orderResultPopup, false);
            SetActive(storeScreenPanel, false);
            SetActive(wheelScreenPanel, false);
            CurrentScreen = Screen.Main;
            ScreenChanged?.Invoke(CurrentScreen);
        }

        public void OpenCooking(Order order)
        {
            // ВАЖНО: активируем cookingScreenPanel ДО вызова Bind. Иначе при Bind →
            // ShowCurrentStep → KitchenObject.SetActive(true) объект ещё неактивен
            // в иерархии, и StartCoroutine для хинта не стартует.
            SetActive(mainScreenPanel, false);
            SetActive(orderResultPopup, false);
            SetActive(storeScreenPanel, false);
            SetActive(wheelScreenPanel, false);
            SetActive(cookingScreenPanel, true);
            if (cookingController != null) cookingController.Bind(order);
            CurrentScreen = Screen.Cooking;
            ScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowOrderResult(OrderResolution resolution)
        {
            if (resultPopupController != null) resultPopupController.Show(resolution);
            SetActive(orderResultPopup, true);
        }

        public void HideOrderResult()
        {
            SetActive(orderResultPopup, false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
        public void OpenStore()
        {
            SetActive(mainScreenPanel, false);
            SetActive(cookingScreenPanel, false);
            SetActive(storeScreenPanel, true);
            SetActive(orderResultPopup, false);
            SetActive(wheelScreenPanel, false);
            CurrentScreen = Screen.Store;
            ScreenChanged?.Invoke(CurrentScreen);
        }

        /// Открыть магазин сразу на нужной вкладке (Recipes / Ingredients / Machine).
        public void OpenStoreOnTab(StoreTab tab)
        {
            if (storeScreenPanel != null)
            {
                var ctrl = storeScreenPanel.GetComponent<StoreScreenController>();
                if (ctrl != null) ctrl.DefaultTab = tab;
            }
            OpenStore();
        }
        public void OpenWheel()
        {
            SetActive(mainScreenPanel, false);
            SetActive(cookingScreenPanel, false);
            SetActive(storeScreenPanel, false);
            SetActive(wheelScreenPanel, true);
            SetActive(orderResultPopup, false);
            CurrentScreen = Screen.Wheel;
            ScreenChanged?.Invoke(CurrentScreen);
        }
    }
}