using System.Collections;
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

    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private float regenInterval = .5f;

    private float lastDamageTime;
    private float lastMPUseTime;

    public Stat Agility { get => agility; set => agility = value; }
    public Stat Magic { get => magic; set => magic = value; }
    public Stat Health { get => health; set => health = value; }
    public Stat Strength { get => strength; set => strength = value; }
    public int CurrentMP
    {
        get => currentMP;
        set
        {
            if (value < currentMP)
            {
                lastMPUseTime = Time.time;
                CancelMPRegenStart();
                CancelInvoke(nameof(MPRegenTick));
                ScheduleMPRegen();
            }

            currentMP = Mathf.Clamp(value, 0, MaxMP);
            MPChanged?.Invoke(currentMP, MaxMP);
        }
    }
    public int MaxMP
    {
        get => maxMP;
        set
        {
            maxMP = value;
            currentMP = Mathf.Min(currentMP, maxMP);
            MPChanged?.Invoke(currentMP, MaxMP);
        }
    }

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

        lastDamageTime = Time.time;
        lastMPUseTime = Time.time;

        ScheduleHPRegen();
        ScheduleMPRegen();
    }

    public override void TakeDamage(int damage)
    {
        lastDamageTime = Time.time;

        CancelHPRegenStart();
        CancelInvoke(nameof(HPRegenTick));

        base.TakeDamage(damage);

        ScheduleHPRegen();
    }

    public bool UseMP(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }
        if (currentMP >= amount)
        {
            currentMP -= amount;
            lastMPUseTime = Time.time;

            CancelMPRegenStart();
            CancelInvoke(nameof(MPRegenTick));
            MPChanged?.Invoke(currentMP, MaxMP);
            ScheduleMPRegen();
            return true;
        }
        return false;
    }

    public void UpgradeHealth(int upgrade)
    {
        health.Upgrade(upgrade);
        int oldMax = MaxHealth;
        MaxHealth = 10 + (health.GetValue() * 10);

        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        HPChanged?.Invoke(CurrentHealth, MaxHealth);

        CancelHPRegenStart();
        CancelInvoke(nameof(HPRegenTick));
        ScheduleHPRegen();
        HealthChanged?.Invoke(health.GetBaseValue(), health.GetModifiersValue());
    }

    public void ModifyHealth(int modifier, bool remove)
    {
        if(remove)
        {
            health.RemoveModifier(modifier);
        }
        else if(modifier != 0)
        {
            health.AddModifier(modifier);
        }

        int oldMax = MaxHealth;
        MaxHealth = 10 + (health.GetValue() * 10);

        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        HPChanged?.Invoke(CurrentHealth, MaxHealth);

        CancelHPRegenStart();
        CancelInvoke(nameof(HPRegenTick));
        ScheduleHPRegen();
        HealthChanged?.Invoke(health.GetBaseValue(), health.GetModifiersValue());
    }

    public void UpgradeStrength(int upgrade)
    {
        strength.Upgrade(upgrade);
        Damage = (int)(strength.GetValue() * 1.5f);
        StrengthChanged?.Invoke(strength.GetBaseValue(), strength.GetModifiersValue());
    }

    public void ModifyStrength(int modifier, bool remove)
    {
        if (remove)
        {
            strength.RemoveModifier(modifier);
        }
        else if (modifier != 0)
        {
            strength.AddModifier(modifier);
        }

        Damage = (int)(strength.GetValue() * 1.5f);
        StrengthChanged?.Invoke(strength.GetBaseValue(), strength.GetModifiersValue());
    }

    public void UpgradeAgility(int upgrade)
    {
        agility.Upgrade(upgrade);
        Armor = agility.GetValue() / 2;
        AgilityChanged?.Invoke(agility.GetBaseValue(), agility.GetModifiersValue());
    }

    public void ModifyAgility(int modifier, bool remove)
    {
        if (remove)
        {
            agility.RemoveModifier(modifier);
        }
        else if (modifier != 0)
        {
            agility.AddModifier(modifier);
        }

        Armor = agility.GetValue() / 2;
        AgilityChanged?.Invoke(agility.GetBaseValue(), agility.GetModifiersValue());
    }

    public void UpgradeMagic(int upgrade)
    {
        magic.Upgrade(upgrade);
        int oldMax = MaxMP;
        MaxMP = 10 + (magic.GetValue() * 2);

        MPChanged?.Invoke(currentMP, MaxMP);
        CancelMPRegenStart();
        CancelInvoke(nameof(MPRegenTick));
        ScheduleMPRegen();
        MagicChanged?.Invoke(magic.GetBaseValue(), magic.GetModifiersValue());
    }

    public void ModifyMagic(int modifier, bool remove)
    {
        if (remove)
        {
            magic.RemoveModifier(modifier);
        }
        else if (modifier != 0)
        {
            magic.AddModifier(modifier);
        }

        int oldMax = MaxMP;
        MaxMP = 10 + (magic.GetValue() * 2);

        MPChanged?.Invoke(currentMP, MaxMP);
        CancelMPRegenStart();
        CancelInvoke(nameof(MPRegenTick));
        ScheduleMPRegen();
        MagicChanged?.Invoke(magic.GetBaseValue(), magic.GetModifiersValue());
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

    private void ScheduleHPRegen()
    {
        CancelHPRegenStart();

        if (CurrentHealth < MaxHealth)
        {
            Invoke(nameof(StartHPRegenRepeating), regenDelay);
        }
    }

    private void CancelHPRegenStart()
    {
        CancelInvoke(nameof(StartHPRegenRepeating));
    }

    private void StartHPRegenRepeating()
    {
        if (CurrentHealth < MaxHealth)
        {
            InvokeRepeating(nameof(HPRegenTick), 0f, regenInterval);
        }
    }

    private void HPRegenTick()
    {
        if (CurrentHealth >= MaxHealth)
        {
            CancelInvoke(nameof(HPRegenTick));
            return;
        }

        Heal(1);
    }

    private void ScheduleMPRegen()
    {
        CancelMPRegenStart();

        if (currentMP < MaxMP)
        {
            Invoke(nameof(StartMPRegenRepeating), regenDelay);
        }
    }

    private void CancelMPRegenStart()
    {
        CancelInvoke(nameof(StartMPRegenRepeating));
    }

    private void StartMPRegenRepeating()
    {
        if (currentMP < MaxMP)
        {
            InvokeRepeating(nameof(MPRegenTick), 0f, regenInterval);
        }
    }

    private void MPRegenTick()
    {
        if (currentMP >= MaxMP)
        {
            CancelInvoke(nameof(MPRegenTick));
            return;
        }

        RegainMP(1);
    }

    public PlayerStatsData CollectPlayerStatsState()
    {
        return new PlayerStatsData
        {
            healthBase = health.GetBaseValue(),
            strengthBase = strength.GetBaseValue(),
            agilityBase = agility.GetBaseValue(),
            magicBase = magic.GetBaseValue(),
            currentHealth = CurrentHealth,
            currentMP = CurrentMP,
            maxMP = MaxMP,
        };
    }

    public void RestorePlayerStats(PlayerStatsData d)
    {
        if (d == null)
        {
            return;
        }

        health.BaseValue = d.healthBase;
        strength.BaseValue = d.strengthBase;
        agility.BaseValue = d.agilityBase;
        magic.BaseValue = d.magicBase;

        MaxHealth = 10 + (health.GetValue() * 10);
        Armor = agility.GetValue() / 2;
        Damage = (int)(strength.GetValue() * 1.5f);
        MaxMP = 10 + (magic.GetValue() * 2);

        CurrentHealth = Mathf.Clamp(d.currentHealth, 0, MaxHealth);
        CurrentMP = Mathf.Clamp(d.currentMP, 0, MaxMP);

        lastDamageTime = Time.time;
        lastMPUseTime = Time.time;

        CancelHPRegenStart();
        CancelInvoke(nameof(HPRegenTick));
        ScheduleHPRegen();

        CancelMPRegenStart();
        CancelInvoke(nameof(MPRegenTick));
        ScheduleMPRegen();

        InvokeAllStats();
    }
}
