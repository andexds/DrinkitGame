using System.Text;
using DrinkitGame.Audio;
using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Поп-ап после выдачи заказа: показывает разбивку чека.
    public class OrderResultPopupController : MonoBehaviour
    {
        public TMP_Text recipeLine;
        public TMP_Text breakdownText;
        public TMP_Text finalLine;
        public Button okButton;

        private void Awake()
        {
            if (okButton != null) okButton.onClick.AddListener(OnOk);
        }

        public void Show(OrderResolution res)
        {
            if (recipeLine != null) recipeLine.text = res.recipeDisplayName;

            var sb = new StringBuilder();
            sb.AppendLine($"База: {res.basePrice} ₽");
            sb.AppendLine($"Скорость ({res.speedLabel}): {FormatPercent(res.speedMultiplier)}");
            sb.AppendLine($"Качество ({res.qualityLabel}): {FormatPercent(res.qualityMultiplier)}");
            if (res.tierBonusMultiplier > 0)
                sb.AppendLine($"Машина T3: {FormatPercent(res.tierBonusMultiplier)}");
            if (res.doubleApplied)
                sb.AppendLine($"×2 буст применён");

            if (breakdownText != null) breakdownText.text = sb.ToString();
            if (finalLine != null) finalLine.text = $"+ {res.finalPayout} ₽";

            // Звук успеха при показе результата.
            if (AudioService.Instance != null) AudioService.Instance.PlaySuccess();
        }

        private void OnOk()
        {
            UIRouter.Instance.HideOrderResult();
        }

        private static string FormatPercent(float mult)
        {
            int pct = Mathf.RoundToInt(mult * 100f);
            return pct >= 0 ? $"+{pct}%" : $"{pct}%";
        }
    }
}