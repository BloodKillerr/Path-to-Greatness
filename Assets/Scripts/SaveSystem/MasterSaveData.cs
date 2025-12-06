using UnityEngine;
using System;
using System.Collections.Generic;
using static StatUpgradeEffectSO;

[Serializable]
public class MasterSaveData
{
    public int LastSceneBuildIndex;

    public PlayerData PlayerData;

    public PlayerStatsData PlayerStatsData;

    public DungeonData DungeonData;

    public List<EnemySaveData> Enemies;

    public List<EnemySpawnerSaveData> Spawners;

    public AbilitySaveData abilityData;

    public QuestSaveData questData;

    public SupervisorSaveData supervisorData;
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

[Serializable]
public class AbilitySaveData
{
    public List<string> passiveAbilityNames = new List<string>();
    public List<string> activeAbilityNames = new List<string>();
    public string[] boundAbilityNames = new string[4];
    public List<string> cooldownAbilityNames = new List<string>();
    public List<float> cooldownsSeconds = new List<float>();
}

[Serializable]
public class QuestInstanceSaveData
{
    public string questId;
    public List<string> eventIds = new List<string>();
    public List<int> progressValues = new List<int>();
}

[Serializable]
public class QuestSaveData
{
    public List<QuestInstanceSaveData> activeQuests = new List<QuestInstanceSaveData>();
    public List<string> completedQuestIds = new List<string>();
}

[Serializable]
public class SupervisorSaveData
{
    public List<EventCap> eventCaps = new List<EventCap>();

    public List<string> eventCountIds = new List<string>();
    public List<int> eventCountValues = new List<int>();

    public List<StatType> statGainTypes = new List<StatType>();
    public List<int> statGainValues = new List<int>();

    public List<string> grantedAbilityIds = new List<string>();
}