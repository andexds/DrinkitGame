using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Главный контроллер магазина: переключает 3 вкладки.
    public class StoreScreenController : MonoBehaviour
    {
        [Header("Tab buttons")]
        public Button recipesTabButton;
        public Button ingredientsTabButton;
        public Button machineTabButton;

        [Header("Tab content roots")]
        public GameObject recipesContent;
        public GameObject ingredientsContent;
        public GameObject machineContent;

        [Header("Tab button colors")]
        public Color activeColor = new(0.353f, 0.553f, 0.863f); // 5A8DDC
        public Color inactiveColor = new(0.710f, 0.780f, 0.898f); // B5C7E5

        private void Awake()
        {
            if (recipesTabButton != null) recipesTabButton.onClick.AddListener(() => ShowTab(0));
            if (ingredientsTabButton != null) ingredientsTabButton.onClick.AddListener(() => ShowTab(1));
            if (machineTabButton != null) machineTabButton.onClick.AddListener(() => ShowTab(2));
        }

        private void OnEnable()
        {
            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            if (recipesContent != null) recipesContent.SetActive(index == 0);
            if (ingredientsContent != null) ingredientsContent.SetActive(index == 1);
            if (machineContent != null) machineContent.SetActive(index == 2);

            SetColor(recipesTabButton, index == 0);
            SetColor(ingredientsTabButton, index == 1);
            SetColor(machineTabButton, index == 2);
        }

        private void SetColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? activeColor : inactiveColor;
        }
    }
}