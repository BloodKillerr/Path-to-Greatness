using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : CharacterStats
{
    [SerializeField] private Stat health;

    [SerializeField] private Stat strength;

    [SerializeField] private Stat agility;

    [SerializeField] private Stat magic;

    private int currentMP;

    private int maxMP;

    public Stat Agility { get => agility; set => agility = value; }
    public Stat Magic { get => magic; set => magic = value; }
    public Stat Health { get => health; set => health = value; }
    public Stat Strength { get => strength; set => strength = value; }
    public int CurrentMP { get => currentMP; set => currentMP = value; }
    public int MaxMP { get => maxMP; set => maxMP = value; }

    public UnityEvent<int, int> MPChanged = new UnityEvent<int, int>();

    public UnityEvent<int, int> HealthChanged = new UnityEvent<int, int> ();
    public UnityEvent<int, int> StrengthChanged = new UnityEvent<int, int>();
    public UnityEvent<int, int> AgilityChanged = new UnityEvent<int, int>();
    public UnityEvent<int, int> MagicChanged = new UnityEvent<int, int>();

    private void Awake()
    {
        MaxHealth = 10 + (health.GetValue() * 10);
        CurrentHealth = MaxHealth;
        HPChanged?.Invoke(CurrentHealth, MaxHealth);
        Armor = agility.GetValue() / 2;
        Damage = (int)(strength.GetValue() * 1.5f);
        maxMP = 10 + (magic.GetValue() * 2);
        currentMP = maxMP;
        HPChanged?.Invoke(CurrentHealth, MaxHealth);
        MPChanged?.Invoke(currentMP, MaxMP);
        HealthChanged?.Invoke(health.GetBaseValue(), health.GetModifiersValue());
        StrengthChanged?.Invoke(strength.GetBaseValue(), strength.GetModifiersValue());
        AgilityChanged?.Invoke(agility.GetBaseValue(), agility.GetModifiersValue());
        MagicChanged?.Invoke(magic.GetBaseValue(), magic.GetModifiersValue());
    }

    public void UpgradeHealth(int upgrade)
    {
        health.Upgrade(upgrade);
        MaxHealth = 10 + (health.GetValue() * 10);
        Heal(upgrade);
    }

    public void UpgradeStrength(int upgrade)
    {
        strength.Upgrade(upgrade);
        Damage = (int)(strength.GetValue() * 1.5f);
    }

    public void UpgradeAgility(int upgrade)
    {
        agility.Upgrade(upgrade);
        Armor = agility.GetValue() / 2;
    }

    public void UpgradeMagic(int upgrade)
    {
        magic.Upgrade(upgrade);
        maxMP = 10 + (magic.GetValue() * 2);
        RegainMP(upgrade);
    }

    public void RegainMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, MaxMP);
        MPChanged?.Invoke(currentMP, maxMP);
    }

    public void InvokeAllStats()
    {
        HPChanged?.Invoke(CurrentHealth, MaxHealth);
        MPChanged?.Invoke(currentMP, MaxMP);
        HealthChanged?.Invoke(health.GetBaseValue(), health.GetModifiersValue());
        StrengthChanged?.Invoke(strength.GetBaseValue(), strength.GetModifiersValue());
        AgilityChanged?.Invoke(agility.GetBaseValue(), agility.GetModifiersValue());
        MagicChanged?.Invoke(magic.GetBaseValue(), magic.GetModifiersValue());
    }
}
