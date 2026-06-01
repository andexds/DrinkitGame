using System;
using DrinkitGame.Cooking;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Тап-зона на сцене готовки (кофемолка, машина, фильтр, банка сиропа и т.п.).
    /// Декларирует, какой шаг CookingFlow она «закрывает» при тапе.
    /// CookingScreenController сам решает, активна она сейчас или нет.
    [RequireComponent(typeof(Button))]
    public class KitchenObject : MonoBehaviour
    {
        [Tooltip("Какие типы шагов закрывает этот объект. Обычно 1–2 (например, " +
                 "CoffeeMachine: Extract + AddHotWater).")]
        public CookingStepType[] handlesSteps;

        [Tooltip("GameObject подсветки/обводки — включается когда объект активен. " +
                 "Можно оставить null если подсветки нет.")]
        public GameObject highlight;

        [Tooltip("Опционально: затемняем CanvasGroup когда объект не активен. " +
                 "Если null — alpha не меняется, только highlight.")]
        public CanvasGroup canvasGroup;

        [Tooltip("Прозрачность когда объект НЕ активен (0..1). Обычно 0.4.")]
        [Range(0f, 1f)]
        public float inactiveAlpha = 0.4f;

        /// Срабатывает по тапу. Подписывается CookingScreenController.
        public event Action Tapped;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
            SetActive(false);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            // Тап считается только если объект сейчас «вооружён».
            if (_button.interactable) Tapped?.Invoke();
        }

        /// Включить/выключить объект как активную тап-зону для текущего шага.
        public void SetActive(bool active)
        {
            if (_button != null) _button.interactable = active;
            if (highlight != null) highlight.SetActive(active);
            if (canvasGroup != null)
                canvasGroup.alpha = active ? 1f : inactiveAlpha;
        }

        /// Подходит ли этот объект для указанного шага?
        public bool Handles(CookingStepType type)
        {
            if (handlesSteps == null) return false;
            for (int i = 0; i < handlesSteps.Length; i++)
                if (handlesSteps[i] == type) return true;
            return false;
        }
    }
}