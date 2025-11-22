using UnityEngine;

[CreateAssetMenu(
    fileName = "SturdyPhysique",
    menuName = "Abilities/Passive/SturdyPhysique"
)]
public class SturdyPhysique : Ability
{
    public int boostAmount = 5;

    public override void OnEquip()
    {
        PlayerStats stats = Player.Instance.GetComponent<PlayerStats>();
        stats.ModifyHealth(boostAmount, false);
        stats.ModifyStrength(boostAmount, false);
        stats.ModifyAgility(boostAmount, false);
    }

    public override void OnUnequip()
    {
        PlayerStats stats = Player.Instance.GetComponent<PlayerStats>();
        stats.ModifyHealth(boostAmount, true);
        stats.ModifyStrength(boostAmount, true);
        stats.ModifyAgility(boostAmount, true);
    }
}
