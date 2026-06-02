using DrinkitGame.Telegram;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Telegram
{
    /// Запускается один раз при загрузке сцены и навешивает HapticButton на ВСЕ
    /// Button-компоненты в сцене (включая выключенные/динамически появляющиеся),
    /// если у них ещё нет HapticButton. Удобно: не надо вручную тыкать в каждую кнопку.
    /// Повесь один экземпляр этого компонента на GameRoot или Canvas.
    public class HapticAutoInstaller : MonoBehaviour
    {
        [Tooltip("Какой стиль хаптика навешивать по умолчанию (если на кнопке нет HapticButton).")]
        public HapticButton.HapticKind defaultKind = HapticButton.HapticKind.Selection;

        [Tooltip("Сканировать только под этим корнем (если задан). Если null — вся сцена.")]
        public Transform rootScope;

        private void Start()
        {
            InstallAll();
        }

        /// Можно вызвать вручную после спавна новых кнопок (например, после Rebuild магазина).
        public void InstallAll()
        {
            Button[] buttons;
            if (rootScope != null)
                buttons = rootScope.GetComponentsInChildren<Button>(true);
            else
                buttons = FindObjectsOfType<Button>(true);

            int added = 0;
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                if (btn.GetComponent<HapticButton>() != null) continue;
                var h = btn.gameObject.AddComponent<HapticButton>();
                h.kind = defaultKind;
                added++;
            }
            if (added > 0) Debug.Log($"[HapticAutoInstaller] Добавил HapticButton к {added} кнопкам.");
        }
    }
}
