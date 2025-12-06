using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataApplier : MonoBehaviour
{
    public bool disableCharacterControllerWhileMoving = true;
    public bool reenableNextFrame = true;

    private void Start()
    {
        MasterSaveData data = SaveManager.LoadedData;
        if (data == null)
        {
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentIndex != data.LastSceneBuildIndex)
        {
            Debug.LogWarning($"[GameDataApplier] Scene index mismatch! " +
                             $"Expected {data.LastSceneBuildIndex}, but we're in {currentIndex}.");
        }

        LoadPlayerData(data);
        LoadPlayerStats(data);
        LoadDungeonData(data);
        LoadEnemiesData(data);

        SaveManager.IsLoadingSave = false;
        SaveManager.LoadedData = null;
    }

    private void LoadPlayerData(MasterSaveData data)
    {
        if (data.PlayerData != null)
        {
            GameObject playerObj = Player.Instance?.gameObject;
            if (playerObj != null)
            {
                PlayerController targetPlayerController = playerObj.GetComponent<PlayerController>();
                if (targetPlayerController == null)
                {
                    return;
                }

                Transform playerTransform = targetPlayerController.transform;
                CharacterController charController = targetPlayerController.GetComponent<CharacterController>();

                if (disableCharacterControllerWhileMoving && charController != null)
                {
                    charController.enabled = false;
                }

                playerTransform.position = data.PlayerData.position;
                playerTransform.rotation = Quaternion.Euler(data.PlayerData.rotationEuler);

                TrySyncPlayerControllerInternalRotation(targetPlayerController, playerTransform.rotation);

                if (charController != null)
                {
                    if (reenableNextFrame)
                    {
                        StartCoroutine(ReenableCharacterControllerNextFrame(charController));
                    }
                    else
                    {
                        charController.enabled = true;
                    }
                }
            }
            else
            {
                Debug.LogError("[GameDataApplier] Could not find Player.Instance to apply saved data.");
            }
        }
    }

    private IEnumerator ReenableCharacterControllerNextFrame(CharacterController cc)
    {
        yield return null;
        if (cc != null)
        {
            cc.stepOffset = 0.3f;
            cc.enabled = true;
        }
    }

    private void TrySyncPlayerControllerInternalRotation(PlayerController pc, Quaternion playerRotation)
    {
        if (pc.PlayerCamera != null)
        {
            Vector3 e = playerRotation.eulerAngles;
            pc.PlayerCamera.transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

        Type type = typeof(PlayerController);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        try
        {
            FieldInfo camRotField = type.GetField("cameraRotation", flags);
            if (camRotField != null)
            {
                Vector2 camRot = Vector2.zero;
                camRot.x = playerRotation.eulerAngles.y;
                camRot.y = 0f;
                camRotField.SetValue(pc, camRot);
            }

            var targetRotField = type.GetField("playerTargetRotation", flags);
            if (targetRotField != null)
            {
                Vector2 targetRot = Vector2.zero;
                targetRot.x = playerRotation.eulerAngles.y;
                targetRotField.SetValue(pc, targetRot);
            }

            var mismatchField = type.GetField("rotationMismatch", flags);
            if (mismatchField != null)
            {
                mismatchField.SetValue(pc, 0f);
            }
        }
        catch
        {

        }
    }

    private void LoadPlayerStats(MasterSaveData data)
    {
        if (data.PlayerStatsData == null)
        {
            return;
        }

        if (Player.Instance == null)
        {
            Debug.LogError("[GameDataApplier] Could not find Player.Instance to apply saved stats.");
            return;
        }

        PlayerStats ps = Player.Instance.GetComponent<PlayerStats>();
        if (ps == null)
        {
            Debug.LogError("[GameDataApplier] PlayerStats component missing on Player.Instance.");
            return;
        }

        ps.RestorePlayerStats(data.PlayerStatsData);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshAllUI();
        }
    }

    private void LoadDungeonData(MasterSaveData data)
    {
        if (data.DungeonData != null && DungeonManager.Instance != null)
        {
            DungeonManager.Instance.RestoreDungeonState(data.DungeonData);
        }
    }

    private void LoadEnemiesData(MasterSaveData data)
    {
        if (data == null)
        {
            return;
        }

        bool hasEnemies = data.enemies != null && data.enemies.Count > 0;
        bool hasSpawners = data.spawners != null && data.spawners.Count > 0;
        if (!hasEnemies && !hasSpawners)
        {
            return;
        }

        Enemy[] existingEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy e in existingEnemies)
        {
            if (e != null)
            {
                Destroy(e.gameObject);
            }
        }

        EnemySpawner[] spawnersInScene = FindObjectsOfType<EnemySpawner>();
        if (data.spawners != null && spawnersInScene != null && spawnersInScene.Length > 0)
        {
            foreach (EnemySpawnerSaveData spData in data.spawners)
            {
                EnemySpawner best = null;
                float bestDist = float.MaxValue;
                foreach (EnemySpawner s in spawnersInScene)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    float d = Vector3.SqrMagnitude(s.transform.position - spData.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = s;
                    }
                }

                if (best != null)
                {
                    best.RestoreSpawnerState(spData);
                }
                else
                {
                    Debug.LogWarning($"[GameDataApplier] No EnemySpawner found to restore spawner at {spData.position}");
                }
            }
        }

        GameObject FindPrefabByEnemyName(string enemyName)
        {
            if (string.IsNullOrEmpty(enemyName))
            {
                return null;
            }

            if (spawnersInScene != null)
            {
                foreach (EnemySpawner s in spawnersInScene)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    GameObject[] arr = s.EnemyPrefabs;
                    if (arr == null)
                    {
                        continue;
                    }

                    foreach (GameObject p in arr)
                    {
                        if (p == null)
                        {
                            continue;
                        }

                        EnemyStats es = p.GetComponent<EnemyStats>();
                        if (es != null && es.CharacterName == enemyName)
                        {
                            return p;
                        }
                    }
                }
            }

            GameObject[] resPrefabs = Resources.LoadAll<GameObject>("Enemies");
            if (resPrefabs != null && resPrefabs.Length > 0)
            {
                foreach (GameObject p in resPrefabs)
                {
                    if (p == null)
                    {
                        continue;
                    }

                    EnemyStats es = p.GetComponent<EnemyStats>();
                    if (es != null && es.CharacterName == enemyName)
                    {
                        return p;
                    }
                }
            }

            GameObject direct = Resources.Load<GameObject>(enemyName);
            if (direct != null)
            {
                return direct;
            }

            return null;
        }

        if (data.enemies != null)
        {
            foreach (EnemySaveData eData in data.enemies)
            {
                if (eData == null)
                {
                    continue;
                }

                GameObject prefab = FindPrefabByEnemyName(eData.enemyName);
                if (prefab == null)
                {
                    Debug.LogWarning($"[GameDataApplier] Could not find prefab with CharacterName '{eData.enemyName}' - skipping enemy restore.");
                    continue;
                }

                GameObject inst = Instantiate(prefab, eData.position, eData.rotation);
                Enemy instEnemy = inst.GetComponent<Enemy>();
                if (instEnemy != null)
                {
                    instEnemy.RestoreEnemyState(eData);
                }
                else
                {
                    Debug.LogWarning($"[GameDataApplier] Instantiated prefab '{prefab.name}' does not contain Enemy component.");
                }
            }
        }
    }
}