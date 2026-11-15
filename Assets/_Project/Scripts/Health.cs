using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool startAtMaxHealth = true;
    [SerializeField] float startingHealth = 100f;
    [SerializeField] float invulnerabilityWindow;
    [SerializeField] bool disableGameObjectOnDeath;

    public UnityEvent<float> OnDamaged;
    public UnityEvent<float> OnHealed;
    public UnityEvent OnDied;

    float _current;
    float _invulnerableUntil;
    bool _dead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => _current;
    public bool IsDead => _dead;
    public float NormalizedHealth => maxHealth <= 0f ? 0f : Mathf.Clamp01(_current / maxHealth);

    void Awake()
    {
        _current = startAtMaxHealth ? maxHealth : Mathf.Clamp(startingHealth, 0f, maxHealth);
        _dead = _current <= 0f;
    }

    public void Heal(float amount)
    {
        if (_dead || amount <= 0f)
        {
            return;
        }

        float before = _current;
        _current = Mathf.Min(maxHealth, _current + amount);
        float healed = _current - before;
        if (healed > 0f)
        {
            OnHealed?.Invoke(healed);
        }
    }

    public void Damage(float amount)
    {
        if (_dead || amount <= 0f)
        {
            return;
        }

        if (Time.time < _invulnerableUntil)
        {
            return;
        }

        _current = Mathf.Max(0f, _current - amount);
        if (invulnerabilityWindow > 0f)
        {
            _invulnerableUntil = Time.time + invulnerabilityWindow;
        }

        OnDamaged?.Invoke(amount);

        if (_current <= 0f)
        {
            Die();
        }
    }

    public void Kill()
    {
        if (_dead)
        {
            return;
        }

        _current = 0f;
        Die();
    }

    void Die()
    {
        if (_dead)
        {
            return;
        }

        _dead = true;
        OnDied?.Invoke();
        if (disableGameObjectOnDeath)
        {
            gameObject.SetActive(false);
        }
    }
}
