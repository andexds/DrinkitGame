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

        [Header("Editor Preview (no effect in Play)")]
        [Tooltip("Эмоция для превью в Edit-моде. Меняй в инспекторе — Scene-окно обновится. " +
                 "В Play-моде это поле игнорируется, SetEmotion управляется сервисами.")]
        public MascotEmotion previewEmotion = MascotEmotion.Idle;

        [Header("Speech bubble")]
        public GameObject speechBubbleRoot;
        public TMP_Text speechText;
        public float bubbleVisibleSeconds = 3f;

        [Tooltip("Скрывается, когда показывается пузырь (например, Pill_Goal). " +
                 "Не обязательно — оставь null, если ничего скрывать не нужно.")]
        public GameObject hideWhenBubbleShown;

        [Header("Emotion colors (placeholders)")]
        public Color idleColor = new(0.31f, 0.65f, 0.85f);     // 4FA7D9 голубой
        public Color happyColor = new(0.18f, 0.72f, 0.51f);    // зелёный
        public Color excitedColor = new(0.95f, 0.61f, 0.07f);  // оранжевый
        public Color welcomingColor = new(0.49f, 0.36f, 0.85f); // фиолетовый
        public Color sadColor = new(0.30f, 0.39f, 0.55f);      // серо-синий
        public Color disappointedColor = new(0.85f, 0.27f, 0.27f); // красноватый
        public Color pointingColor = new(0.18f, 0.55f, 0.85f); // ярко-синий
        public Color sleepingColor = new(0.51f, 0.51f, 0.51f); // серый
        public Color angryColor = new(0.95f, 0.20f, 0.20f);    // красный

        [Header("Tap reactions (тапнул по Дринчику)")]
        [Tooltip("Случайная фраза, которую Дринчик скажет в Angry-эмоции при тапе.")]
        public List<string> angryPhrases = new()
        {
            "И зачем ты это сделал?",
            "Хватит в меня тыкать",
            "Сварил бы лучше кофе, люди ждут",
            "Дада, я понял, что тебе делать нечего",
        };

        [Tooltip("Минимальная пауза между тапами по маскоту (сек), чтобы не спамить.")]
        [Range(0.1f, 3f)]
        public float tapCooldown = 0.8f;

        private float _lastTapTime = -999f;

        // Запоминаем последнюю эмоцию которую попросили показать пока маскот был скрыт —
        // отыграем её при возврате на главный экран.
        private MascotEmotion? _queuedEmotion;
        private string _queuedText;

        private GameStateManager _gsm;
        private Coroutine _hideBubbleCoroutine;
        private Coroutine _animCoroutine;

        public MascotEmotion CurrentEmotion { get; private set; } = MascotEmotion.Idle;

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            HideBubble();
            SetEmotion(MascotEmotion.Idle);

            // Делаем bodyImage кликабельной — для tap-to-anger реакции.
            // Явно включаем raycastTarget + Button с явным TargetGraphic.
            if (bodyImage != null)
            {
                bodyImage.raycastTarget = true;
                var btn = bodyImage.GetComponent<Button>();
                if (btn == null) btn = bodyImage.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = bodyImage;
                btn.interactable = true;
                btn.onClick.RemoveListener(OnMascotTapped); // на всякий случай дедуп
                btn.onClick.AddListener(OnMascotTapped);
                Debug.Log($"[Mascot] Tap-button attached to {bodyImage.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[Mascot] bodyImage не назначен — тапы по маскоту работать не будут.");
            }

            if (_gsm == null) return;

            // Подписки на события — Дринчик реагирует
            _gsm.OrderResolution.OrderCompleted += OnOrderCompleted;
            _gsm.Orders.OrderAbandoned += OnOrderAbandoned;
            _gsm.Recipes.RecipeUnlocked += OnRecipeUnlocked;
            _gsm.Machine.Upgraded += OnMachineUpgraded;
            _gsm.Wheel.Spun += OnWheelSpun;
            _gsm.Orders.CannotSpawnNoIngredients += OnCannotSpawn;
        }

        private void OnMascotTapped()
        {
            Debug.Log("[Mascot] Tap detected");
            // Дебаунс — не чаще раза в N сек.
            if (Time.time - _lastTapTime < tapCooldown) return;
            _lastTapTime = Time.time;

            if (angryPhrases == null || angryPhrases.Count == 0)
            {
                Debug.LogWarning("[Mascot] angryPhrases пустой — нечего сказать.");
                return;
            }
            var phrase = angryPhrases[Random.Range(0, angryPhrases.Count)];
            Say(phrase, MascotEmotion.Angry);
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

#if UNITY_EDITOR
        /// Превью в Edit-моде: показывает визуал previewEmotion прямо в Scene-окне,
        /// без запуска Play. Анимация не крутится (в Edit нет Update-цикла), показываем
        /// первый кадр; статика и цвет-плейсхолдер работают полностью.
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // delayCall — чтобы не пытаться менять сцену прямо в OnValidate
            // (Unity иначе кидает warning «SendMessage during OnValidate»).
            UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
        }

        private void ApplyEditorPreview()
        {
            // Объект мог быть уже удалён к моменту срабатывания delayCall.
            if (this == null) return;
            if (Application.isPlaying) return;
            if (bodyImage == null) return;

            var visual = FindVisual(previewEmotion);
            Sprite preview = null;
            if (visual != null)
            {
                if (visual.animationFrames != null && visual.animationFrames.Length > 0)
                    preview = visual.animationFrames[0];
                else
                    preview = visual.staticSprite;
            }

            if (preview != null)
            {
                bodyImage.sprite = preview;
                bodyImage.color = Color.white;
                if (bodyLabel != null) bodyLabel.gameObject.SetActive(false);
            }
            else
            {
                bodyImage.sprite = null;
                bodyImage.color = ColorForEmotion(previewEmotion);
                if (bodyLabel != null)
                {
                    bodyLabel.gameObject.SetActive(true);
                    bodyLabel.text = LabelForEmotion(previewEmotion);
                }
            }

            // Помечаем сцену как изменённую, чтобы Unity её сохранил.
            UnityEditor.EditorUtility.SetDirty(bodyImage);
        }
#endif

        public void Say(string text, MascotEmotion emotion = MascotEmotion.Idle)
        {
            // Если маскот скрыт (мы на другом экране) — запоминаем последний вызов,
            // отыграем его на OnEnable когда вернёмся. Так Happy/Excited/Sad с других
            // экранов всё-таки доходят до игрока.
            if (!gameObject.activeInHierarchy)
            {
                _queuedEmotion = emotion;
                _queuedText = text;
                return;
            }
            if (speechBubbleRoot == null || speechText == null) return;

            SetEmotion(emotion);
            speechText.text = text;
            speechBubbleRoot.SetActive(true);
            // Скрываем перекрывающий UI (Pill_Goal и т.п.) на время показа пузыря.
            if (hideWhenBubbleShown != null) hideWhenBubbleShown.SetActive(false);

            if (_hideBubbleCoroutine != null) StopCoroutine(_hideBubbleCoroutine);
            _hideBubbleCoroutine = StartCoroutine(HideBubbleAfter(bubbleVisibleSeconds));
        }

        private void OnEnable()
        {
            // Если пока маскот был скрыт прилетело событие с эмоцией (Happy/Excited/Sad
            // от других экранов) — отыграем его сейчас. Иначе сбрасываем в Idle.
            if (_queuedEmotion.HasValue)
            {
                var e = _queuedEmotion.Value;
                var t = _queuedText;
                _queuedEmotion = null;
                _queuedText = null;
                // Отложим на следующий кадр, чтобы дать сцене дорисоваться.
                StartCoroutine(SayQueuedNextFrame(t, e));
            }
            else
            {
                SetEmotion(MascotEmotion.Idle);
            }
        }

        private IEnumerator SayQueuedNextFrame(string text, MascotEmotion emotion)
        {
            yield return null; // ждём 1 кадр чтобы CookingScreenPanel.SetActive(false) долетел
            if (gameObject.activeInHierarchy) Say(text, emotion);
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
            // Возвращаем перекрытый UI обратно.
            if (hideWhenBubbleShown != null) hideWhenBubbleShown.SetActive(true);
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
                MascotEmotion.Angry => angryColor,
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
                MascotEmotion.Angry => ">:(",
                _ => "Дринчик"
            };
        }
    }
}