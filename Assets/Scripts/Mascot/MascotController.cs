using System.Collections;
using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.Mascot
{
    /// Один визуал для одной эмоции. Фолбэк: анимация → статика → цвет-плейсхолдер.
    /// Художник заполняет столько, сколько успел; остальные эмоции работают со старым плейсхолдером.
    [System.Serializable]
    public class EmotionVisual
    {
        public MascotEmotion emotion;

        [Tooltip("Статичная картинка. Используется если массив кадров пуст.")]
        public Sprite staticSprite;

        [Tooltip("Кадры анимации. Если массив не пуст — играется циклически, перебивая staticSprite.")]
        public Sprite[] animationFrames;

        [Range(1f, 24f)]
        [Tooltip("Скорость анимации, кадров в секунду.")]
        public float fps = 6f;
    }

    /// Управляет визуалом и эмоциями маскота Дринчика.
    /// Висит на DrinchikPlaceholder GameObject (или его контейнере).
    /// Если для эмоции загружен спрайт/анимация — показывает их; иначе цветной плейсхолдер.
    public class MascotController : MonoBehaviour
    {
        [Header("Visual placeholders (until real art)")]
        public Image bodyImage;
        public TMP_Text bodyLabel;    // плейсхолдер: текст эмоции внутри квадрата

        [Header("Art per emotion (sprites override colors)")]
        [Tooltip("По одной записи на эмоцию. Заполняй по мере поступления арта. " +
                 "Эмоции без визуала показываются цветным плейсхолдером.")]
        public List<EmotionVisual> visuals = new();

        [Header("Speech bubble")]
        public GameObject speechBubbleRoot;
        public TMP_Text speechText;
        public float bubbleVisibleSeconds = 3f;

        [Header("Emotion colors (placeholders)")]
        public Color idleColor = new(0.31f, 0.65f, 0.85f);     // 4FA7D9 голубой
        public Color happyColor = new(0.18f, 0.72f, 0.51f);    // зелёный
        public Color excitedColor = new(0.95f, 0.61f, 0.07f);  // оранжевый
        public Color welcomingColor = new(0.49f, 0.36f, 0.85f); // фиолетовый
        public Color sadColor = new(0.30f, 0.39f, 0.55f);      // серо-синий
        public Color disappointedColor = new(0.85f, 0.27f, 0.27f); // красноватый
        public Color pointingColor = new(0.18f, 0.55f, 0.85f); // ярко-синий
        public Color sleepingColor = new(0.51f, 0.51f, 0.51f); // серый

        private GameStateManager _gsm;
        private Coroutine _hideBubbleCoroutine;
        private Coroutine _animCoroutine;

        public MascotEmotion CurrentEmotion { get; private set; } = MascotEmotion.Idle;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            HideBubble();
            SetEmotion(MascotEmotion.Idle);

            if (_gsm == null) return;

            // Подписки на события — Дринчик реагирует
            _gsm.OrderResolution.OrderCompleted += OnOrderCompleted;
            _gsm.Orders.OrderAbandoned += OnOrderAbandoned;
            _gsm.Recipes.RecipeUnlocked += OnRecipeUnlocked;
            _gsm.Machine.Upgraded += OnMachineUpgraded;
            _gsm.Wheel.Spun += OnWheelSpun;
            _gsm.Orders.CannotSpawnNoIngredients += OnCannotSpawn;
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.OrderResolution.OrderCompleted -= OnOrderCompleted;
            _gsm.Orders.OrderAbandoned -= OnOrderAbandoned;
            _gsm.Recipes.RecipeUnlocked -= OnRecipeUnlocked;
            _gsm.Machine.Upgraded -= OnMachineUpgraded;
            _gsm.Wheel.Spun -= OnWheelSpun;
            _gsm.Orders.CannotSpawnNoIngredients -= OnCannotSpawn;
        }

        // Сигнал «нечего готовить» приходит каждые ~3 сек, пока нет зёрен. Троттлим,
        // чтобы Дринчик не повторял фразу постоянно: не чаще раза в 12 сек.
        private float _lastNoIngredientsHintTime = -999f;

        private void OnCannotSpawn()
        {
            if (Time.time - _lastNoIngredientsHintTime < 12f) return;
            _lastNoIngredientsHintTime = Time.time;
            Say("Кончились зёрна! Купи в магазине", MascotEmotion.Pointing);
        }

        public void SetEmotion(MascotEmotion emotion)
        {
            CurrentEmotion = emotion;

            // Останавливаем предыдущую анимацию (если была) — иначе кадры старой эмоции
            // продолжают мигать поверх новой.
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }

            var visual = FindVisual(emotion);
            bool hasAnim = visual != null && visual.animationFrames != null && visual.animationFrames.Length > 0;
            bool hasStatic = visual != null && visual.staticSprite != null;

            if (hasAnim)
            {
                // 1. Анимация (старшая опция)
                ApplySpriteMode();
                // Запускаем корутину только если объект активен (иначе StartCoroutine кинет).
                if (gameObject.activeInHierarchy)
                    _animCoroutine = StartCoroutine(AnimateLoop(visual.animationFrames, visual.fps));
                else if (bodyImage != null)
                    bodyImage.sprite = visual.animationFrames[0]; // хотя бы первый кадр
            }
            else if (hasStatic)
            {
                // 2. Статичный спрайт
                ApplySpriteMode();
                if (bodyImage != null) bodyImage.sprite = visual.staticSprite;
            }
            else
            {
                // 3. Фолбэк на цвет-плейсхолдер (как было)
                if (bodyImage != null)
                {
                    bodyImage.sprite = null;
                    bodyImage.color = ColorForEmotion(emotion);
                }
                if (bodyLabel != null)
                {
                    bodyLabel.gameObject.SetActive(true);
                    bodyLabel.text = LabelForEmotion(emotion);
                }
            }
        }

        /// Готовим Image для показа спрайта: белый цвет (не тонировать), скрываем плейсхолдер-надпись.
        private void ApplySpriteMode()
        {
            if (bodyImage != null) bodyImage.color = Color.white;
            if (bodyLabel != null) bodyLabel.gameObject.SetActive(false);
        }

        private EmotionVisual FindVisual(MascotEmotion e)
        {
            if (visuals == null) return null;
            foreach (var v in visuals)
                if (v != null && v.emotion == e) return v;
            return null;
        }

        private IEnumerator AnimateLoop(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) yield break;
            fps = Mathf.Max(1f, fps);
            var wait = new WaitForSeconds(1f / fps);
            int i = 0;
            while (true)
            {
                if (bodyImage != null && frames[i] != null) bodyImage.sprite = frames[i];
                i = (i + 1) % frames.Length;
                yield return wait;
            }
        }

        public void Say(string text, MascotEmotion emotion = MascotEmotion.Idle)
        {
            SetEmotion(emotion);
            if (speechBubbleRoot == null || speechText == null) return;
            // Если маскот скрыт (мы не на главном экране) — не показываем пузырь и не
            // запускаем корутину: StartCoroutine на неактивном GameObject бросает ошибку.
            if (!gameObject.activeInHierarchy) return;
            speechText.text = text;
            speechBubbleRoot.SetActive(true);

            if (_hideBubbleCoroutine != null) StopCoroutine(_hideBubbleCoroutine);
            _hideBubbleCoroutine = StartCoroutine(HideBubbleAfter(bubbleVisibleSeconds));
        }

        private void OnDisable()
        {
            // Когда маскот скрывается (ушли с главного экрана), корутина авто-скрытия
            // пузыря останавливается Unity и не доживает до HideBubble. Прячем пузырь здесь,
            // иначе он «зависает» видимым при возврате на экран.
            HideBubble();
            _hideBubbleCoroutine = null;
            // Корутина анимации тоже убивается Unity — обнуляем хэндл, чтобы при OnEnable
            // SetEmotion корректно стартанул новую (а не думал, что предыдущая ещё крутится).
            _animCoroutine = null;
        }

        public void HideBubble()
        {
            if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
        }

        private IEnumerator HideBubbleAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideBubble();
            SetEmotion(MascotEmotion.Idle);
            _hideBubbleCoroutine = null;
        }

        // === Реакции на события ===

        private void OnOrderCompleted(OrderResolution res)
        {
            if (res.qualityMultiplier >= 0.20f)
                Say("Топ! Качество — огонь!", MascotEmotion.Happy);
            else if (res.qualityMultiplier <= -0.10f)
                Say("Можем лучше...", MascotEmotion.Disappointed);
        }

        private void OnOrderAbandoned(Order order)
        {
            Say("Клиент ушёл :(", MascotEmotion.Sad);
        }

        private void OnRecipeUnlocked(RecipeDefinition recipe)
        {
            Say($"Открыли «{recipe.displayName}»!", MascotEmotion.Excited);
        }

        private void OnMachineUpgraded(MachineTierDefinition tier)
        {
            Say($"Кофемашина {tier.displayName}! Огонь!", MascotEmotion.Excited);
        }

        private void OnWheelSpun(WheelSectorDefinition sector)
        {
            if (sector.prizeType == WheelPrizeType.Nothing)
                Say("Эх, повезёт в следующий раз", MascotEmotion.Sad);
            else
                Say("Ура! Приз!", MascotEmotion.Excited);
        }

        // === Хелперы ===

        private Color ColorForEmotion(MascotEmotion e)
        {
            return e switch
            {
                MascotEmotion.Happy => happyColor,
                MascotEmotion.Excited => excitedColor,
                MascotEmotion.Welcoming => welcomingColor,
                MascotEmotion.Sad => sadColor,
                MascotEmotion.Disappointed => disappointedColor,
                MascotEmotion.Pointing => pointingColor,
                MascotEmotion.Sleeping => sleepingColor,
                _ => idleColor
            };
        }

        private static string LabelForEmotion(MascotEmotion e)
        {
            // Плейсхолдер-текст внутри квадрата маскота. Только ASCII/кириллица — кастомный
            // SDF-шрифт не содержит эмодзи/стрелок (иначе варнинг "character not found").
            // Заменится спрайтами в Phase 12.
            return e switch
            {
                MascotEmotion.Happy => ":)",
                MascotEmotion.Excited => "!",
                MascotEmotion.Welcoming => "<3",
                MascotEmotion.Sad => ":(",
                MascotEmotion.Disappointed => ":|",
                MascotEmotion.Pointing => "->",
                MascotEmotion.Sleeping => "zzz",
                _ => "Дринчик"
            };
        }
    }
}