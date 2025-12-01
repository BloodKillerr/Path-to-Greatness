using UnityEngine;
using UnityEngine.Events;

public enum EnemyGroupType
{
    Ground,
    Air
}

public enum EnemyType
{
    Skeleton,
    Dragon,
    Imp,

    Custom
}

public class EnemyStats : CharacterStats
{
    [Header("Classification")]
    public EnemyGroupType GroupType = EnemyGroupType.Ground;
    public EnemyType Type = EnemyType.Skeleton;

    public override void Die()
    {
        base.Die();
        Animator childAnimator = GetComponentInChildren<Animator>();
        if (childAnimator != null)
        {
            childAnimator.Play("Death");
        }
    }
}
