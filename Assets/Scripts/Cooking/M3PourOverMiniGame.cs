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
    /// Прогресс реализован через изменение sizeDelta.x RectTransform'а (не Image.fillAmount),
    /// чтобы скруглённые углы спрайта (Sliced/Simple) не резались.
    /// Pivot Image должен стоять слева (X=0), тогда полоска растёт слева направо.
    public class M3PourOverMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("UI")]
        [Tooltip("Image полоски прогресса. Image Type = Simple или Sliced (НЕ Filled). " +
                 "Pivot RectTransform = (0, 0.5) — слева. " +
                 "Размер в инспекторе = полная ширина полоски (запоминается на Awake).")]
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
        private Vector2 _fullSize; // запомненный sizeDelta полоски при старте сцены

        private void Awake()
        {
            if (progressFill != null)
                _fullSize = progressFill.rectTransform.sizeDelta;

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
            if (progressFill == null) return;
            // Меняем только ширину: высота остаётся как в инспекторе.
            float progress = _heldSeconds / maxDuration;
            var size = _fullSize;
            size.x = _fullSize.x * progress;
            progressFill.rectTransform.sizeDelta = size;
        }

        private void UpdateZoneOverlay()
        {
            if (greenZoneOverlay == null) return;
            // Используем _fullSize, а не текущий rect полоски (он меняется при заливке).
            // ВАЖНО: greenZoneOverlay должен быть сиблингом progressFill (на том же контейнере),
            // а не его дочерним — иначе при смене ширины зона поедет.
            float w = _zoneWidth * _fullSize.x;
            float zoneCenter = targetDuration / maxDuration;
            float x = -_fullSize.x * 0.5f + zoneCenter * _fullSize.x;
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