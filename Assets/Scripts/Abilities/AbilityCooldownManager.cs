using System.Collections.Generic;
using UnityEngine;

public class AbilityCooldownManager
{
    private static AbilityCooldownManager _instance;
    public static AbilityCooldownManager Instance => _instance ?? (_instance = new AbilityCooldownManager());

    // maps ability instance id -> next allowed Time.time
    private readonly Dictionary<int, float> nextAvailableTime = new Dictionary<int, float>();

    private AbilityCooldownManager() { }

    public bool TryUse(Ability ability)
    {
        if (ability == null)
        {
            return false;
        }

        int id = ability.GetInstanceID();
        float now = Time.time;

        if (nextAvailableTime.TryGetValue(id, out float allowedAt))
        {
            if (now < allowedAt)
            {
                return false;
            }
        }

        nextAvailableTime[id] = now + Mathf.Max(0f, ability.CooldownSeconds);
        return true;
    }

    public float GetTimeRemaining(Ability ability)
    {
        if (ability == null)
        {
            return 0f;
        }

        int id = ability.GetInstanceID();
        if (!nextAvailableTime.TryGetValue(id, out float allowedAt))
        {
            return 0f;
        }

        return Mathf.Max(0f, allowedAt - Time.time);
    }

    public void ResetCooldown(Ability ability)
    {
        if (ability == null)
        {
            return;
        }

        nextAvailableTime.Remove(ability.GetInstanceID());
    }
}
