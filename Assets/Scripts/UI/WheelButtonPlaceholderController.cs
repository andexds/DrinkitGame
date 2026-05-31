using DrinkitGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Кнопка колеса на главном экране — показывает счётчик жетонов и открывает экран колеса.
    [RequireComponent(typeof(Button))]
    public class WheelButtonPlaceholderController : MonoBehaviour
    {
        public TMP_Text tokenBadge;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => UIRouter.Instance?.OpenWheel());

            var gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                gsm.Wheel.TokensChanged += OnTokensChanged;
                OnTokensChanged(gsm.Wheel.Tokens);
            }
        }

        private void OnDestroy()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null) gsm.Wheel.TokensChanged -= OnTokensChanged;
        }

        private void OnTokensChanged(int count)
        {
            if (tokenBadge != null)
            {
                tokenBadge.text = count > 0 ? count.ToString() : "";
                tokenBadge.gameObject.SetActive(count > 0);
            }
        }
    }
}