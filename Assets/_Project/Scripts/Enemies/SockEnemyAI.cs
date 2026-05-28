using System;
using UnityEngine;
using UnityEngine.AI;

namespace MidnightLaundroCats
{
    public sealed class SockEnemyAI : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private Health targetHealth;
        [SerializeField] private Health selfHealth;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private float moveSpeed = 2.75f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float stoppingDistance = 0.9f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldownSeconds = 1f;
        [SerializeField] private float destroyAfterDeathSeconds = 0.05f;

        public event Action<SockEnemyAI> Removed;

        private Transform targetTransform;
        private float nextAttackTime;
        private bool removalNotified;

        private void Awake()
        {
            if (selfHealth == null)
            {
                selfHealth = GetComponent<Health>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            ConfigureAgent();
            CacheTargetTransform();
        }

        private void Start()
        {
            // NavMeshAgent controls the Transform while enabled, even when not
            // placed on a NavMesh.  Disable it here so direct movement can take
            // over if no NavMesh is present — removes the baked-NavMesh
            // requirement for the first prototype.
            if (navMeshAgent != null && navMeshAgent.enabled && !navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.enabled = false;
                Debug.Log(
                    $"{nameof(SockEnemyAI)} '{name}': NavMeshAgent found but not on a NavMesh. " +
                    "Using direct movement fallback. Bake a NavMesh to enable pathfinding.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (selfHealth != null)
            {
                selfHealth.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (selfHealth != null)
            {
                selfHealth.Died -= HandleDied;
            }

            NotifyRemoved();
        }

        private void Update()
        {
            if (!CanAct())
            {
                StopMoving();
                return;
            }

            ChaseTarget();
            TryAttackTarget();
        }

        public void Configure(GameStateManager stateManager, Health target)
        {
            gameStateManager = stateManager;
            targetHealth = target;
            CacheTargetTransform();
        }

        private bool CanAct()
        {
            if (targetHealth == null || !targetHealth.IsAlive || targetTransform == null)
            {
                return false;
            }

            return gameStateManager == null || gameStateManager.CurrentState == GameState.LaundryRunning;
        }

        private void CacheTargetTransform()
        {
            targetTransform = targetHealth != null ? targetHealth.transform : null;
        }

        private void ConfigureAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;
        }

        private void ChaseTarget()
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetTransform.position);
                return;
            }

            Vector3 toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                rotationSpeed * Time.deltaTime);
        }

        private void TryAttackTarget()
        {
            Vector3 toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > attackRange * attackRange || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldownSeconds;
            targetHealth.TakeDamage(attackDamage);
        }

        private void StopMoving()
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        private void HandleDied(Health health)
        {
            StopMoving();
            NotifyRemoved();
            Destroy(gameObject, destroyAfterDeathSeconds);
        }

        private void NotifyRemoved()
        {
            if (removalNotified)
            {
                return;
            }

            removalNotified = true;
            Removed?.Invoke(this);
        }
    }
}
