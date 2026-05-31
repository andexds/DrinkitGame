using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M2: hold-to-fill, отпускай в зелёной зоне.
    public class M2MilkSteamingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Image fillImage;              // image с Image Type=Filled, Vertical
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

        private void Awake()
        {
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
            if (fillImage != null) fillImage.fillAmount = _fill;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null || fillImage == null) return;
            var rect = fillImage.rectTransform.rect;
            float zoneH = _zoneWidth * rect.height;
            float yCenter = -rect.height * 0.5f + zoneCenter * rect.height;
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