using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Строгий letterbox-зум для UI: внутренний ContentRoot всегда держит размер
    /// referenceResolution (375×812 по дефолту), масштабируется как единое целое
    /// чтобы поместиться в Canvas, центрируется. Лишние пиксели по краям экрана
    /// = «полосы» (показывают то, что лежит за Canvas, обычно фон).
    ///
    /// Использование:
    /// 1. Под твоим Canvas создай пустой GameObject ContentRoot (UI → empty).
    /// 2. Перенеси под ContentRoot ВСЕ существующие панели (MainScreenPanel,
    ///    CookingScreenPanel, StoreScreenPanel, WheelScreenPanel, OrderResultPopup,
    ///    TabBar, OnboardingOverlay и т.д.).
    /// 3. На Canvas повесь этот скрипт. В поле Content Root перетащи ContentRoot.
    /// 4. CanvasScaler оставь в Scale With Screen Size, Reference Res 375×812 —
    ///    скрипт перепишет настройки RectTransform у ContentRoot, остальное не трогает.
    [ExecuteAlways]
    public class CanvasFitScaler : MonoBehaviour
    {
        [Tooltip("RectTransform, в котором лежат все панели UI. Скрипт фиксирует его " +
                 "размер на referenceResolution и масштабирует чтобы вписаться в Canvas.")]
        public RectTransform contentRoot;

        [Tooltip("Дизайн-разрешение, в котором ты делал верстку. UI будет всегда " +
                 "выглядеть как при этом разрешении, только зум меняется.")]
        public Vector2 referenceResolution = new Vector2(375, 812);

        private void OnEnable() => Apply();
        private void Update() => Apply();

        private void Apply()
        {
            if (contentRoot == null) return;

            // 1. ContentRoot всегда фиксированного размера, центрирован.
            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot     = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = referenceResolution;

            // 2. Считаем масштаб «fit» — чтобы 375×812 поместилось в Canvas
            //    без обрезки. По меньшей оси заполняем, по большей появятся полосы.
            var canvasRT = transform as RectTransform;
            if (canvasRT == null) return;
            float canvasW = canvasRT.rect.width;
            float canvasH = canvasRT.rect.height;
            if (canvasW <= 0f || canvasH <= 0f) return;

            float sx = canvasW / referenceResolution.x;
            float sy = canvasH / referenceResolution.y;
            float scale = Mathf.Min(sx, sy);

            contentRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
