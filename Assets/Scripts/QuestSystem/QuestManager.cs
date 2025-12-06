using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static StatUpgradeEffectSO;
public class QuestManager : MonoBehaviour
{
    private readonly Dictionary<GameObject, List<QuestInstance>> activeQuests = new();

    private readonly Dictionary<GameObject, HashSet<string>> completedQuests = new();

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

        if (completedQuests.TryGetValue(player, out var completedSet) && completedSet.Contains(quest.questId))
        {
            Debug.Log($"[QuestManager] Player {player.name} already completed quest {quest.title} — skipping add.");
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

        var eventData = new Dictionary<string, object>()
        {
            { "target", player },
            { "questId", quest.questId },
            { "questTitle", quest.title },
            { "questDesc", quest.description },
            { "requirementsCount", quest.requirements?.Count ?? 0 },
            { "rewardType", quest.rewardType.ToString() }
        };

        if (quest.rewardType == QuestRewardType.StatUpgrade)
        {
            eventData["stat"] = quest.rewardStat;
            eventData["amount"] = quest.rewardAmount;
        }
        else if (quest.rewardType == QuestRewardType.AbilityGrant)
        {
            eventData["abilityName"] = quest.rewardAbilityName;
            eventData["singleGrant"] = quest.singleGrantPerPlayer;
        }

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new SupervisorEvent("Quest.QuestAdded", this.gameObject, eventData));
        }
        else
        {
            Debug.LogWarning("[QuestManager] EventBus.Instance is null — Quest.QuestAdded was not published.");
        }

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

        if (!completedQuests.TryGetValue(player, out var set))
        {
            set = new HashSet<string>();
            completedQuests[player] = set;
        }
        set.Add(inst.quest.questId);

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
        MessageManager.Instance.HandleSpawnWorld(string.Format("{0} Completed!", inst.quest.title));
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

    public QuestSaveData CollectActiveQuestsForPlayer(GameObject player)
    {
        QuestSaveData qsd = new QuestSaveData();
        if (player == null)
        {
            return qsd;
        }

        if (!activeQuests.TryGetValue(player, out var list) || list == null)
        {
            list = null;
        }

        if (list != null)
        {
            foreach (QuestInstance inst in list)
            {
                QuestInstanceSaveData sid = new QuestInstanceSaveData();
                sid.questId = inst.quest.questId;
                foreach (QuestRequirement req in inst.quest.requirements)
                {
                    sid.eventIds.Add(req.eventId);
                    sid.progressValues.Add(inst.GetProgress(req.eventId));
                }
                qsd.activeQuests.Add(sid);
            }
        }

        if (completedQuests.TryGetValue(player, out var completedSet) && completedSet != null)
        {
            foreach (string qid in completedSet)
            {
                qsd.completedQuestIds.Add(qid);
            }
        }

        return qsd;
    }

    public void RestoreActiveQuestsForPlayer(GameObject player, QuestSaveData saved)
    {
        if (player == null || saved == null)
        {
            return;
        }

        if (!completedQuests.ContainsKey(player))
        {
            completedQuests[player] = new HashSet<string>();
        }

        completedQuests[player].Clear();
        foreach (string id in saved.completedQuestIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                completedQuests[player].Add(id);
            }
        }

        if (activeQuests.TryGetValue(player, out var existingList) && existingList != null)
        {
            foreach (QuestInstance inst in existingList.ToList())
            {
                inst.Dispose();
            }
            existingList.Clear();
        }
        else
        {
            activeQuests[player] = new List<QuestInstance>();
        }

        QuestSO FindQuestSOById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            QuestSO[] all = Resources.LoadAll<QuestSO>("Quests");
            foreach (QuestSO q in all)
            {
                if (q != null && q.questId == id)
                {
                    return q;
                }
            }

            return null;
        }

        foreach (QuestInstanceSaveData sInst in saved.activeQuests)
        {
            if (completedQuests[player].Contains(sInst.questId))
            {
                Debug.Log($"[QuestManager] Skipping restore of active quest '{sInst.questId}' for {player.name} because it is marked completed.");
                continue;
            }

            QuestSO qso = FindQuestSOById(sInst.questId);
            if (qso == null)
            {
                Debug.LogWarning($"[QuestManager] Could not find QuestSO '{sInst.questId}' in Resources/Quests.");
                continue;
            }

            bool added = AddQuest(qso, player);
            if (!added)
            {
                continue;
            }

            QuestInstance instance = activeQuests[player].FirstOrDefault(x => x.quest.questId == sInst.questId);
            if (instance != null)
            {
                Dictionary<string, int> dict = new Dictionary<string, int>();
                for (int i = 0; i < sInst.eventIds.Count; i++)
                {
                    string id = sInst.eventIds[i];
                    int val = (i < sInst.progressValues.Count) ? sInst.progressValues[i] : 0;
                    dict[id] = val;
                }
                instance.RestoreProgress(dict);
            }
        }
    }
}
