using System.Collections.Generic;
using DrinkitGame.Core;
using DrinkitGame.Mascot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Простой 5-шаговый онбординг для первого запуска.
    /// Hash шагов — переход по кнопке "Дальше" ИЛИ по ивенту сервиса (например, первый OrderCompleted).
    public class OnboardingController : MonoBehaviour
    {
        public enum StepTrigger
        {
            NextButton,                 // ждём клик "Дальше"
            FirstOrderSpawned,          // ждём первый OrderSpawned
            FirstOrderCompleted         // ждём первый OrderCompleted
        }

        [System.Serializable]
        public class Step
        {
            public string text;
            public StepTrigger trigger;
            public MascotEmotion emotion = MascotEmotion.Welcoming;
        }

        [Header("Refs")]
        public TMP_Text textLabel;
        public Button nextButton;
        public MascotController mascot;

        [Header("Steps (in order)")]
        public List<Step> steps = new();

        private GameStateManager _gsm;
        private int _currentIndex = -1;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);

            // Шаги по умолчанию (если не задано в инспекторе)
            if (steps.Count == 0)
            {
                steps.Add(new Step { text = "Привет! Я Дринчик. Это твоя кофейня. Готов?",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Welcoming });
                steps.Add(new Step { text = "Сюда будут приходить заказы. Подожди первый клиент.",
                                      trigger = StepTrigger.FirstOrderSpawned,
                                      emotion = MascotEmotion.Pointing });
                steps.Add(new Step { text = "Тапни по заказу и приготовь его!",
                                      trigger = StepTrigger.FirstOrderCompleted,
                                      emotion = MascotEmotion.Pointing });
                steps.Add(new Step { text = "Отлично! За деньги покупай рецепты и прокачивай машину в «Магазине».",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Happy });
                steps.Add(new Step { text = "А ещё — крути колесо удачи! Держи бесплатный жетон.",
                                      trigger = StepTrigger.NextButton,
                                      emotion = MascotEmotion.Excited });
            }
        }

        private void Start()
        {
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            if (_gsm.State.onboardingCompleted)
            {
                gameObject.SetActive(false);
                return;
            }

            // Подписки
            _gsm.Orders.OrderSpawned += OnOrderSpawned;
            _gsm.OrderResolution.OrderCompleted += OnOrderCompleted;

            BeginNextStep();
        }

        private void OnDestroy()
        {
            if (_gsm == null) return;
            _gsm.Orders.OrderSpawned -= OnOrderSpawned;
            _gsm.OrderResolution.OrderCompleted -= OnOrderCompleted;
        }

        private void BeginNextStep()
        {
            _currentIndex++;
            if (_currentIndex >= steps.Count)
            {
                FinishOnboarding();
                return;
            }

            var step = steps[_currentIndex];

            // Заказ мог появиться, пока игрок читал прошлый шаг (слоты заполняются сами,
            // либо восстановились из сейва до подписки). Тогда шаг "жди заказ" уже выполнен.
            if (step.trigger == StepTrigger.FirstOrderSpawned && AnyOrderPresent())
            {
                BeginNextStep();
                return;
            }

            if (textLabel != null) textLabel.text = step.text;
            if (mascot != null) mascot.Say(step.text, step.emotion);

            // Нарративные шаги (кнопка "Дальше") — модальный затемнённый оверлей.
            // Игровые шаги (жди заказ / тапни заказ) — оверлей скрыт, подсказывает Дринчик,
            // чтобы не перекрывать ни заказы, ни экран готовки.
            bool modal = step.trigger == StepTrigger.NextButton;
            if (nextButton != null) nextButton.gameObject.SetActive(modal);
            gameObject.SetActive(modal);
        }

        private bool AnyOrderPresent()
        {
            if (_gsm == null || _gsm.Orders == null) return false;
            for (int i = 0; i < OrderService.SlotCount; i++)
                if (_gsm.Orders.GetSlot(i) != null) return true;
            return false;
        }

        private void OnNext()
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.NextButton) return;

            // На предпоследнем шаге (выдача бесплатного жетона) — даём жетон
            if (_currentIndex == steps.Count - 1)
            {
                _gsm.Wheel.GrantStarterToken();
            }

            BeginNextStep();
        }

        private void OnOrderSpawned(Order order)
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.FirstOrderSpawned) return;
            BeginNextStep();
        }

        private void OnOrderCompleted(OrderResolution res)
        {
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;
            if (steps[_currentIndex].trigger != StepTrigger.FirstOrderCompleted) return;
            BeginNextStep();
        }

        private void FinishOnboarding()
        {
            if (_gsm != null)
            {
                _gsm.State.onboardingCompleted = true;
                _gsm.Save.Save(_gsm.State);
            }
            gameObject.SetActive(false);
        }
    }
}