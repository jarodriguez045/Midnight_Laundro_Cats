using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class LaundryMachine : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private InteractablePrompt prompt;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private string readyPrompt = "Press E to start laundry";
        [SerializeField] private string runningPrompt = "Laundry is running";
        [SerializeField] private string completedPrompt = "Laundry complete";
        [SerializeField] private string failedPrompt = "The laundry was lost";

        private CatPlayerController playerInRange;

        private void Awake()
        {
            if (gameStateManager == null)
            {
                Debug.LogWarning($"{nameof(LaundryMachine)}: no GameStateManager assigned. The machine will not function.", this);
            }
        }

        private void OnEnable()
        {
            if (gameStateManager != null)
            {
                gameStateManager.StateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (gameStateManager != null)
            {
                gameStateManager.StateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (playerInRange == null || gameStateManager == null)
            {
                return;
            }

            if (gameStateManager.CurrentState == GameState.WaitingToStart && Input.GetKeyDown(interactKey))
            {
                gameStateManager.TryStartLaundryCycle();
                UpdatePrompt();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CatPlayerController player = other.GetComponentInParent<CatPlayerController>();

            if (player == null)
            {
                return;
            }

            playerInRange = player;
            UpdatePrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            CatPlayerController player = other.GetComponentInParent<CatPlayerController>();

            if (player == null || player != playerInRange)
            {
                return;
            }

            playerInRange = null;

            if (prompt != null)
            {
                prompt.Hide();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (prompt == null || playerInRange == null || gameStateManager == null)
            {
                return;
            }

            switch (gameStateManager.CurrentState)
            {
                case GameState.WaitingToStart:
                    prompt.Show(readyPrompt);
                    break;
                case GameState.LaundryRunning:
                    prompt.Show(runningPrompt);
                    break;
                case GameState.Win:
                    prompt.Show(completedPrompt);
                    break;
                case GameState.Lose:
                    prompt.Show(failedPrompt);
                    break;
            }
        }
    }
}
