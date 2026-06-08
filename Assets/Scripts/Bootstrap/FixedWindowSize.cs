using UnityEngine;

namespace DrinkitGame.Bootstrap
{
    /// Принудительно держит окно standalone-сборки в фиксированных пикселях
    /// (по умолчанию 375×812 — портрет под Telegram-формат).
    ///
    /// В Editor / WebGL / мобиле не делает ничего — там размер контролируют
    /// другие механизмы (Game View, страница в Telegram, экран телефона).
    ///
    /// Привяжи на GameRoot (или любой постоянный GameObject в сцене Main).
    public class FixedWindowSize : MonoBehaviour
    {
        [Tooltip("Ширина окна в логических пикселях.")]
        public int width = 375;

        [Tooltip("Высота окна в логических пикселях.")]
        public int height = 812;

        private void Start()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            // На Mac размер окна может «прыгать» от Retina-скейлинга — повторим через кадр
            // чтобы перебить любые системные подстройки.
            Invoke(nameof(EnforceAgain), 0.1f);
#endif
        }

        private void EnforceAgain()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            if (Screen.width != width || Screen.height != height)
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
        }
    }
}
