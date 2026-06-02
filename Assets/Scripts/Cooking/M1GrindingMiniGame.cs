using System;
using DrinkitGame.Data;
using DrinkitGame.Telegram;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M1: помол кофе.
    /// Индикатор движется по горизонтальной полосе. Тап → останавливается → quality по близости к центру зелёной зоны.
    public class M1GrindingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public RectTransform bar;            // горизонтальная полоса
        public RectTransform indicator;      // движущийся индикатор
        public RectTransform greenZone;      // подсветка зелёной зоны
        public Button stopButton;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float indicatorSpeed = 0.8f;  // полных проходов в секунду
        public float zoneCenter = 0.5f;      // центр зелёной зоны (0..1)

        public event Action<float> Completed;

        private float _zoneWidth;
        private float _position;             // 0..1
        private int _direction = 1;
        private bool _running;

        private void Awake()
        {
            if (stopButton != null) stopButton.onClick.AddListener(OnStop);
        }

        public void Begin(MachineTierDefinition tier)
        {
            _zoneWidth = tier != null ? tier.grindingZoneWidth : 0.25f;
            _position = 0f;
            _direction = 1;
            _running = true;
            UpdateZoneVisuals();
            UpdateIndicator();
            if (instructionLabel != null) instructionLabel.text = "Тапни когда индикатор в зелёной зоне!";
        }

        private void Update()
        {
            if (!_running) return;
            _position += _direction * indicatorSpeed * Time.deltaTime;
            if (_position >= 1f) { _position = 1f; _direction = -1; }
            else if (_position <= 0f) { _position = 0f; _direction = 1; }
            UpdateIndicator();
        }

        private void OnStop()
        {
            if (!_running) return;
            _running = false;
            float quality = MiniGameQuality.FromZoneHit(_position, zoneCenter, _zoneWidth);
            // Хаптик по результату: попал зелено → success; жёлто → light; красно → warning
            if (quality >= 80f) TelegramHaptics.Success();
            else if (quality >= 50f) TelegramHaptics.Light();
            else TelegramHaptics.Warning();
            Completed?.Invoke(quality);
        }

        private void UpdateIndicator()
        {
            if (bar == null || indicator == null) return;
            float barWidth = bar.rect.width;
            float x = -barWidth * 0.5f + _position * barWidth;
            indicator.anchoredPosition = new Vector2(x, indicator.anchoredPosition.y);
        }

        private void UpdateZoneVisuals()
        {
            if (bar == null || greenZone == null) return;
            float barWidth = bar.rect.width;
            float zoneW = _zoneWidth * barWidth;
            float x = -barWidth * 0.5f + zoneCenter * barWidth;
            greenZone.anchoredPosition = new Vector2(x, greenZone.anchoredPosition.y);
            greenZone.sizeDelta = new Vector2(zoneW, greenZone.sizeDelta.y);
        }
    }
}