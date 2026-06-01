using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M2: hold-to-fill, отпускай в зелёной зоне.
    /// Налив реализован через изменение sizeDelta.y RectTransform'а (не Image.fillAmount),
    /// чтобы скруглённые углы спрайта (Sliced или Simple) не резались.
    /// Pivot Image должен стоять снизу (Y=0), тогда полоска растёт снизу вверх.
    public class M2MilkSteamingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        [Tooltip("Image полоски молока. Image Type = Simple или Sliced (НЕ Filled). " +
                 "Pivot RectTransform = (0.5, 0) — снизу. " +
                 "Размер в инспекторе = полная высота полоски (запоминается на Awake).")]
        public Image fillImage;
        public RectTransform greenZoneOverlay;
        public Button holdButton;            // нужен EventTrigger для PointerDown/Up
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float fillSpeed = 1.5f;       // долей в секунду
        public float zoneCenter = 0.7f;      // 0..1
        public float maxFill = 1f;

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _fill;
        private bool _running;
        private bool _holding;
        private Vector2 _fullSize; // запомненный sizeDelta полоски при старте сцены

        private void Awake()
        {
            // Запоминаем «полный размер» полоски — то, что задано в инспекторе.
            // Дальше масштабируем по высоте от 0 до _fullSize.y.
            if (fillImage != null)
                _fullSize = fillImage.rectTransform.sizeDelta;

            // Добавляем EventTrigger программно
            if (holdButton != null)
            {
                var trigger = holdButton.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = holdButton.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, _ => _holding = true);
                AddTrigger(trigger, EventTriggerType.PointerUp, _ => OnRelease());
                AddTrigger(trigger, EventTriggerType.PointerExit, _ => { if (_holding) OnRelease(); });
            }
        }

        public void Begin(MachineTierDefinition tier)
        {
            _zoneWidth = tier != null ? tier.milkSteamingZoneWidth : 0.25f;
            _fill = 0f;
            _running = true;
            _holding = false;
            UpdateZoneOverlay();
            UpdateFill();
            if (instructionLabel != null) instructionLabel.text = "Удерживай, отпусти в зелёной зоне";
        }

        private void Update()
        {
            if (!_running) return;
            if (_holding)
            {
                _fill = Mathf.Min(maxFill, _fill + fillSpeed * Time.deltaTime);
                UpdateFill();
                if (_fill >= maxFill) OnRelease(); // авто-стоп при переполнении
            }
        }

        private void OnRelease()
        {
            if (!_running) return;
            _holding = false;
            _running = false;
            float quality = MiniGameQuality.FromZoneHit(_fill, zoneCenter, _zoneWidth);
            Completed?.Invoke(quality);
        }

        private void UpdateFill()
        {
            if (fillImage == null) return;
            // Меняем только высоту: ширина остаётся как в инспекторе.
            var size = _fullSize;
            size.y = _fullSize.y * _fill;
            fillImage.rectTransform.sizeDelta = size;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null) return;
            // Используем _fullSize, а не текущий rect полоски (он меняется при наливе).
            // ВАЖНО: greenZoneOverlay должен быть сиблингом fillImage (на том же контейнере),
            // а не его дочерним — иначе при смене размера fillImage зона тоже поедет.
            float zoneH = _zoneWidth * _fullSize.y;
            float yCenter = -_fullSize.y * 0.5f + zoneCenter * _fullSize.y;
            greenZoneOverlay.anchoredPosition = new Vector2(greenZoneOverlay.anchoredPosition.x, yCenter);
            greenZoneOverlay.sizeDelta = new Vector2(greenZoneOverlay.sizeDelta.x, zoneH);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => action(d));
            trigger.triggers.Add(entry);
        }
    }
}