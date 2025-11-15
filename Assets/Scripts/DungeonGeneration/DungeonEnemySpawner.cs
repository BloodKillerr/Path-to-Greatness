using UnityEngine;

public class DungeonEnemySpawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;

    public void SpawnEnemies()
    {
        //foreach (Transform spawnPoint in spawnPoints)
        //{
        //    GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        //    Quaternion rot = spawnPoint.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        //    Enemy enemy = Instantiate(prefab,
        //                            spawnPoint.position,
        //                            rot,
        //                            transform)
        //                  .GetComponent<Enemy>();

        //    RoomController controller = GetComponent<RoomController>();
        //    controller.Enemies.Add(enemy);
        //    enemy.RoomController = controller;
        //}
    }
}
