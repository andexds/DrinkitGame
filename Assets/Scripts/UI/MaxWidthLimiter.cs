using UnityEngine;

namespace DrinkitGame.UI
{
    /// Удерживает ширину UI-элемента не больше maxWidth, центрируя его в родителе
    /// по горизонтали. Если родитель уже maxWidth — элемент занимает всю доступную
    /// ширину. По вертикали — стандартный stretch.
    ///
    /// Привяжи на CookingScreenPanel (или любую панель), задай Max Width.
    /// Можно использовать на нескольких панелях одновременно — каждой свой лимит.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class MaxWidthLimiter : MonoBehaviour
    {
        [Tooltip("Максимальная ширина в логических пикселях Canvas Scaler. " +
                 "При 375×812 reference выстави, например, 400-500 — на телефоне " +
                 "будет полная ширина, на планшете/десктопе обрежется по бокам.")]
        public float maxWidth = 500f;

        [Tooltip("Растягивать ли по высоте на всё пространство родителя. " +
                 "Если false — высоту настраивай вручную через RectTransform.")]
        public bool stretchVertical = true;

        private RectTransform _rt;
        private RectTransform _parentRt;

        private void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            _parentRt = _rt.parent as RectTransform;
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (_rt == null || _parentRt == null) return;

            // Якоря: горизонталь — точка по центру (0.5, 0.5), вертикаль — растягиваем (0, 1).
            float yMin = stretchVertical ? 0f : _rt.anchorMin.y;
            float yMax = stretchVertical ? 1f : _rt.anchorMax.y;
            _rt.anchorMin = new Vector2(0.5f, yMin);
            _rt.anchorMax = new Vector2(0.5f, yMax);
            _rt.pivot = new Vector2(0.5f, 0.5f);

            // Считаем фактическую ширину: min(parent, maxWidth).
            float parentW = _parentRt.rect.width;
            float w = Mathf.Min(parentW, maxWidth);

            // sizeDelta: при центральной горизонтальной якорной точке .x = реальная ширина.
            // При растянутой вертикали .y = добавка к высоте (0 = вся высота родителя).
            var size = _rt.sizeDelta;
            size.x = w;
            if (stretchVertical) size.y = 0f;
            _rt.sizeDelta = size;

            // Центрируем горизонтально.
            var pos = _rt.anchoredPosition;
            pos.x = 0f;
            if (stretchVertical) pos.y = 0f;
            _rt.anchoredPosition = pos;
        }
    }
}
