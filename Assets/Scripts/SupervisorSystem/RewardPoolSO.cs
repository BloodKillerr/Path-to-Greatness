using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Supervisor/RewardPool")]
public class RewardPoolSO : ScriptableObject
{
    public List<RewardSO> Rewards = new List<RewardSO>();
}
