using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class SockSpawner : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private Health targetHealth;
        [SerializeField] private GameObject sockPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnIntervalSeconds = 4f;
        [SerializeField] private int maxAliveSocks = 8;
        [SerializeField] private bool spawnImmediatelyOnLaundryStart = true;
        [SerializeField] private bool useRandomSpawnPoints = true;

        public event Action<int> AliveCountChanged;

        public int AliveCount => activeSocks.Count;

        private readonly List<SockEnemyAI> activeSocks = new List<SockEnemyAI>();
        private float nextSpawnTime;
        private int nextSpawnPointIndex;

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

            for (int i = activeSocks.Count - 1; i >= 0; i--)
            {
                if (activeSocks[i] != null)
                {
                    activeSocks[i].Removed -= HandleSockRemoved;
                }
            }
        }

        private void Update()
        {
            PruneMissingSocks();

            if (gameStateManager == null || gameStateManager.CurrentState != GameState.LaundryRunning)
            {
                return;
            }

            if (Time.time >= nextSpawnTime)
            {
                TrySpawnSock();
                nextSpawnTime = Time.time + spawnIntervalSeconds;
            }
        }

        public bool TrySpawnSock()
        {
            PruneMissingSocks();

            if (sockPrefab == null)
            {
                Debug.LogWarning($"{nameof(SockSpawner)} needs a sock prefab before it can spawn enemies.", this);
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"{nameof(SockSpawner)} needs at least one spawn point.", this);
                return false;
            }

            if (activeSocks.Count >= maxAliveSocks)
            {
                return false;
            }

            Transform spawnPoint = GetNextSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{nameof(SockSpawner)} found a missing spawn point reference.", this);
                return false;
            }

            GameObject instance = Instantiate(sockPrefab, spawnPoint.position, spawnPoint.rotation);
            SockEnemyAI enemy = instance.GetComponent<SockEnemyAI>();

            if (enemy == null)
            {
                Debug.LogWarning("Sock prefab needs a SockEnemyAI component.", instance);
                Destroy(instance);
                return false;
            }

            enemy.Configure(gameStateManager, targetHealth);
            enemy.Removed += HandleSockRemoved;
            activeSocks.Add(enemy);
            AliveCountChanged?.Invoke(activeSocks.Count);
            return true;
        }

        public void DespawnAllSocks()
        {
            for (int i = activeSocks.Count - 1; i >= 0; i--)
            {
                SockEnemyAI sock = activeSocks[i];
                if (sock == null)
                {
                    activeSocks.RemoveAt(i);
                    continue;
                }

                sock.Removed -= HandleSockRemoved;
                Destroy(sock.gameObject);
                activeSocks.RemoveAt(i);
            }

            AliveCountChanged?.Invoke(activeSocks.Count);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state != GameState.LaundryRunning)
            {
                return;
            }

            nextSpawnTime = spawnImmediatelyOnLaundryStart ? Time.time : Time.time + spawnIntervalSeconds;
        }

        private Transform GetNextSpawnPoint()
        {
            if (useRandomSpawnPoints)
            {
                return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            }

            Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            return spawnPoint;
        }

        private void HandleSockRemoved(SockEnemyAI sock)
        {
            if (sock != null)
            {
                sock.Removed -= HandleSockRemoved;
            }

            activeSocks.Remove(sock);
            AliveCountChanged?.Invoke(activeSocks.Count);
        }

        private void PruneMissingSocks()
        {
            bool removedAny = false;

            for (int i = activeSocks.Count - 1; i >= 0; i--)
            {
                if (activeSocks[i] != null)
                {
                    continue;
                }

                activeSocks.RemoveAt(i);
                removedAny = true;
            }

            if (removedAny)
            {
                AliveCountChanged?.Invoke(activeSocks.Count);
            }
        }
    }
}
