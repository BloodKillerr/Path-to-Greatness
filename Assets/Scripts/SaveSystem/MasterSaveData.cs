using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MasterSaveData
{
    public int LastSceneBuildIndex;

    public PlayerData PlayerData;

    public PlayerStatsData PlayerStatsData;

    public DungeonData DungeonData;

    public List<EnemySaveData> enemies;

    public List<EnemySpawnerSaveData> spawners;
}

[Serializable]
public class PlayerData
{
    public Vector3 position;
    public Vector3 rotationEuler;

    public PlayerData() { }

    public PlayerData(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotationEuler = rot.eulerAngles;
    }
}

[Serializable]
public class PlayerStatsData
{
    public int healthBase;
    public int strengthBase;
    public int agilityBase;
    public int magicBase;

    public int currentHealth;
    public int currentMP;
    public int maxMP;
}

[Serializable]
public class DungeonData
{
    public int seed;
    public Vector3 playerWorldPosition;
    public List<Vector2Int> roomsEntered;
    public List<Vector2Int> roomsCleared;
    public List<RoomControllerState> roomsState;
    public BossPortalData portalData;
}

[Serializable]
public class RoomControllerState
{
    public Vector2Int position;
    public bool hasBeenEntered;
    public bool isFinished;
    public bool checkCompletion;
}

[Serializable]
public class BossPortalData
{
    public Vector3 position;
    public Quaternion rotation;
    public int sceneIndex;
}

[Serializable]
public class EnemySaveData
{
    public string enemyName;
    public Vector3 position;
    public Quaternion rotation;

    public int currentHealth;
    public int maxHealth;
    public bool isInvincible;

    public EnemyGroupType groupType;
    public EnemyType enemyType;
}

[Serializable]
public class EnemySpawnerSaveData
{
    public Vector3 position;
    public int maxConcurrent;
    public int maxTotalSpawns;
    public int currentAlive;
    public int pendingSpawns;
    public int totalSpawned;
}