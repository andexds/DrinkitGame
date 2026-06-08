using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.Audio
{
    /// Центральный сервис звука. Хранит ссылки на клипы и три AudioSource:
    /// музыка (loop, перезапускается при смене экрана), амбиент (loop, тикают часы
    /// во время готовки), SFX (PlayOneShot для тычков-успехов).
    ///
    /// Висит на отдельном GameObject (например AudioRoot под GameRoot).
    /// Подписывается на UIRouter.ScreenChanged + Orders.OrderSpawned + Wheel.Spun.
    /// Шорткаты типа PlayClick(), PlayCoffeeMachine() — для прямых вызовов из UI.
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        // ===================== Клипы =====================

        [Header("Music (loop)")]
        [Tooltip("Music Loop — играет на главном/магазине/колесе.")]
        public AudioClip musicMain;
        [Tooltip("Cooking — играет на экране готовки.")]
        public AudioClip musicCooking;

        [Header("Ambience (loop)")]
        [Tooltip("Clock — тикающие часы во время готовки. Идёт параллельно с music.")]
        public AudioClip ambienceClock;

        [Header("SFX (one-shot)")]
        [Tooltip("Click — на все кнопки по умолчанию (кроме исключений).")]
        public AudioClip sfxClick;
        [Tooltip("Coffee machine — при тапе на кофемашину во время готовки.")]
        public AudioClip sfxCoffeeMachine;
        [Tooltip("Focus — когда появляется подсказка-хинт при готовке.")]
        public AudioClip sfxFocus;
        [Tooltip("New serve — когда пришёл новый заказ.")]
        public AudioClip sfxNewServe;
        [Tooltip("Right way — когда тапнул на правильный объект во время готовки.")]
        public AudioClip sfxRightWay;
        [Tooltip("Success — попап результата заказа + выигрыш на колесе.")]
        public AudioClip sfxSuccess;

        // ===================== Audio Sources =====================

        [Header("Audio Sources")]
        [Tooltip("AudioSource для музыки. Создай дочерний объект Music с AudioSource, " +
                 "loop=true, playOnAwake=false. Перетащи сюда.")]
        public AudioSource musicSource;
        [Tooltip("AudioSource для амбиента (часы). loop=true, playOnAwake=false.")]
        public AudioSource ambienceSource;
        [Tooltip("AudioSource для one-shot SFX. loop=false, playOnAwake=false.")]
        public AudioSource sfxSource;

        [Tooltip("AudioSource для звуков, которые надо прерывать (кофемашина). " +
                 "loop=false, playOnAwake=false. Создай отдельный дочерний с AudioSource.")]
        public AudioSource interruptibleSource;

        // ===================== Громкости =====================

        [Header("Volumes")]
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        [Range(0f, 1f)] public float ambienceVolume = 0.4f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;

        private GameStateManager _gsm;
        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Принудительно настраиваем источники.
            if (musicSource != null) { musicSource.loop = true; musicSource.playOnAwake = false; }
            if (ambienceSource != null) { ambienceSource.loop = true; ambienceSource.playOnAwake = false; }
            if (sfxSource != null) { sfxSource.loop = false; sfxSource.playOnAwake = false; }
            if (interruptibleSource != null) { interruptibleSource.loop = false; interruptibleSource.playOnAwake = false; }
        }

        private void Start()
        {
            TrySubscribe();
            // Если UIRouter уже инициализирован — сразу применим стартовую музыку.
            if (UI.UIRouter.Instance != null)
                OnScreenChanged(UI.UIRouter.Instance.CurrentScreen);
            else
                PlayMusic(musicMain);
        }

        private void Update()
        {
            // UIRouter может появиться позже AudioService (зависит от порядка Start).
            // Перепроверяем подписку каждый кадр, пока не подключим.
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            _gsm = GameStateManager.Instance;
            if (_gsm == null) return;

            _gsm.Orders.OrderSpawned += OnOrderSpawned;
            _gsm.Wheel.Spun += OnWheelSpun;

            if (UI.UIRouter.Instance != null)
            {
                UI.UIRouter.Instance.ScreenChanged += OnScreenChanged;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_gsm != null)
            {
                _gsm.Orders.OrderSpawned -= OnOrderSpawned;
                _gsm.Wheel.Spun -= OnWheelSpun;
            }
            if (UI.UIRouter.Instance != null)
                UI.UIRouter.Instance.ScreenChanged -= OnScreenChanged;
        }

        // ===================== Реакции на события =====================

        private void OnOrderSpawned(Order _) => PlayNewServe();

        private void OnWheelSpun(WheelSectorDefinition sector)
        {
            // Выигрыш — Success. Nothing — без звука (грустный пузырь Дринчика отыграет).
            if (sector != null && sector.prizeType != WheelPrizeType.Nothing)
                PlaySuccess();
        }

        private void OnScreenChanged(UI.Screen screen)
        {
            // На экране готовки — другая музыка + амбиент часов.
            // На всех остальных — основная музыка, амбиент стоп.
            if (screen == UI.Screen.Cooking)
            {
                PlayMusic(musicCooking);
                PlayAmbience(ambienceClock);
            }
            else
            {
                PlayMusic(musicMain);
                StopAmbience();
            }
        }

        // ===================== API =====================

        /// Запустить музыку (или сменить с фейдом). Если clip == текущий и уже играет — ничего.
        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null) return;
            if (clip == null) { musicSource.Stop(); return; }
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void PlayAmbience(AudioClip clip)
        {
            if (ambienceSource == null) return;
            if (clip == null) { ambienceSource.Stop(); return; }
            if (ambienceSource.clip == clip && ambienceSource.isPlaying) return;
            ambienceSource.clip = clip;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }

        public void StopAmbience()
        {
            if (ambienceSource != null) ambienceSource.Stop();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        // ===================== Шорткаты =====================

        public void PlayClick()         => PlaySFX(sfxClick);
        public void PlayFocus()         => PlaySFX(sfxFocus);
        public void PlayNewServe()      => PlaySFX(sfxNewServe);
        public void PlayRightWay()      => PlaySFX(sfxRightWay);
        public void PlaySuccess()       => PlaySFX(sfxSuccess);

        /// Кофемашина играет на отдельном interruptibleSource, чтобы её можно было
        /// оборвать через StopCoffeeMachine() (вызывается из UIBurster.Burst()).
        public void PlayCoffeeMachine()
        {
            if (interruptibleSource != null && sfxCoffeeMachine != null)
            {
                interruptibleSource.clip = sfxCoffeeMachine;
                interruptibleSource.volume = sfxVolume;
                interruptibleSource.Play();
            }
            else
            {
                // Fallback на one-shot если interruptibleSource не подключен.
                PlaySFX(sfxCoffeeMachine);
            }
        }

        public void StopCoffeeMachine()
        {
            if (interruptibleSource != null) interruptibleSource.Stop();
        }
    }
}
