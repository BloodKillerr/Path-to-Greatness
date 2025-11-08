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

    private void Awake()
    {
        MaxHealth = health.GetValue() * 10;
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Armor = agility.GetValue() / 2;
        Damage = (int)(strength.GetValue() * 1.5f);
        maxMP = magic.GetValue() * 2;
        currentMP = maxMP;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        MPChanged?.Invoke(currentMP, MaxMP);
    }

    public void UpgradeHealth(int upgrade)
    {
        health.Upgrade(upgrade);
        MaxHealth = health.GetValue() * 100;
        Heal(upgrade);
    }
}
