using UnityEngine;

public abstract class Ability : ScriptableObject
{
    public string AbilityName;
    public string Description;

    public float CooldownSeconds = 1f;

    public enum AbilityType { Passive, Active }
    public AbilityType type;

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }

    public virtual void Use() { }
}