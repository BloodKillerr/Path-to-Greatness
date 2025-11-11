using System.Collections.Generic;
using UnityEngine;

public static class RewardGenerator
{
    public static RewardSO PickWeighted(List<RewardSO> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }
        float total = 0f;
        foreach (RewardSO r in pool)
        {
            total += Mathf.Max(0.0001f, r.weight);
        }
        float pick = Random.value * total;
        float running = 0f;
        foreach (RewardSO r in pool)
        {
            running += Mathf.Max(0.0001f, r.weight);
            if (pick <= running)
            {
                return r;
            }
        }
        return pool[pool.Count - 1];
    }
}
