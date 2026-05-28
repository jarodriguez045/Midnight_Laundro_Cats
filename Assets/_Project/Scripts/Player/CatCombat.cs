using System;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class CatCombat : MonoBehaviour
    {
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private float attackForwardOffset = 0.75f;
        [SerializeField] private float attackRadius = 0.9f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldownSeconds = 0.45f;
        [SerializeField] private KeyCode keyboardAttackKey = KeyCode.Space;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private Health selfHealth;

        public event Action Attacked;

        private readonly Collider[] hits = new Collider[16];
        private readonly Health[] damagedTargets = new Health[16];
        private float nextAttackTime;

        private void Awake()
        {
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }

            if (selfHealth == null)
            {
                selfHealth = GetComponentInParent<Health>();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(keyboardAttackKey))
            {
                TryAttack();
            }
        }

        public bool TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return false;
            }

            nextAttackTime = Time.time + attackCooldownSeconds;
            Vector3 attackCenter = attackOrigin.position + attackOrigin.forward * attackForwardOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(attackCenter, attackRadius, hits, targetLayers, triggerInteraction);
            int damagedTargetCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Health targetHealth = hits[i].GetComponentInParent<Health>();

                if (targetHealth == null || targetHealth == selfHealth || !targetHealth.IsAlive || HasAlreadyDamaged(targetHealth, damagedTargetCount))
                {
                    continue;
                }

                targetHealth.TakeDamage(attackDamage);
                damagedTargets[damagedTargetCount] = targetHealth;
                damagedTargetCount++;
            }

            Attacked?.Invoke();
            return true;
        }

        private bool HasAlreadyDamaged(Health targetHealth, int damagedTargetCount)
        {
            for (int i = 0; i < damagedTargetCount; i++)
            {
                if (damagedTargets[i] == targetHealth)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin.position + origin.forward * attackForwardOffset, attackRadius);
        }
    }
}
