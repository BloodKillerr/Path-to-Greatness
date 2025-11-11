using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static StatUpgradeEffectSO;

[System.Serializable]
public class EventCap
{
    public string eventId;
    public int cap = 20;
}

[System.Serializable]
public class EventToPool
{
    public string eventId;
    public RewardPoolSO pool;
}

public class Supervisor : MonoBehaviour
{
    public List<EventCap> EventCaps = new List<EventCap>()
    {
        new EventCap { eventId = "Arrow.Health", cap = 20 },
        new EventCap { eventId = "Arrow.Agility", cap = 20 },
        new EventCap { eventId = "Arrow.Strength", cap = 20 }
    };

    public List<EventToPool> EventPools = new List<EventToPool>();

    private readonly Dictionary<GameObject, Dictionary<string, int>> eventCounts = new();

    private UnityAction<SupervisorEvent> _statUpgradeHandler;

    public static Supervisor Instance { get; private set; }

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

        _statUpgradeHandler = HandleStatUpgradeRequest;

        if(EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.Subscribe("Supervisor.StatUpgradeRequest", _statUpgradeHandler);
    }

    private void OnDestroy()
    {
        if (EventBus.Instance != null && _statUpgradeHandler != null)
        {
            EventBus.Instance.Unsubscribe("Supervisor.StatUpgradeRequest", _statUpgradeHandler);
        }
            
    }

    public int GetCapForEvent(string eventId)
    {
        foreach (EventCap ec in EventCaps)
        {
            if (ec.eventId == eventId)
            {
                return Mathf.Max(0, ec.cap);
            }
        }
            
        return -1;
    }

    private int GetAppliedCount(GameObject player, string eventId)
    {
        if (player == null)
        {
            return 0;
        }

        if (!eventCounts.TryGetValue(player, out var map))
        {
            return 0;
        }

        if (!map.TryGetValue(eventId, out var c))
        {
            return 0;
        }

        return c;
    }

    private void AddAppliedCount(GameObject player, string eventId, int amount)
    {
        if (player == null)
        {
            return;
        }

        if (!eventCounts.TryGetValue(player, out var map))
        {
            map = new Dictionary<string, int>();
            eventCounts[player] = map;
        }

        if (!map.TryGetValue(eventId, out var c))
        {
            c = 0;
        }

        map[eventId] = c + amount;
    }

    private RewardPoolSO GetPoolForEvent(string eventId)
    {
        foreach (EventToPool ep in EventPools)
        {
            if (ep.eventId == eventId)
            {
                return ep.pool;
            }  
        }
        return null;
    }

    #region Handlers
    private void HandleStatUpgradeRequest(SupervisorEvent ev)
    {
        GameObject player = ev.Get<GameObject>("target", null);
        string eventId = ev.Get<string>("eventId", ev.EventType);
        int requestedAmount = ev.Get<int>("amount", 1);

        if (player == null)
        {
            Debug.LogWarning("[Supervisor] StatUpgradeRequest missing target");
            return;
        }

        int cap = GetCapForEvent(eventId);
        int appliedSoFar = GetAppliedCount(player, eventId);
        int allowed;
        if (cap < 0)
        {
            allowed = requestedAmount;
        }
        else
        {
            allowed = Mathf.Max(0, Mathf.Min(requestedAmount, cap - appliedSoFar));
        }

        if (allowed <= 0)
        {
            Debug.Log($"[Supervisor] Event '{eventId}' reached cap for {player.name}. No upgrade applied.");
            var denialData = new Dictionary<string, object>()
            {
                { "target", player },
                { "eventId", eventId },
                { "requestedAmount", requestedAmount }
            };
            EventBus.Instance.Publish(new SupervisorEvent("Supervisor.EventCapReached", gameObject, denialData));
            return;
        }

        RewardPoolSO pool = GetPoolForEvent(eventId);

        if (pool == null || pool.Rewards == null || pool.Rewards.Count == 0)
        {
            StatType stat = ev.Get<StatType>("stat", StatType.Strength);
            StatUpgradeEffectSO effectSO = ScriptableObject.CreateInstance<StatUpgradeEffectSO>();
            effectSO.Type = stat;
            effectSO.Amount = allowed;
            int consumed = effectSO.ApplyWithLimit(player, allowed);
            AddAppliedCount(player, eventId, consumed);

            var rewardDataFallback = new Dictionary<string, object>()
            {
                { "target", player },
                { "eventId", eventId },
                { "stat", stat },
                { "appliedAmount", consumed }
            };
            EventBus.Instance.Publish(new SupervisorEvent("Supervisor.RewardGiven", gameObject, rewardDataFallback));

#if UNITY_EDITOR
            DestroyImmediate(effectSO);
#else
            Destroy(effectSO);
#endif
            return;
        }

        int remaining = allowed;
        var available = new List<RewardSO>(pool.Rewards);

        while (remaining > 0 && available.Count > 0)
        {
            RewardSO pick = RewardGenerator.PickWeighted(available);
            if (pick == null)
            {
                break;
            }

            int consumed = 0;
            if (pick.Effect != null)
            {
                consumed = pick.Effect.ApplyWithLimit(player, remaining);
            }

            if (consumed <= 0)
            {
                available.Remove(pick);
                continue;
            }

            remaining -= consumed;
            AddAppliedCount(player, eventId, consumed);

            var rewardData = new Dictionary<string, object>()
            {
                { "target", player },
                { "eventId", eventId },
                { "rewardId", pick.Id },
                { "appliedUnits", consumed }
            };
            EventBus.Instance.Publish(new SupervisorEvent("Supervisor.RewardGiven", gameObject, rewardData));
        }

        if (remaining > 0)
        {
            Debug.Log($"[Supervisor] Event '{eventId}' for {player.name} applied only {allowed - remaining}/{allowed} units (pool exhausted or all rewards blocked).");
            var partialData = new Dictionary<string, object>()
            {
                { "target", player },
                { "eventId", eventId },
                { "requested", requestedAmount },
                { "applied", allowed - remaining },
                { "remaining", remaining }
            };
            EventBus.Instance.Publish(new SupervisorEvent("Supervisor.RewardPartiallyApplied", gameObject, partialData));
        }
    }
    #endregion

    public int GetRemainingForPlayerEvent(GameObject player, string eventId)
    {
        int cap = GetCapForEvent(eventId);
        if (cap < 0)
        {
            return int.MaxValue;
        }
        int applied = GetAppliedCount(player, eventId);
        return Mathf.Max(0, cap - applied);
    }

    public void SetEventCap(string eventId, int newCap)
    {
        bool found = false;
        foreach (EventCap ec in EventCaps)
        {
            if (ec.eventId == eventId)
            {
                ec.cap = newCap; found = true; 
                break; 
            }
        }
        if (!found)
        {
            EventCaps.Add(new EventCap { eventId = eventId, cap = newCap });
        }
    }
}
