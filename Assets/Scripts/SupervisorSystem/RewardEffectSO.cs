using UnityEngine;

public abstract class RewardEffectSO : ScriptableObject
{
    public abstract void Apply(GameObject target);

    public virtual int ApplyWithLimit(GameObject target, int limit)
    {
        if (limit <= 0)
        {
            return 0;
        }
        Apply(target);
        return 1;
    }
}
