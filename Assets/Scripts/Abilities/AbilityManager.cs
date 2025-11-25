using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    private List<Ability> currentPassiveAbilities = new List<Ability>();
    private List<Ability> currentActiveAbilities = new List<Ability>();

    private Ability[] boundAbilities = new Ability[4];

    public Transform AbilitySpawnPoint;

    public static AbilityManager Instance { get; private set; }
    public List<Ability> CurrentPassiveAbilities { get => currentPassiveAbilities; set => currentPassiveAbilities = value; }
    public List<Ability> CurrentActiveAbilities { get => currentActiveAbilities; set => currentActiveAbilities = value; }
    public Ability[] BoundAbilities { get => boundAbilities; set => boundAbilities = value; }

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
    }

    public void AddAbility(Ability newAbility)
    {
        Ability copy = Instantiate(newAbility);

        if(copy.type == Ability.AbilityType.Passive)
        {
            foreach (Ability ability in currentPassiveAbilities)
            {
                if (ability.AbilityName == newAbility.AbilityName)
                {
                    return;
                }
            }

            currentPassiveAbilities.Add(copy);
        }
        else if(copy.type == Ability.AbilityType.Active)
        {
            foreach (Ability ability in currentActiveAbilities)
            {
                if (ability.AbilityName == newAbility.AbilityName)
                {
                    return;
                }
            }

            currentActiveAbilities.Add(copy);
        }
        
        copy.OnEquip();

        UIManager.Instance.UpdateAbilitiesUI();
    }

    public void RemoveAbility(Ability ability)
    {
        foreach (Ability ab in currentPassiveAbilities)
        {
            if (ab.AbilityName == ability.AbilityName)
            {
                ab.OnUnequip();

                currentPassiveAbilities.Remove(ab);
                break;
            }
        }

        foreach (Ability ab in currentActiveAbilities)
        {
            if (ab.AbilityName == ability.AbilityName)
            {
                ab.OnUnequip();

                currentActiveAbilities.Remove(ab);

                for (int i = 0; i < boundAbilities.Length; i++)
                {
                    if (boundAbilities[i] != null && boundAbilities[i].AbilityName == ab.AbilityName)
                    {
                        boundAbilities[i] = null;
                    }
                }

                break;
            }
        }

        UIManager.Instance.UpdateAbilitiesUI();
    }

    public void RemoveAllAbilities()
    {
        foreach (Ability ability in currentPassiveAbilities.ToList())
        {
            RemoveAbility(ability);
        }

        foreach (Ability ability in currentActiveAbilities.ToList())
        {
            RemoveAbility(ability);
        }

        currentPassiveAbilities.Clear();
        UIManager.Instance.UpdateAbilitiesUI();
    }

    public bool HasAbility(Type abilityType)
    {
        return currentPassiveAbilities.Any(a => a.GetType() == abilityType);
    }

    public bool BindAbilityToSlot(Ability ability, int slot)
    {
        if (slot < 0 || slot >= boundAbilities.Length)
        {
            return false;
        }

        if (ability == null)
        {
            UnbindSlot(slot);
            return true;
        }

        if (ability.type != Ability.AbilityType.Active)
        {
            Debug.LogWarning("[AbilityManager] Only active abilities can be bound to slots.");
            return false;
        }

        Ability runtimeInstance = currentActiveAbilities.FirstOrDefault(a => a.AbilityName == ability.AbilityName) ?? ability;

        for (int i = 0; i < boundAbilities.Length; i++)
        {
            if (boundAbilities[i] != null && boundAbilities[i].AbilityName == runtimeInstance.AbilityName)
            {
                boundAbilities[i] = null;
            }
        }

        boundAbilities[slot] = runtimeInstance;
        UIManager.Instance.UpdateAbilitiesUI();
        return true;
    }

    public void UnbindSlot(int slot)
    {
        if (slot < 0 || slot >= boundAbilities.Length)
        {
            return;
        }

        boundAbilities[slot] = null;
        UIManager.Instance.UpdateAbilitiesUI();
    }

    public Ability GetBoundAbility(int slot)
    {
        if (slot < 0 || slot >= boundAbilities.Length)
        {
            return null;
        }

        return boundAbilities[slot];
    }

    public void ActivateBoundAbility(int slot)
    {
        if (slot < 0 || slot >= boundAbilities.Length)
        {
            return;
        }

        Ability ability = boundAbilities[slot];
        if (ability == null)
        {
            return;
        }

        if (!AbilityCooldownManager.Instance.TryUse(ability))
        {
            return;
        }

        AbilityUseContext.SpawnPoint = AbilitySpawnPoint;

        ability.Use();

        AbilityUseContext.SpawnPoint = null;
    }

    //public List<string> GetCurrentAbilityNames()
    //{
    //    return currentPassiveAbilities.Select(a => a.AbilityName).ToList();
    //}

    //public void SetCurrentAbilitiesByName(List<string> names)
    //{
    //    RemoveAllAbilities();

    //    foreach (string name in names)
    //    {
    //        Ability prefab = AbilityDatabase.Instance.GetByName(name);
    //        if (prefab != null)
    //        {
    //            AddAbility(prefab);
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"[AbilityManager] Missing ability '{name}' in database");
    //        }
    //    }
    //}
}