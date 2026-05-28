using System;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private int currentHealth = 5;
        [SerializeField] private bool startAtMaxHealth = true;

        public event Action<Health, int, int> HealthChanged;
        public event Action<Health, int> Damaged;
        public event Action<Health, int> Healed;
        public event Action<Health> Died;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = startAtMaxHealth ? maxHealth : Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        private void Start()
        {
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive)
            {
                return;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            int damageTaken = previousHealth - currentHealth;

            Damaged?.Invoke(this, damageTaken);
            HealthChanged?.Invoke(this, currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Died?.Invoke(this);
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            int healedAmount = currentHealth - previousHealth;

            if (healedAmount <= 0)
            {
                return;
            }

            Healed?.Invoke(this, healedAmount);
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
        }
    }
}
