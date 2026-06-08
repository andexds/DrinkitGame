using DrinkitGame.Audio;
using DrinkitGame.Telegram;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Telegram
{
    /// Простой компонент: подвешивается на любую UI-кнопку и эмитит хаптик + звук при тапе.
    /// По дефолту — Selection хаптик + Click звук. Меняй в инспекторе под исключения.
    [RequireComponent(typeof(Button))]
    public class HapticButton : MonoBehaviour
    {
        public enum HapticKind
        {
            None,
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

        public enum SoundKind
        {
            Click,            // дефолт — Click.mp3
            None,
            CoffeeMachine,
            Focus,
            NewServe,
            RightWay,
            Success
        }

        [Tooltip("Тип хаптика на тап. Selection = стандарт.")]
        public HapticKind kind = HapticKind.Selection;

        [Tooltip("Звук при тапе. Click = по умолчанию. None — тишина.")]
        public SoundKind sound = SoundKind.Click;

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
            // Haptic
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
                // None — ничего не делаем
            }

            // Sound
            var audio = AudioService.Instance;
            if (audio == null) return;
            switch (sound)
            {
                case SoundKind.Click:         audio.PlayClick(); break;
                case SoundKind.CoffeeMachine: audio.PlayCoffeeMachine(); break;
                case SoundKind.Focus:         audio.PlayFocus(); break;
                case SoundKind.NewServe:      audio.PlayNewServe(); break;
                case SoundKind.RightWay:      audio.PlayRightWay(); break;
                case SoundKind.Success:       audio.PlaySuccess(); break;
                // None — тишина
            }
        }
    }
}
