using System;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class LaundryCycleManager : MonoBehaviour
    {
        [SerializeField] private float cycleDurationSeconds = 120f;

        public event Action CycleStarted;
        public event Action CycleStopped;
        public event Action CycleCompleted;
        public event Action<float, float> TimerChanged;

        public bool IsRunning { get; private set; }
        public float CycleDurationSeconds => cycleDurationSeconds;
        public float RemainingSeconds { get; private set; }
        public float NormalizedRemaining => cycleDurationSeconds <= 0f ? 0f : RemainingSeconds / cycleDurationSeconds;

        private void Awake()
        {
            ResetCycle();
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            TimerChanged?.Invoke(RemainingSeconds, cycleDurationSeconds);

            if (RemainingSeconds <= 0f)
            {
                CompleteCycle();
            }
        }

        public bool StartCycle()
        {
            if (IsRunning)
            {
                return false;
            }

            RemainingSeconds = Mathf.Max(0f, cycleDurationSeconds);
            IsRunning = true;
            CycleStarted?.Invoke();
            TimerChanged?.Invoke(RemainingSeconds, cycleDurationSeconds);

            if (RemainingSeconds <= 0f)
            {
                CompleteCycle();
            }

            return true;
        }

        public void StopCycle(bool resetTimer)
        {
            if (!IsRunning)
            {
                if (resetTimer)
                {
                    ResetCycle();
                }

                return;
            }

            IsRunning = false;

            if (resetTimer)
            {
                RemainingSeconds = Mathf.Max(0f, cycleDurationSeconds);
                TimerChanged?.Invoke(RemainingSeconds, cycleDurationSeconds);
            }

            CycleStopped?.Invoke();
        }

        public void ResetCycle()
        {
            IsRunning = false;
            RemainingSeconds = Mathf.Max(0f, cycleDurationSeconds);
            TimerChanged?.Invoke(RemainingSeconds, cycleDurationSeconds);
        }

        private void CompleteCycle()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            RemainingSeconds = 0f;
            TimerChanged?.Invoke(RemainingSeconds, cycleDurationSeconds);
            CycleCompleted?.Invoke();
        }
    }
}
