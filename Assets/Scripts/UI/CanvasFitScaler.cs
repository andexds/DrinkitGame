using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Делает Canvas Scaler «тупо масштабирующим»: что бы ни было на экране,
    /// весь UI заверстаный под reference resolution просто зумится так, чтобы
    /// влезть. Никакого relayout — если экран другой аспект, появятся «полосы»
    /// сверху-снизу или по бокам, но позиции/пропорции элементов остаются как
    /// были в дизайне.
    ///
    /// Привяжи на Canvas (тот же GameObject, где висит Canvas Scaler).
    /// Reference Resolution в Canvas Scaler оставь как у тебя в дизайне
    /// (например 393×852 = iPhone 16 portrait).
    /// Screen Match Mode будет автоматически переключаться между Width и Height.
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways] // обновляется и в Edit Mode, чтобы видеть превью в редакторе
    public class CanvasFitScaler : MonoBehaviour
    {
        private CanvasScaler _scaler;

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            ApplyMatch();
        }

        private void Update()
        {
            ApplyMatch();
        }

        private void ApplyMatch()
        {
            if (_scaler == null) return;
            // Принудительно ставим режим Scale With Screen Size + Match Width Or Height.
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            float screenW = Screen.width;
            float screenH = Screen.height;
            if (screenW <= 0 || screenH <= 0) return;

            float screenAspect = screenW / screenH;
            float refAspect = _scaler.referenceResolution.x / _scaler.referenceResolution.y;

            // Если экран шире (короче) чем дизайн → ограничиваем по высоте (match=1).
            // Если экран уже (длиннее) чем дизайн → ограничиваем по ширине (match=0).
            // Так UI всегда влезает целиком, без обрезок — с полосами по «лишней» оси.
            float match = screenAspect > refAspect ? 1f : 0f;
            // Сглаживаем чтобы не мигало при ресайзе окна в редакторе.
            if (!Mathf.Approximately(_scaler.matchWidthOrHeight, match))
                _scaler.matchWidthOrHeight = match;
        }
    }
}
