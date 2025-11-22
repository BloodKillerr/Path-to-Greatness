using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class AbilityManager : MonoBehaviour
{
    private List<Ability> currentAbilities = new List<Ability>();

    public UnityEvent OnPlayerAttack;

    public static AbilityManager Instance { get; private set; }
    public List<Ability> CurrentAbilities { get => currentAbilities; set => currentAbilities = value; }

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
        foreach (Ability ability in currentAbilities)
        {
            if (ability.AbilityName == newAbility.AbilityName)
            {
                return;
            }
        }

        Ability copy = Instantiate(newAbility);
        currentAbilities.Add(copy);
        copy.OnEquip();

        if (newAbility.type == Ability.AbilityType.Active)
        {
            OnPlayerAttack.AddListener(copy.OnPlayerAttack);
        }
        UIManager.Instance.UpdateAbilitiesUI();
    }

    public void RemoveAbility(Ability ability)
    {
        foreach (Ability ab in currentAbilities)
        {
            if (ab.AbilityName == ability.AbilityName)
            {
                ab.OnUnequip();

                if (ab.type == Ability.AbilityType.Active)
                {
                    OnPlayerAttack.RemoveListener(ab.OnPlayerAttack);
                }

                currentAbilities.Remove(ab);
                break;
            }
        }
        UIManager.Instance.UpdateAbilitiesUI();
    }

    public void RemoveAllAbilities()
    {
        foreach (Ability ability in currentAbilities.ToList())
        {
            RemoveAbility(ability);
        }

        currentAbilities.Clear();
        UIManager.Instance.UpdateAbilitiesUI();
    }

    public bool HasAbility(Type abilityType)
    {
        return currentAbilities.Any(a => a.GetType() == abilityType);
    }

    //public List<string> GetCurrentAbilityNames()
    //{
    //    return currentAbilities.Select(a => a.AbilityName).ToList();
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

    public void PlayerDidAttack() => OnPlayerAttack?.Invoke();
}