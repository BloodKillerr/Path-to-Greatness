using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Stat
{
    [SerializeField] private int baseValue;

    private List<int> modifiers = new List<int>();

    public UnityEvent ValueChanged = new UnityEvent();

    public int BaseValue { get => baseValue; set => baseValue = value; }

    public int GetValue()
    {
        int finalValue = baseValue;

        modifiers.ForEach(x => finalValue += x);

        return finalValue;
    }

    public void AddModifier(int modifier)
    {
        if (modifier != 0)
        {
            modifiers.Add(modifier);
            ValueChanged?.Invoke();
        }
    }

    public void RemoveModifier(int modifier)
    {
        if (modifier != 0)
        {
            modifiers.Remove(modifier);
            ValueChanged?.Invoke();
        }
    }

    public void Upgrade(int upgrade)
    {
        baseValue += upgrade;
    }

    public int GetBaseValue()
    {
        return baseValue;
    }
}
