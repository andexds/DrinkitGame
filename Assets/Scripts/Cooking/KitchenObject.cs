using System;
using System.Collections;
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

        [Header("Hint (показывается через hintShowDelay сек бездействия)")]
        [Tooltip("Опциональная картинка-хинт (стрелка/палец) рядом с кнопкой. " +
                 "Появляется через hintShowDelay секунд после активации и пульсирует, " +
                 "пока игрок не тапнет (т.е. пока контроллер не вызовет SetActive(false)).")]
        public Image hint;

        [Tooltip("Через сколько секунд после активации показать hint.")]
        [Range(0.5f, 10f)]
        public float hintShowDelay = 2f;

        [Tooltip("Амплитуда пульсации (0.05 = ±5%). 0 = без пульса.")]
        [Range(0f, 0.3f)]
        public float hintPulseAmplitude = 0.05f;

        [Tooltip("Частота пульсации, Гц.")]
        [Range(1f, 12f)]
        public float hintPulseFrequency = 6f;

        /// Срабатывает по тапу, только если объект сейчас активен.
        /// Подписывается CookingScreenController в Awake.
        public event Action Tapped;

        private Button _button;
        private Coroutine _hintCoroutine;

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

            // Hint: останавливаем предыдущую корутину и прячем картинку — стартовое состояние.
            if (_hintCoroutine != null)
            {
                StopCoroutine(_hintCoroutine);
                _hintCoroutine = null;
            }
            if (hint != null)
            {
                hint.gameObject.SetActive(false);
                // На всякий случай сбрасываем scale (мог остаться растянутым после прошлой пульсации).
                hint.rectTransform.localScale = Vector3.one;
            }

            // Если объект активирован — запустим таймер показа hint.
            if (active && hint != null && gameObject.activeInHierarchy)
            {
                _hintCoroutine = StartCoroutine(ShowHintAfterDelay());
            }
        }

        /// Подходит ли этот объект для указанного шага?
        public bool Handles(CookingStepType type)
        {
            if (handlesSteps == null) return false;
            for (int i = 0; i < handlesSteps.Length; i++)
                if (handlesSteps[i] == type) return true;
            return false;
        }

        private IEnumerator ShowHintAfterDelay()
        {
            yield return new WaitForSeconds(hintShowDelay);
            if (hint == null) yield break;

            hint.gameObject.SetActive(true);
            var rt = hint.rectTransform;
            Vector3 baseScale = Vector3.one;

            // Пульс — пока hint.gameObject активен (контроллер выключит его через SetActive(false)).
            while (hint != null && hint.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * hintPulseFrequency) * hintPulseAmplitude;
                rt.localScale = baseScale * pulse;
                yield return null;
            }

            if (rt != null) rt.localScale = baseScale;
            _hintCoroutine = null;
        }
    }
}
