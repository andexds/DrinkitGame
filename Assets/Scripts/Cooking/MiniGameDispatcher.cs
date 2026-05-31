using System;
using DrinkitGame.Core;
using DrinkitGame.Data;
using UnityEngine;

namespace DrinkitGame.Cooking
{
    /// Управляет 4 мини-играми: знает какую открыть для какого CookingStepType.
    /// Один MonoBehaviour висит на MiniGameOverlay GameObject; держит ссылки на 4 sub-overlay'я.
    public class MiniGameDispatcher : MonoBehaviour
    {
        [Header("Mini-game sub-overlays")]
        public GameObject m1Root;
        public M1GrindingMiniGame m1;

        public GameObject m2Root;
        public M2MilkSteamingMiniGame m2;

        public GameObject m3Root;
        public M3PourOverMiniGame m3;

        public GameObject m4Root;
        public M4WhiskingMiniGame m4;

        public event Action<float> Completed;

        private IMiniGame _active;

        /// Запустить мини-игру, соответствующую шагу. Возвращает true если запуск произошёл (шаг — мини-игра).
        public bool TryBegin(CookingStep step, MachineTierDefinition tier)
        {
            gameObject.SetActive(true);
            SetActive(m1Root, false);
            SetActive(m2Root, false);
            SetActive(m3Root, false);
            SetActive(m4Root, false);

            DetachCurrent();

            switch (step.type)
            {
                case CookingStepType.GrindCoffee:
                    SetActive(m1Root, true);
                    _active = m1;
                    break;
                case CookingStepType.SteamMilk:
                case CookingStepType.SteamCream:
                    SetActive(m2Root, true);
                    _active = m2;
                    break;
                case CookingStepType.PourOver:
                    SetActive(m3Root, true);
                    _active = m3;
                    break;
                case CookingStepType.Whisk:
                    SetActive(m4Root, true);
                    _active = m4;
                    break;
                default:
                    // Шаг не является мини-игрой — оверлей не нужен
                    gameObject.SetActive(false);
                    return false;
            }

            if (_active != null)
            {
                _active.Completed += OnCompleted;
                _active.Begin(tier);
                return true;
            }
            return false;
        }

        private void OnCompleted(float quality)
        {
            DetachCurrent();
            gameObject.SetActive(false);
            Completed?.Invoke(quality);
        }

        private void DetachCurrent()
        {
            if (_active != null)
            {
                _active.Completed -= OnCompleted;
                _active = null;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}