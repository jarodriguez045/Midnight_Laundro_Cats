using TMPro;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class InteractablePrompt : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;

        private void Awake()
        {
            if (promptRoot == null)
            {
                promptRoot = gameObject;
            }

            Hide();
        }

        public void Show(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}
