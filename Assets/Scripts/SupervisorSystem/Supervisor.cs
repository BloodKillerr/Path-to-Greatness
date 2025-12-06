using System.Collections.Generic;
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

[System.Serializable]
public class StatAbilityBundle
{
    public string id;

    public int healthThreshold = 20;
    public int strengthThreshold = 20;
    public int agilityThreshold = 20;
    public int magicThreshold = 0;

    public string abilityName;
    public string abilityDesc;

    public bool singleGrantPerPlayer = true;
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

    public List<StatAbilityBundle> statAbilityBundles = new List<StatAbilityBundle>();

    private readonly Dictionary<GameObject, Dictionary<StatType, int>> perPlayerStatGains = new();

    private readonly Dictionary<GameObject, HashSet<string>> perPlayerGrantedAbilityIds = new();

    private UnityAction<SupervisorEvent> _statUpgradeHandler;

    private UnityAction<SupervisorEvent> _statGainedHandler;

    private UnityAction<SupervisorEvent> _grantAbilityHandler;

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

        _statGainedHandler = HandleStatGained;

        _grantAbilityHandler = HandleGrantAbilityRequest;

        if (EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.Subscribe("Supervisor.StatGained", _statGainedHandler);

        EventBus.Instance.Subscribe("Supervisor.StatUpgradeRequest", _statUpgradeHandler);

        EventBus.Instance.Subscribe("Supervisor.GrantAbilityRequest", _grantAbilityHandler);
    }

    private void OnDestroy()
    {
        if (EventBus.Instance != null && _statUpgradeHandler != null)
        {
            EventBus.Instance.Unsubscribe("Supervisor.StatUpgradeRequest", _statUpgradeHandler);
            EventBus.Instance.Unsubscribe("Supervisor.StatGained", _statGainedHandler);
            EventBus.Instance.Unsubscribe("Supervisor.GrantAbilityRequest", _grantAbilityHandler);
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

    private void HandleStatGained(SupervisorEvent ev)
    {
        GameObject player = ev.Get<GameObject>("target", null);
        StatType stat = ev.Get<StatType>("stat", StatType.Strength);
        int amount = ev.Get<int>("amount", 0);

        if (player == null || amount <= 0)
        {
            return;
        }

        AddStatGain(player, stat, amount);
        foreach (StatAbilityBundle bundle in statAbilityBundles)
        {
            if (bundle == null || string.IsNullOrEmpty(bundle.abilityName))
            {
                continue;
            }

            if (bundle.singleGrantPerPlayer && HasPlayerBeenGranted(player, bundle.id))
            {
                continue;
            }

            int h = GetAccumulatedStatGain(player, StatType.Health);
            int s = GetAccumulatedStatGain(player, StatType.Strength);
            int a = GetAccumulatedStatGain(player, StatType.Agility);
            int m = GetAccumulatedStatGain(player, StatType.Magic);

            if (h >= bundle.healthThreshold && s >= bundle.strengthThreshold && a >= bundle.agilityThreshold && m >= bundle.magicThreshold)
            {
                GrantAbilityToPlayer(player, bundle);
            }
        }
    }

    private void HandleGrantAbilityRequest(SupervisorEvent ev)
    {
        GameObject player = ev.Get<GameObject>("target", null);
        string abilityName = ev.Get<string>("abilityName", null);
        string abilityDesc = ev.Get<string>("abilityDesc", null);
        string bundleId = ev.Get<string>("bundleId", null);
        bool singleGrant = ev.Get<bool>("singleGrant", true);

        if (player == null || string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning("[Supervisor] GrantAbilityRequest missing target or abilityName");
            return;
        }

        string idToMark = string.IsNullOrEmpty(bundleId) ? abilityName : bundleId;
        if (singleGrant && HasPlayerBeenGranted(player, idToMark))
        {
            Debug.Log($"[Supervisor] Player already granted '{idToMark}'. Skipping.");
            return;
        }

        Ability abilityPrefab = AbilityDatabase.Instance?.GetByName(abilityName);
        if (abilityPrefab == null)
        {
            Debug.LogWarning($"[Supervisor] GrantAbilityRequest: ability '{abilityName}' not found.");
            return;
        }

        AbilityManager.Instance?.AddAbility(abilityPrefab);
        if (singleGrant)
        {
            MarkPlayerGranted(player, idToMark);
        }

        var data = new Dictionary<string, object>()
        {
            { "target", player },
            { "abilityName", abilityName },
            { "abilityDesc", abilityDesc },
            { "bundleId", idToMark }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Supervisor.AbilityGranted", gameObject, data));
    }

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

    public int GetAccumulatedStatGain(GameObject player, StatType stat)
    {
        if (player == null)
        {
            return 0;
        }

        if (!perPlayerStatGains.TryGetValue(player, out var map))
        {
            return 0;
        }

        if (!map.TryGetValue(stat, out var val))
        {
            return 0;
        }

        return val;
    }

    private void AddStatGain(GameObject player, StatType stat, int amount)
    {
        if (player == null || amount == 0)
        {
            return;
        }

        if (!perPlayerStatGains.TryGetValue(player, out var map))
        {
            map = new Dictionary<StatType, int>();
            perPlayerStatGains[player] = map;
        }

        if (!map.TryGetValue(stat, out var current))
        {
            current = 0;
        }

        map[stat] = current + amount;
    }

    private bool HasPlayerBeenGranted(GameObject player, string bundleId)
    {
        if (player == null)
        {
            return false;
        }

        if (!perPlayerGrantedAbilityIds.TryGetValue(player, out var set))
        {
            return false;
        }

        return set.Contains(bundleId);
    }

    private void MarkPlayerGranted(GameObject player, string bundleId)
    {
        if (player == null)
        {
            return;
        }

        if (!perPlayerGrantedAbilityIds.TryGetValue(player, out var set))
        {
            set = new HashSet<string>();
            perPlayerGrantedAbilityIds[player] = set;
        }
        set.Add(bundleId);
    }

    private void GrantAbilityToPlayer(GameObject player, StatAbilityBundle bundle)
    {
        if (player == null || bundle == null)
        {
            return;
        }

        Ability abilityPrefab = AbilityDatabase.Instance?.GetByName(bundle.abilityName);
        if (abilityPrefab == null)
        {
            Debug.LogWarning($"[Supervisor] Unable to find ability '{bundle.abilityName}' to grant for bundle '{bundle.id}'.");
            return;
        }
        AbilityManager.Instance?.AddAbility(abilityPrefab);
        if (bundle.singleGrantPerPlayer)
        {
            MarkPlayerGranted(player, bundle.id);
        }

        Debug.Log($"[Supervisor] Granted ability '{bundle.abilityName}' to {player.name} (bundle {bundle.id}).");
        var data = new Dictionary<string, object>()
        {
            { "target", player },
            { "bundleId", bundle.id },
            { "abilityName", bundle.abilityName },
            { "abilityDesc", bundle.abilityDesc }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Supervisor.AbilityGranted", gameObject, data));
    }
    public void ResetAccumulatedGainsForPlayer(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        if (perPlayerStatGains.ContainsKey(player))
        {
            perPlayerStatGains[player].Clear();
        }

        if (perPlayerGrantedAbilityIds.ContainsKey(player))
        {
            perPlayerGrantedAbilityIds[player].Clear();
        }
    }

    public SupervisorSaveData CollectSupervisorState()
    {
        SupervisorSaveData d = new SupervisorSaveData();

        d.eventCaps = EventCaps != null ? new List<EventCap>(EventCaps) : new List<EventCap>();

        GameObject p = Player.Instance?.gameObject;
        if (p == null)
        {
            return d;
        }

        if (eventCounts.TryGetValue(p, out var map))
        {
            foreach (var kv in map)
            {
                d.eventCountIds.Add(kv.Key);
                d.eventCountValues.Add(kv.Value);
            }
        }

        if (perPlayerStatGains.TryGetValue(p, out var statMap))
        {
            foreach (var kv in statMap)
            {
                d.statGainTypes.Add(kv.Key);
                d.statGainValues.Add(kv.Value);
            }
        }

        if (perPlayerGrantedAbilityIds.TryGetValue(p, out var set))
        {
            foreach (string id in set)
            {
                d.grantedAbilityIds.Add(id);
            }
        }

        return d;
    }

    public void RestoreSupervisorState(SupervisorSaveData d)
    {
        if (d == null)
        {
            return;
        }

        EventCaps = d.eventCaps != null ? new List<EventCap>(d.eventCaps) : new List<EventCap>();

        GameObject p = Player.Instance?.gameObject;
        if (p == null)
        {
            return;
        }

        if (!eventCounts.ContainsKey(p))
        {
            eventCounts[p] = new Dictionary<string, int>();
        }

        eventCounts[p].Clear();
        for (int i = 0; i < d.eventCountIds.Count && i < d.eventCountValues.Count; i++)
        {
            eventCounts[p][d.eventCountIds[i]] = d.eventCountValues[i];
        }

        if (!perPlayerStatGains.ContainsKey(p))
        {
            perPlayerStatGains[p] = new Dictionary<StatType, int>();
        }

        perPlayerStatGains[p].Clear();
        for (int i = 0; i < d.statGainTypes.Count && i < d.statGainValues.Count; i++)
        {
            perPlayerStatGains[p][d.statGainTypes[i]] = d.statGainValues[i];
        }

        if (!perPlayerGrantedAbilityIds.ContainsKey(p))
        {
            perPlayerGrantedAbilityIds[p] = new HashSet<string>();
        }

        perPlayerGrantedAbilityIds[p].Clear();
        foreach (string id in d.grantedAbilityIds)
        {
            perPlayerGrantedAbilityIds[p].Add(id);

            Ability abilityPrefab = AbilityDatabase.Instance?.GetByName(id);
            if (abilityPrefab != null)
            {
                AbilityManager.Instance?.AddAbility(abilityPrefab);
            }
        }
    }
}
