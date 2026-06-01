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

        [Tooltip("Опционально: Button, при тапе на который открывается магазин " +
                 "на вкладке «Машины». Обычно вешается на корневой объект MachineSection.")]
        public Button openStoreButton;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Machine.Upgraded += OnUpgraded;
            if (openStoreButton != null)
                openStoreButton.onClick.AddListener(OnOpenStoreClicked);
            Refresh(_gsm.Machine.CurrentTier);
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Machine.Upgraded -= OnUpgraded;
            if (openStoreButton != null)
                openStoreButton.onClick.RemoveListener(OnOpenStoreClicked);
        }

        private void OnOpenStoreClicked()
        {
            if (UIRouter.Instance != null)
                UIRouter.Instance.OpenStoreOnTab(StoreTab.Machine);
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