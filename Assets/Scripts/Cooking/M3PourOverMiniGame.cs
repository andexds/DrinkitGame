using System;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M3: long-tap нужной длительности.
    /// Игрок удерживает кнопку. Прогресс растёт. Цель — отпустить в "зелёной зоне" длительности.
    public class M3PourOverMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Image progressFill;
        public RectTransform greenZoneOverlay;
        public Button holdButton;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float targetDuration = 3f;    // секунды
        public float maxDuration = 5f;

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _heldSeconds;
        private bool _running;
        private bool _holding;

        private void Awake()
        {
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
            _zoneWidth = tier != null ? tier.pourOverZoneWidth : 0.25f;
            _heldSeconds = 0f;
            _running = true;
            _holding = false;
            UpdateZoneOverlay();
            UpdateProgress();
            if (instructionLabel != null) instructionLabel.text = "Удерживай нужное время";
        }

        private void Update()
        {
            if (!_running) return;
            if (_holding)
            {
                _heldSeconds = Mathf.Min(maxDuration, _heldSeconds + Time.deltaTime);
                UpdateProgress();
                if (_heldSeconds >= maxDuration) OnRelease();
            }
        }

        private void OnRelease()
        {
            if (!_running) return;
            _holding = false;
            _running = false;
            float position = _heldSeconds / maxDuration; // 0..1
            float zoneCenter = targetDuration / maxDuration;
            float quality = MiniGameQuality.FromZoneHit(position, zoneCenter, _zoneWidth);
            Completed?.Invoke(quality);
        }

        private void UpdateProgress()
        {
            if (progressFill != null) progressFill.fillAmount = _heldSeconds / maxDuration;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null || progressFill == null) return;
            var rect = progressFill.rectTransform.rect;
            float w = _zoneWidth * rect.width;
            float zoneCenter = targetDuration / maxDuration;
            float x = -rect.width * 0.5f + zoneCenter * rect.width;
            greenZoneOverlay.anchoredPosition = new Vector2(x, greenZoneOverlay.anchoredPosition.y);
            greenZoneOverlay.sizeDelta = new Vector2(w, greenZoneOverlay.sizeDelta.y);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => action(d));
            trigger.triggers.Add(entry);
        }
    }
}