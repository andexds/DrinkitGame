using System.Collections;
using System.Collections.Generic;
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

        [Header("Sectors in visual order")]
        [Tooltip("Сектора в порядке как нарисованы на колесе — ПО ЧАСОВОЙ СТРЕЛКЕ " +
                 "начиная с верхнего (того, что под стрелкой при нулевом повороте). " +
                 "Если выпавший приз не найден в этом списке — спин крутится без точной остановки.")]
        public List<WheelSectorDefinition> sectorsInVisualOrder = new();

        [Tooltip("Случайный разброс внутри сектора (0 = строго в центр, 0.5 = почти на границе). " +
                 "Делает остановку чуть «живее».")]
        [Range(0f, 0.5f)]
        public float sectorJitter = 0.3f;

        private GameStateManager _gsm;
        private bool _spinning;
        private float _currentRotation; // накопленный угол поворота колеса между спинами

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
            if (sectorLabel != null) sectorLabel.text = ""; // ничего не пишем
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

            // Находим индекс выпавшего сектора в визуальном порядке (CW от верхнего).
            int idx = sectorsInVisualOrder.IndexOf(resultSector);
            if (idx < 0)
            {
                Debug.LogWarning(
                    $"[Wheel] Сектор '{resultSector.name}' не в sectorsInVisualOrder — " +
                    "крутим без выравнивания.");
                idx = 0;
            }

            int count = Mathf.Max(1, sectorsInVisualOrder.Count);
            float anglePerSector = 360f / count;

            // Лёгкий случайный разброс внутри сектора, чтобы стрелка не упиралась всегда в центр.
            float jitterRange = anglePerSector * sectorJitter;
            float jitter = Random.Range(-jitterRange, jitterRange);

            // Сектора нарисованы CW от верха. Чтобы сектор K оказался под стрелкой,
            // колесо поворачивается CCW (Z>0 в Unity UI) на idx*anglePerSector + jitter градусов
            // (по модулю 360°).
            float targetMod = (idx * anglePerSector + jitter);
            targetMod = ((targetMod % 360f) + 360f) % 360f;

            // Где колесо стоит сейчас (по модулю 360°).
            float currentMod = ((_currentRotation % 360f) + 360f) % 360f;

            // Сколько повернуть в положительную сторону (CCW), чтобы прийти к targetMod.
            float diff = targetMod - currentMod;
            if (diff <= 0f) diff += 360f; // всегда > 0 для CCW движения

            // Плюс N полных оборотов для эффекта.
            float totalRotation = spinFullRotations * 360f + diff;
            float startAngle = _currentRotation;
            float endAngle = _currentRotation + totalRotation;

            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);
                // ease-out: быстро в начале, медленно в конце
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float angle = Mathf.Lerp(startAngle, endAngle, eased);
                if (wheelImage != null)
                    wheelImage.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }

            // Жёстко выставляем итоговый угол — точно над сектором.
            if (wheelImage != null)
                wheelImage.localRotation = Quaternion.Euler(0, 0, endAngle);
            _currentRotation = endAngle;

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
