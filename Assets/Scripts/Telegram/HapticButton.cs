using DrinkitGame.Telegram;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Telegram
{
    /// Простой компонент: подвешивается на любую UI-кнопку и эмитит хаптик при тапе.
    /// По умолчанию — selectionChanged (лёгкий «клик»). Меняй в инспекторе для разных вкусов.
    [RequireComponent(typeof(Button))]
    public class HapticButton : MonoBehaviour
    {
        public enum HapticKind
        {
            Selection,        // лёгкий «клик» — большинство кнопок
            ImpactLight,
            ImpactMedium,
            ImpactHeavy,
            ImpactSoft,
            ImpactRigid,
            Success,
            Error,
            Warning
        }

        [Tooltip("Тип хаптика на тап. Selection = стандарт для всех кнопок.")]
        public HapticKind kind = HapticKind.Selection;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Trigger);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Trigger);
        }

        private void Trigger()
        {
            switch (kind)
            {
                case HapticKind.Selection:   TelegramHaptics.Selection(); break;
                case HapticKind.ImpactLight: TelegramHaptics.Light(); break;
                case HapticKind.ImpactMedium:TelegramHaptics.Medium(); break;
                case HapticKind.ImpactHeavy: TelegramHaptics.Heavy(); break;
                case HapticKind.ImpactSoft:  TelegramHaptics.Soft(); break;
                case HapticKind.ImpactRigid: TelegramHaptics.Rigid(); break;
                case HapticKind.Success:     TelegramHaptics.Success(); break;
                case HapticKind.Error:       TelegramHaptics.Error(); break;
                case HapticKind.Warning:     TelegramHaptics.Warning(); break;
            }
        }
    }
}
