using System;
using UnityEngine;

namespace MidnightLaundroCats
{
    public enum GameState
    {
        WaitingToStart,
        LaundryRunning,
        Win,
        Lose
    }

    public sealed class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GameState initialState = GameState.WaitingToStart;
        [SerializeField] private LaundryCycleManager laundryCycleManager;
        [SerializeField] private Health playerHealth;

        public event Action<GameState> StateChanged;

        public GameState CurrentState { get; private set; }
        public bool IsRoundOver => CurrentState == GameState.Win || CurrentState == GameState.Lose;

        private void Awake()
        {
            CurrentState = initialState;
        }

        private void OnEnable()
        {
            if (laundryCycleManager != null)
            {
                laundryCycleManager.CycleCompleted += HandleLaundryCycleCompleted;
            }

            if (playerHealth != null)
            {
                playerHealth.Died += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (laundryCycleManager != null)
            {
                laundryCycleManager.CycleCompleted -= HandleLaundryCycleCompleted;
            }

            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }
        }

        private void Start()
        {
            StateChanged?.Invoke(CurrentState);
        }

        public bool TryStartLaundryCycle()
        {
            if (CurrentState != GameState.WaitingToStart)
            {
                return false;
            }

            if (laundryCycleManager == null)
            {
                Debug.LogWarning($"{nameof(GameStateManager)} cannot start laundry without a {nameof(LaundryCycleManager)} reference.", this);
                return false;
            }

            SetState(GameState.LaundryRunning);
            laundryCycleManager.StartCycle();
            return true;
        }

        public void ForceWin()
        {
            if (CurrentState == GameState.LaundryRunning)
            {
                SetState(GameState.Win);
            }
        }

        public void ForceLose()
        {
            if (CurrentState == GameState.Win || CurrentState == GameState.Lose)
            {
                return;
            }

            if (laundryCycleManager != null)
            {
                laundryCycleManager.StopCycle(false);
            }

            SetState(GameState.Lose);
        }

        public void ResetToWaiting()
        {
            if (laundryCycleManager != null)
            {
                laundryCycleManager.ResetCycle();
            }

            SetState(GameState.WaitingToStart);
        }

        private void HandleLaundryCycleCompleted()
        {
            if (CurrentState == GameState.LaundryRunning)
            {
                SetState(GameState.Win);
            }
        }

        private void HandlePlayerDied(Health _)
        {
            ForceLose();
        }

        private void SetState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
