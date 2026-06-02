using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Динамически меняет Canvas Scaler.matchWidthOrHeight в зависимости от
    /// аспекта экрана так, чтобы UI всегда «влезал» в reference resolution
    /// без обрезок. На экранах с другим аспектом по одной из осей будет
    /// свободное место — оно покажет то, что лежит позади Canvas (фон сцены).
    ///
    /// Привяжи на Canvas (рядом с Canvas Scaler). Canvas Scaler должен быть в
    /// Scale With Screen Size, Reference Resolution — твоя дизайн-база.
    ///
    /// ⚠️ Это НЕ строгий letterbox: элементы, заякоренные к краям экрана,
    /// остаются у краёв (т.е. могут визуально «смещаться» при сильно отличном
    /// аспекте). Для большинства телефонов разница ~5 пикселей, не критично.
    /// Если нужен абсолютно одинаковый UI на любом девайсе — см. README по
    /// ContentRoot-подходу (требует ручного refactor сцены).
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public class CanvasFitScaler : MonoBehaviour
    {
        private CanvasScaler _scaler;

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            ApplyMatch();
        }

        private void Update() => ApplyMatch();

        private void ApplyMatch()
        {
            if (_scaler == null) return;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            float sw = UnityEngine.Screen.width;
            float sh = UnityEngine.Screen.height;
            if (sw <= 0f || sh <= 0f) return;

            float screenAspect = sw / sh;
            float refAspect = _scaler.referenceResolution.x / _scaler.referenceResolution.y;
            float match = screenAspect > refAspect ? 1f : 0f;
            if (!Mathf.Approximately(_scaler.matchWidthOrHeight, match))
                _scaler.matchWidthOrHeight = match;
        }
    }
}
