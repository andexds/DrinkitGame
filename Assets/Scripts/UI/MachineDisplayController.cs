using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Отображает текущий тир кофемашины (текст + спрайт) и реагирует на прокачку.
    public class MachineDisplayController : MonoBehaviour
    {
        [Tooltip("Текстовая подпись 'Кофемашина T1'")]
        public TMP_Text tierLabel;

        [Tooltip("Картинка машины. Source Image возьмётся из MachineTierDefinition.icon если задан, иначе оставляем плейсхолдер-цвет.")]
        public Image machineImage;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Machine.Upgraded += OnUpgraded;
            Refresh(_gsm.Machine.CurrentTier);
        }

        private void OnDestroy()
        {
            if (_gsm != null) _gsm.Machine.Upgraded -= OnUpgraded;
        }

        private void OnUpgraded(MachineTierDefinition newTier) => Refresh(newTier);

        private void Refresh(MachineTierDefinition tier)
        {
            if (tier == null) return;
            if (tierLabel != null)
                tierLabel.text = $"Кофемашина T{tier.tierIndex}" +
                                 (string.IsNullOrEmpty(tier.displayName) ? "" : $" — {tier.displayName}");
            if (machineImage != null && tier.icon != null)
                machineImage.sprite = tier.icon;
        }
    }
}