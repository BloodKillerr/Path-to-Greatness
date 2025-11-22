using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static StatUpgradeEffectSO;
public class QuestManager : MonoBehaviour
{
    private readonly Dictionary<GameObject, List<QuestInstance>> activeQuests = new();

    public static QuestManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public bool AddQuest(QuestSO quest, GameObject player)
    {
        if (quest == null || player == null)
        {
            return false;
        }

        if (!activeQuests.TryGetValue(player, out var list))
        {
            list = new List<QuestInstance>();
            activeQuests[player] = list;
        }

        if (list.Exists(q => q.quest.questId == quest.questId))
        {
            Debug.Log($"[QuestManager] Player {player.name} already has quest {quest.title}");
            return false;
        }

        QuestInstance instance = new QuestInstance(quest, player, OnQuestCompleted, OnQuestProgressChanged);
        list.Add(instance);

        Debug.Log($"[QuestManager] Quest '{quest.title}' added for {player.name}");
        return true;
    }
    public void RemoveQuest(QuestSO quest, GameObject player)
    {
        if (quest == null || player == null)
        {
            return;
        }

        if (!activeQuests.TryGetValue(player, out var list))
        {
            return;
        }

        QuestInstance inst = list.Find(q => q.quest.questId == quest.questId);
        if (inst != null)
        {
            inst.Dispose();
            list.Remove(inst);
        }
    }
    public IReadOnlyList<QuestInstance> GetActiveQuests(GameObject player)
    {
        if (player == null || !activeQuests.TryGetValue(player, out var list))
        {
            return Array.Empty<QuestInstance>();
        }

        return list.AsReadOnly();
    }
    private void OnQuestCompleted(QuestInstance inst)
    {
        if (inst == null)
        {
            return;
        }

        GameObject player = inst.player;
        Debug.Log($"[QuestManager] Quest '{inst.quest.title}' completed for {player.name}");
        ApplyReward(inst.quest, player);
        if (activeQuests.TryGetValue(player, out var list))
        {
            list.Remove(inst);
        }
        var data = new Dictionary<string, object>()
        {
            { "target", player },
            { "questId", inst.quest.questId },
            { "questTitle", inst.quest.title }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Quest.QuestCompleted", gameObject, data));
    }

    private void OnQuestProgressChanged(QuestInstance inst, string eventId, int progress, int required)
    {
        Debug.Log($"[QuestManager] Progress for quest '{inst.quest.title}' ({eventId}): {progress}/{required}");
        var data = new Dictionary<string, object>() {
            { "target", inst.player },
            { "questId", inst.quest.questId },
            { "eventId", eventId },
            { "progress", progress },
            { "required", required }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Quest.QuestProgress", gameObject, data));
    }
    private void ApplyReward(QuestSO quest, GameObject player)
    {
        if (quest == null || player == null)
        {
            return;
        }

        switch (quest.rewardType)
        {
            case QuestRewardType.StatUpgrade:
                PlayerStats ps = player.GetComponent<PlayerStats>();
                if (ps == null)
                {
                    Debug.LogWarning("[QuestManager] PlayerStats not found on player when applying stat reward.");
                    break;
                }

                switch (quest.rewardStat)
                {
                    case StatType.Health: 
                        ps.UpgradeHealth(quest.rewardAmount); 
                        break;
                    case StatType.Strength: 
                        ps.UpgradeStrength(quest.rewardAmount); 
                        break;
                    case StatType.Agility: 
                        ps.UpgradeAgility(quest.rewardAmount); 
                        break;
                    case StatType.Magic: 
                        ps.UpgradeMagic(quest.rewardAmount); 
                        break;
                }
                var sdat = new Dictionary<string, object>()
                {
                    { "target", player }, { "questId", quest.questId }, { "rewardType", "Stat" },
                    { "stat", quest.rewardStat }, { "amount", quest.rewardAmount }
                };
                EventBus.Instance.Publish(new SupervisorEvent("Quest.RewardGiven", gameObject, sdat));
                break;

            case QuestRewardType.AbilityGrant:
                var req = new Dictionary<string, object>()
                {
                    { "target", player },
                    { "abilityName", quest.rewardAbilityName },
                    { "bundleId", $"quest_{quest.questId}" },
                    { "singleGrant", quest.singleGrantPerPlayer }
                };
                EventBus.Instance.Publish(new SupervisorEvent("Supervisor.GrantAbilityRequest", gameObject, req));
                break;
        }
    }
}
