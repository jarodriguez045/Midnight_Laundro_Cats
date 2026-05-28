using TMPro;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private LaundryCycleManager laundryCycleManager;
        [SerializeField] private Health playerHealth;
        [SerializeField] private SockSpawner sockSpawner;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text socksAliveText;
        [SerializeField] private TMP_Text gameStateText;

        private void OnEnable()
        {
            if (gameStateManager != null)
            {
                gameStateManager.StateChanged += HandleStateChanged;
            }

            if (laundryCycleManager != null)
            {
                laundryCycleManager.TimerChanged += HandleTimerChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged += HandleHealthChanged;
            }

            if (sockSpawner != null)
            {
                sockSpawner.AliveCountChanged += HandleAliveCountChanged;
            }
        }

        private void OnDisable()
        {
            if (gameStateManager != null)
            {
                gameStateManager.StateChanged -= HandleStateChanged;
            }

            if (laundryCycleManager != null)
            {
                laundryCycleManager.TimerChanged -= HandleTimerChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }

            if (sockSpawner != null)
            {
                sockSpawner.AliveCountChanged -= HandleAliveCountChanged;
            }
        }

        private void Start()
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (laundryCycleManager != null)
            {
                UpdateTimer(laundryCycleManager.RemainingSeconds);
            }

            if (playerHealth != null)
            {
                UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (sockSpawner != null)
            {
                UpdateSocksAlive(sockSpawner.AliveCount);
            }

            if (gameStateManager != null)
            {
                UpdateGameState(gameStateManager.CurrentState);
            }
        }

        private void HandleTimerChanged(float remainingSeconds, float durationSeconds)
        {
            UpdateTimer(remainingSeconds);
        }

        private void HandleHealthChanged(Health health, int currentHealth, int maxHealth)
        {
            UpdateHealth(currentHealth, maxHealth);
        }

        private void HandleAliveCountChanged(int aliveCount)
        {
            UpdateSocksAlive(aliveCount);
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateGameState(state);
        }

        private void UpdateTimer(float remainingSeconds)
        {
            if (timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = $"Timer: {minutes:00}:{seconds:00}";
        }

        private void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthText != null)
            {
                healthText.text = $"Health: {currentHealth}/{maxHealth}";
            }
        }

        private void UpdateSocksAlive(int aliveCount)
        {
            if (socksAliveText != null)
            {
                socksAliveText.text = $"Socks: {aliveCount}";
            }
        }

        private void UpdateGameState(GameState state)
        {
            if (gameStateText != null)
            {
                gameStateText.text = $"State: {state}";
            }
        }
    }
}
