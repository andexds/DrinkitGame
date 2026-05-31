using System.Collections;
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Контроллер экрана колеса удачи. Анимирует спин и показывает приз.
    public class WheelScreenController : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text tokensLabel;
        public RectTransform wheelImage;
        public TMP_Text sectorLabel;
        public TMP_Text resultLabel;
        public Button spinButton;
        public TMP_Text spinButtonLabel;

        [Header("Animation")]
        public float spinDuration = 2.0f;
        public float spinFullRotations = 4f;

        private GameStateManager _gsm;
        private bool _spinning;

        private void Awake()
        {
            if (spinButton != null) spinButton.onClick.AddListener(OnSpin);
        }

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            _gsm.Wheel.TokensChanged += OnTokensChanged;
            RefreshTokens(_gsm.Wheel.Tokens);
            if (resultLabel != null) resultLabel.text = "";
            if (sectorLabel != null) sectorLabel.text = "?";
        }

        private void OnDisable()
        {
            if (_gsm != null) _gsm.Wheel.TokensChanged -= OnTokensChanged;
        }

        private void OnTokensChanged(int newCount) => RefreshTokens(newCount);

        private void RefreshTokens(int count)
        {
            if (tokensLabel != null) tokensLabel.text = $"Жетоны: {count}";
            if (spinButton != null) spinButton.interactable = count > 0 && !_spinning;
        }

        private void OnSpin()
        {
            if (_spinning) return;
            var sector = _gsm.Wheel.TrySpin();
            if (sector == null) return;
            StartCoroutine(AnimateSpin(sector));
        }

        private IEnumerator AnimateSpin(WheelSectorDefinition resultSector)
        {
            _spinning = true;
            if (spinButton != null) spinButton.interactable = false;
            if (resultLabel != null) resultLabel.text = "";
            if (sectorLabel != null) sectorLabel.text = "...";

            float elapsed = 0f;
            float totalAngle = spinFullRotations * 360f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);
                // ease-out: быстро в начале, медленно в конце
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float angle = totalAngle * eased;
                if (wheelImage != null)
                    wheelImage.localRotation = Quaternion.Euler(0, 0, -angle);
                yield return null;
            }

            if (sectorLabel != null) sectorLabel.text = resultSector.displayLabel;
            if (resultLabel != null) resultLabel.text = FormatResult(resultSector);
            _spinning = false;
            if (spinButton != null) spinButton.interactable = _gsm.Wheel.Tokens > 0;
        }

        private static string FormatResult(WheelSectorDefinition s)
        {
            switch (s.prizeType)
            {
                case WheelPrizeType.Coins: return $"+ {s.coinsAmount} ₽";
                case WheelPrizeType.IngredientPack:
                    return $"+ {s.displayLabel}";
                case WheelPrizeType.DiscountVoucher: return "Получен ваучер -50%";
                case WheelPrizeType.DoubleNextOrder: return "Получен буст ×2 заказ";
                case WheelPrizeType.Nothing: return "Не повезло :(";
                default: return s.displayLabel;
            }
        }
    }
}