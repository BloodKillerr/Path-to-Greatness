using System.Collections;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject[] Prefabs;

    public float MinInterval = 0.5f;

    public float MaxInterval = 2f;

    public Transform[] SpawnPoints;

    public Transform ParentForSpawned;

    public bool StartOnAwake = true;

    public float AutoDestroyAfter = 5f;

    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (StartOnAwake)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(MinInterval, MaxInterval));
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (Prefabs == null || Prefabs.Length == 0)
        {
            Debug.LogWarning("RandomProjectileSpawner: no prefabs assigned.");
            return;
        }

        GameObject prefab = Prefabs[Random.Range(0, Prefabs.Length)];
        if (prefab == null)
        {
            return;
        }

        Vector3 pos;
        Quaternion rot = Quaternion.identity;
        if (SpawnPoints != null && SpawnPoints.Length > 0)
        {
            Transform sp = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
            pos = sp.position;
            rot = sp.rotation;
        }
        else
        {
            pos = transform.position;
            rot = transform.rotation;
        }

        GameObject obj = Instantiate(prefab, pos, rot, ParentForSpawned);
        if (AutoDestroyAfter > 0f)
        {
            Destroy(obj, AutoDestroyAfter);
        }
    }
}
