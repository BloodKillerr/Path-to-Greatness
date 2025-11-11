using UnityEngine;

[CreateAssetMenu(menuName = "Supervisor/Reward")]
public class RewardSO : ScriptableObject
{
    public string Id;
    public RewardEffectSO Effect;
    public float weight = 1f;
}
