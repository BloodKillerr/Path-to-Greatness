using UnityEngine;

[CreateAssetMenu(menuName = "SupervisorEffects/StatUpgrade")]
public class StatUpgradeEffectSO : RewardEffectSO
{
    public enum StatType
    {
        Health,
        Strength,
        Agility,
        Magic
    }

    public StatType Type;
    public int Amount;

    public override void Apply(GameObject target)
    {
        PlayerStats ps = target.GetComponent<PlayerStats>();
        if (ps == null) 
        { 
            Debug.LogWarning("Target has no PlayerStats"); 
            return;
        }
        switch (Type)
        {
            case StatType.Health:
                ps.UpgradeHealth(Amount);
                break;
            case StatType.Strength:
                ps.UpgradeStrength(Amount);
                break;
            case StatType.Agility:
                ps.UpgradeAgility(Amount);
                break;
            case StatType.Magic:
                ps.UpgradeMagic(Amount);
                break;
        }

        ps.InvokeAllStats();
    }

    public override int ApplyWithLimit(GameObject target, int limit)
    {
        if (limit <= 0)
        {
            return 0;
        }
        PlayerStats ps = target.GetComponent<PlayerStats>();
        if (ps == null)
        {
            Debug.LogWarning("[StatUpgradeEffectSO] target has no PlayerStats.");
            return 0;
        }

        int toApply = Mathf.Min(Amount, limit);
        if (toApply <= 0)
        {
            return 0;
        }

        switch (Type)
        {
            case StatType.Health: 
                ps.UpgradeHealth(toApply); 
                break;
            case StatType.Strength: 
                ps.UpgradeStrength(toApply);
                break;
            case StatType.Agility: 
                ps.UpgradeAgility(toApply); 
                break;
            case StatType.Magic: 
                ps.UpgradeMagic(toApply); 
                break;
        }
        ps.InvokeAllStats();
        return toApply;
    }
}
