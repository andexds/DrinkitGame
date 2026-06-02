using System;
using DrinkitGame.Data;
using DrinkitGame.Telegram;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Cooking
{
    /// Мини-игра M4: тапай быстро 2 сек, цель — 12 тапов.
    public class M4WhiskingMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        public Button tapButton;
        public TMP_Text counterLabel;
        public TMP_Text timerLabel;
        public TMP_Text instructionLabel;

        [Header("Settings")]
        public float duration = 2f;
        public int targetTaps = 12;

        public event Action<float> Completed;

        private int _taps;
        private float _remaining;
        private bool _running;

        private void Awake()
        {
            if (tapButton != null) tapButton.onClick.AddListener(OnTap);
        }

        public void Begin(MachineTierDefinition tier)
        {
            _taps = 0;
            _remaining = duration;
            _running = true;
            UpdateLabels();
            if (instructionLabel != null) instructionLabel.text = "Тапай быстро!";
        }

        private void Update()
        {
            if (!_running) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                UpdateLabels();
                float quality = MiniGameQuality.FromTapCount(_taps, targetTaps);
                Completed?.Invoke(quality);
                return;
            }
            UpdateLabels();
        }

        private void OnTap()
        {
            if (!_running) return;
            _taps++;
            // Лёгкий тычок на каждый тап — даёт «дрыжь» под пальцем при быстром тапанье.
            TelegramHaptics.Light();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (counterLabel != null) counterLabel.text = $"Тапов: {_taps} / {targetTaps}";
            if (timerLabel != null) timerLabel.text = $"{_remaining:0.0} сек";
        }
    }
}