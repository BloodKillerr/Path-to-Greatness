using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs = null;

    [SerializeField] private int maxConcurrent = 5;

    [SerializeField] private int maxTotalSpawns = 0;

    [SerializeField] private float spawnIntervalMin = 1f;

    [SerializeField] private float spawnIntervalMax = 3f;

    [SerializeField] private float spawnRadius = 10f;

    [SerializeField] private LayerMask obstacleMask = 0;

    [SerializeField] private float spawnClearance = 0.5f;

    [SerializeField] private int maxSpawnAttempts = 10;

    [SerializeField] private bool spawnOnStart = true;

    private int currentAlive = 0;
    private int pendingSpawns = 0;
    private int totalSpawned = 0;

    private readonly System.Random rng = new System.Random();

    private void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] No enemy prefabs assigned on {gameObject.name}.");
            enabled = false;
            return;
        }

        if (spawnOnStart)
        {
            TryScheduleSpawns();
        }
    }
    private void TryScheduleSpawns()
    {
        while ((currentAlive + pendingSpawns) < maxConcurrent && (maxTotalSpawns == 0 || (totalSpawned + pendingSpawns) < maxTotalSpawns))
        {
            pendingSpawns++;
            float delay = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            StartCoroutine(SpawnAfterDelay(delay));
        }
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        pendingSpawns--;
        if (maxTotalSpawns != 0 && totalSpawned >= maxTotalSpawns)
        {
            yield break;
        }

        SpawnNow();
        totalSpawned++;
        TryScheduleSpawns();
    }

    private void SpawnNow()
    {
        GameObject prefab = GetRandomPrefab();
        if (prefab == null)
        {
            return;
        }

        Vector3 pos = GetValidSpawnPosition();
        Quaternion rot = Quaternion.identity;

        GameObject spawned = Instantiate(prefab, pos, rot, null);
        currentAlive++;
        IEnemyDeathListener enemyInterface = spawned.GetComponent<IEnemyDeathListener>();
        if (enemyInterface != null)
        {
            enemyInterface.OnDeath += () => HandleEnemyDeath(spawned);
        }
        else
        {
            Enemy enemy = spawned.GetComponent<Enemy>();
            if (enemy != null)
                enemy.OnDeath += () => HandleEnemyDeath(spawned);
            else
            {
                StartCoroutine(WatchForDestroyed(spawned));
            }
        }
    }

    private IEnumerator WatchForDestroyed(GameObject obj)
    {
        while (obj != null)
        {
            yield return null;
        }
        HandleEnemyDeath(null);
    }

    private void HandleEnemyDeath(GameObject dead)
    {
        currentAlive = Mathf.Max(0, currentAlive - 1);
        TryScheduleSpawns();
    }

    private GameObject GetRandomPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return null;
        }

        int idx = UnityEngine.Random.Range(0, enemyPrefabs.Length);
        return enemyPrefabs[idx];
    }

    private Vector3 GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(r.x, 0f, r.y);
            if (obstacleMask != 0)
            {
                if (!Physics.CheckSphere(candidate, spawnClearance, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    return candidate;
                }
            }
            else
            {
                return candidate;
            }
        }
        return transform.position;
    }

    #region Editor Helpers
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.08f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
    #endregion
}
public interface IEnemyDeathListener
{
    event Action OnDeath;
}
