using DrinkitGame.Core;
using TMPro;
using UnityEngine;

namespace DrinkitGame.UI
{
    /// Маленький оверлей со статистикой в углу — для отладки/плейтеста.
    public class DebugStatsOverlay : MonoBehaviour
    {
        public TMP_Text statsLabel;
        public bool visibleByDefault = true;

        private GameStateManager _gsm;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (!visibleByDefault && statsLabel != null) statsLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_gsm == null || statsLabel == null || !statsLabel.gameObject.activeSelf) return;
            statsLabel.text =
                $"Balance: {_gsm.Economy.Balance}₽\n" +
                $"Reputation: {_gsm.Reputation.Reputation:F1}\n" +
                $"Machine: T{_gsm.Machine.CurrentTierIndex}\n" +
                $"Orders done: {_gsm.State.totalOrdersCompleted}\n" +
                $"Wheel tokens: {_gsm.Wheel.Tokens}";
        }
    }
}