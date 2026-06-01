using DrinkitGame.Core;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Простой роутер между UI-панелями. Висит на Canvas или GameRoot.
    /// Singleton — UI компоненты находят его через Instance.
    public class UIRouter : MonoBehaviour
    {
        public static UIRouter Instance { get; private set; }

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
        }

        public void OpenCooking(Order order)
        {
            if (cookingController != null) cookingController.Bind(order);
            SetActive(mainScreenPanel, false);
            SetActive(cookingScreenPanel, true);
            SetActive(orderResultPopup, false);
            SetActive(storeScreenPanel, false);
            SetActive(wheelScreenPanel, false);
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
        }
    }
}