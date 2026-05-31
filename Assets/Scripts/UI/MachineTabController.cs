using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    public class MachineTabController : MonoBehaviour
    {
        [Header("Current card")]
        public TMP_Text currentTitle;
        public TMP_Text currentDescription;
        public Image currentImage;

        [Header("Next card")]
        public GameObject nextCardRoot;
        public TMP_Text nextTitle;
        public TMP_Text questLine;
        public TMP_Text priceLine;
        public Button buyButton;
        public TMP_Text buyButtonLabel;

        private GameStateManager _gsm;

        private void OnEnable()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;
            _gsm.Economy.BalanceChanged += _ => Refresh();
            _gsm.Quests.CountChanged += (_, __) => Refresh();
            _gsm.Machine.Upgraded += _ => Refresh();
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            Refresh();
        }

        private void OnDisable()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuy);
        }

        private void Refresh()
        {
            var cur = _gsm.Machine.CurrentTier;
            if (cur != null && currentTitle != null)
                currentTitle.text = $"Текущая: T{cur.tierIndex} — {cur.displayName}";
            if (cur != null && currentDescription != null)
                currentDescription.text =
                    $"Помол: зона {cur.grindingZoneWidth:0.00}. Экстракция: {cur.extractionTimeSeconds:0.0} сек." +
                    (cur.checkBonusPercent > 0 ? $" Бонус +{cur.checkBonusPercent}%." : "");
            if (cur != null && cur.icon != null && currentImage != null)
                currentImage.sprite = cur.icon;

            var next = _gsm.Machine.NextTier;
            if (next == null)
            {
                if (nextCardRoot != null) nextCardRoot.SetActive(false);
                return;
            }
            if (nextCardRoot != null) nextCardRoot.SetActive(true);

            if (nextTitle != null) nextTitle.text = $"Следующая: T{next.tierIndex} — {next.displayName}";

            var availability = _gsm.Machine.GetUpgradeAvailability();
            string questText = "";
            if (next.questTargetRecipe1 != null && next.questTargetCount1 > 0)
                questText += $"{next.questDescription}: {_gsm.Quests.GetSoldCount(next.questTargetRecipe1.id)} / {next.questTargetCount1}";
            if (next.questTargetRecipe2 != null && next.questTargetCount2 > 0)
                questText += $" + {_gsm.Quests.GetSoldCount(next.questTargetRecipe2.id)} / {next.questTargetCount2}";
            if (questLine != null) questLine.text = questText;

            if (priceLine != null) priceLine.text = $"Цена: {next.purchasePrice} ₽";

            string buyText = availability switch
            {
                UpgradeAvailability.Available => "Купить",
                UpgradeAvailability.NotEnoughMoney => "Не хватает денег",
                UpgradeAvailability.QuestIncomplete => "Выполни квест",
                _ => "—"
            };
            if (buyButtonLabel != null) buyButtonLabel.text = buyText;
            if (buyButton != null) buyButton.interactable = availability == UpgradeAvailability.Available;
        }

        private void OnBuy()
        {
            bool ok = _gsm.Machine.TryUpgrade();
            Debug.Log($"[Store] Upgrade: {(ok ? "успех" : "неудача")}");
            Refresh();
        }
    }
}