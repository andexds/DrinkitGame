using System;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Тап-зона на сцене готовки (кофемолка, машина, фильтр, банка сиропа и т.п.).
    /// Декларирует, какие типы шагов CookingFlow она «закрывает» при тапе.
    /// CookingScreenController сам решает, активна она сейчас или нет, и подключает обработчик.
    [RequireComponent(typeof(Button))]
    public class KitchenObject : MonoBehaviour
    {
        [Tooltip("Какие типы шагов закрывает этот объект. Обычно 1–2 значения, но Topping " +
                 "может одновременно отвечать за AddSyrup + AddTopping + AddCacao + AddMatcha.")]
        public CookingStepType[] handlesSteps;

        [Header("TakeCup only")]
        [Tooltip("Только для шага TakeCup: true = стакан 'с собой', false = 'тут'. " +
                 "Контроллер сверяет с order.isToGo — если не совпало, тап игнорируется.")]
        public bool isToGoCup;

        [Header("Visual")]
        [Tooltip("Дочерний GameObject подсветки (обводка/glow). Включается когда объект активен. " +
                 "Можно оставить null если без подсветки.")]
        public GameObject highlight;

        [Tooltip("Опционально: CanvasGroup на этом же объекте — затемняется когда объект не активен. " +
                 "Если null — alpha не меняется, только highlight.")]
        public CanvasGroup canvasGroup;

        [Tooltip("Прозрачность когда объект НЕ активен (0..1). По умолчанию 0.4.")]
        [Range(0f, 1f)]
        public float inactiveAlpha = 0.4f;

        /// Срабатывает по тапу, только если объект сейчас активен.
        /// Подписывается CookingScreenController в Awake.
        public event Action Tapped;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
            SetActive(false); // по умолчанию неактивен, контроллер включит когда нужен шаг
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (_button != null && _button.interactable) Tapped?.Invoke();
        }

        /// Включить/выключить объект как активную тап-зону.
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
