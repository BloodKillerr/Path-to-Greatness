using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using static StatUpgradeEffectSO;

[RequireComponent(typeof(PlayerStats))]
public class StatChangeReporter : MonoBehaviour
{
    private PlayerStats ps;

    private int prevHealthBase;
    private int prevStrengthBase;
    private int prevAgilityBase;
    private int prevMagicBase;

    private void Awake()
    {
        ps = GetComponent<PlayerStats>();
        if (ps == null)
        {
            Debug.LogError("[StatChangeReporter] PlayerStats missing!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        prevHealthBase = ps.Health?.GetBaseValue() ?? 0;
        prevStrengthBase = ps.Strength?.GetBaseValue() ?? 0;
        prevAgilityBase = ps.Agility?.GetBaseValue() ?? 0;
        prevMagicBase = ps.Magic?.GetBaseValue() ?? 0;
        ps.HealthChanged.AddListener(OnHealthChanged);
        ps.StrengthChanged.AddListener(OnStrengthChanged);
        ps.AgilityChanged.AddListener(OnAgilityChanged);
        ps.MagicChanged.AddListener(OnMagicChanged);
    }

    private void OnDisable()
    {
        if (ps != null)
        {
            ps.HealthChanged.RemoveListener(OnHealthChanged);
            ps.StrengthChanged.RemoveListener(OnStrengthChanged);
            ps.AgilityChanged.RemoveListener(OnAgilityChanged);
            ps.MagicChanged.RemoveListener(OnMagicChanged);
        }
    }

    private void OnHealthChanged(int newBase, int modifiersValue)
    {
        int delta = newBase - prevHealthBase;
        prevHealthBase = newBase;
        if (delta > 0)
        {
            PublishStatGained(StatType.Health, delta);
        }
    }

    private void OnStrengthChanged(int newBase, int modifiersValue)
    {
        int delta = newBase - prevStrengthBase;
        prevStrengthBase = newBase;
        if (delta > 0)
        {
            PublishStatGained(StatType.Strength, delta);
        }
    }

    private void OnMagicChanged(int newBase, int modifiersValue)
    {
        int delta = newBase - prevMagicBase;
        prevMagicBase = newBase;
        if (delta > 0)
        {
            PublishStatGained(StatType.Magic, delta);
        }
    }

    private void OnAgilityChanged(int newBase, int modifiersValue)
    {
        int delta = newBase - prevAgilityBase;
        prevAgilityBase = newBase;
        if (delta > 0)
        {
            PublishStatGained(StatType.Agility, delta);
        }
    }

    private void PublishStatGained(StatType stat, int amount)
    {
        var data = new Dictionary<string, object>()
        {
            { "target", gameObject },
            { "stat", stat },
            { "amount", amount }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Supervisor.StatGained", gameObject, data));
    }
}
