using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] Transform attackOrigin;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] float attackDamage = 25f;
    [SerializeField] float attackCooldown = 0.45f;
    [SerializeField] float attackRange = 1.35f;
    [SerializeField] float attackRadius = 0.75f;
    [SerializeField] float knockbackForce = 9f;
    [SerializeField] KeyCode attackKey = KeyCode.Mouse0;

    float _nextAttackTime;

    public bool IsAttackReady => Time.time >= _nextAttackTime;
    public float AttackCooldownRemaining => Mathf.Max(0f, _nextAttackTime - Time.time);

    void Update()
    {
        if (!Input.GetKeyDown(attackKey) || !IsAttackReady)
        {
            return;
        }

        PerformAttack();
    }

    public void PerformAttack()
    {
        _nextAttackTime = Time.time + attackCooldown;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 point = origin.position + origin.forward * attackRange * 0.5f;
        Collider[] hits = Physics.OverlapSphere(point, attackRadius, enemyMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
            {
                continue;
            }

            Health health = hit.GetComponentInParent<Health>();
            if (health != null && health.gameObject != gameObject)
            {
                health.Damage(attackDamage);
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 dir = (hit.transform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f)
                {
                    dir = origin.forward;
                }

                rb.AddForce(dir.normalized * knockbackForce + Vector3.up * (knockbackForce * 0.25f), ForceMode.Impulse);
            }
        }
    }
}
