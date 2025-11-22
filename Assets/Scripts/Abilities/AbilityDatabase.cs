using UnityEngine;

public class AbilityDatabase : MonoBehaviour
{
    public static AbilityDatabase Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Ability GetByName(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            return null;
        }
        Ability a = Resources.Load<Ability>($"Abilities/{abilityName}");
        if (a == null)
        {
            Debug.LogWarning($"[AbilityDatabase] Missing Resources/Abilities/{abilityName}");
        }
        return a;
    }
}