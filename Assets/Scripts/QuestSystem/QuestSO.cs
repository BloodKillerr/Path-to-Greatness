using System.Collections.Generic;
using UnityEngine;
using static StatUpgradeEffectSO;

public enum QuestRewardType 
{ 
    StatUpgrade,
    AbilityGrant 
}

[System.Serializable]
public class QuestRequirement
{
    public string eventId;

    public int requiredAmount = 1;
}

[CreateAssetMenu(menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public string questId;

    public string title;

    [TextArea] public string description;

    public List<QuestRequirement> requirements = new List<QuestRequirement>();

    public QuestRewardType rewardType = QuestRewardType.StatUpgrade;

    public StatType rewardStat = StatType.Magic;
    public int rewardAmount = 10;

    public string rewardAbilityName;

    public bool singleGrantPerPlayer = true;
}