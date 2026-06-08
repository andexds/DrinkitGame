using System.Collections;
using DrinkitGame.Audio;
using DrinkitGame.Telegram;
using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    /// Простой «успех»-фидбэк: при вызове Burst() клонирует Image-шаблон N раз,
    /// разбрасывает копии по кругу с затуханием. Без сторонних твин-библиотек,
    /// без ParticleSystem (не дружит со Screen Space Overlay Canvas).
    /// Шаблон должен быть выключен по умолчанию и лежать внутри этого же GameObject
    /// (или рядом — главное, чтобы template.transform.parent был доступен).
    public class UIBurster : MonoBehaviour
    {
        [Header("Источник частицы")]
        [Tooltip("Image-шаблон одной частицы (шарик, звёздочка). " +
                 "GameObject должен быть выключен — мы его клонируем при Burst().")]
        public Image template;

        [Header("Настройки взрыва")]
        [Tooltip("Сколько частиц вылетает за один Burst().")]
        [Range(1, 32)]
        public int particleCount = 8;

        [Tooltip("На сколько пикселей частицы разлетаются от центра за время duration.")]
        [Range(10f, 400f)]
        public float radius = 80f;

        [Tooltip("Сколько секунд живёт каждая частица. К концу — alpha=0 и Destroy.")]
        [Range(0.2f, 2f)]
        public float duration = 0.7f;

        [Tooltip("Случайный разброс угла каждой частицы (радианы). 0 = строго по сектору.")]
        [Range(0f, 1f)]
        public float angleJitter = 0.3f;

        [Tooltip("Опционально: масштаб частицы в начале и в конце (1=без изменений).")]
        public Vector2 scaleStartEnd = new Vector2(1f, 0.5f);

        [Header("Haptic")]
        [Tooltip("Эмитить telegram-haptic при каждом Burst().")]
        public bool emitHaptic = true;

        /// Запустить взрыв из текущей позиции (или из позиции template — если задан).
        public void Burst()
        {
            if (template == null) return;
            var parent = template.transform.parent;
            if (parent == null) parent = transform;

            var templateRT = template.rectTransform;
            Vector2 origin = templateRT != null ? templateRT.anchoredPosition : Vector2.zero;

            for (int i = 0; i < particleCount; i++)
            {
                float baseAngle = (i / (float)particleCount) * Mathf.PI * 2f;
                float angle = baseAngle + Random.Range(-angleJitter, angleJitter);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                var go = Instantiate(template.gameObject, parent);
                go.SetActive(true);

                // Главное: ставим железную страховку на удаление через duration+запас.
                // Это срабатывает независимо от состояния корутины (Unity может убить
                // её при деактивации родителя). До этого фикса частицы иногда висели
                // вечно на сцене после ухода с экрана готовки.
                Destroy(go, duration + 0.1f);

                StartCoroutine(AnimateParticle(go, origin, dir));
            }

            if (emitHaptic) TelegramHaptics.Light();
            // Полетели частицы «DONE» → гасим долго-играющий звук кофемашины
            // (если он сейчас не играет, метод тихо ничего не делает).
            if (AudioService.Instance != null) AudioService.Instance.StopCoffeeMachine();
        }

        private IEnumerator AnimateParticle(GameObject p, Vector2 origin, Vector2 dir)
        {
            var rt = p.GetComponent<RectTransform>();
            var img = p.GetComponent<Image>();
            if (rt == null || img == null) { if (p != null) Destroy(p); yield break; }

            rt.anchoredPosition = origin;
            rt.localScale = Vector3.one * scaleStartEnd.x;
            Color baseColor = img.color;
            baseColor.a = 1f;
            img.color = baseColor;

            float t = 0f;
            while (t < duration && p != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                // Лёгкое замедление к концу
                float ease = 1f - (1f - k) * (1f - k);
                rt.anchoredPosition = origin + dir * radius * ease;
                float scale = Mathf.Lerp(scaleStartEnd.x, scaleStartEnd.y, k);
                rt.localScale = Vector3.one * scale;
                Color c = baseColor; c.a = 1f - k; img.color = c;
                yield return null;
            }

            // Дублирующий Destroy — на случай если страховочный таймер ещё не сработал.
            // Если корутина дошла до конца штатно, p ещё живой → удаляем.
            if (p != null) Destroy(p);
        }
    }
}
