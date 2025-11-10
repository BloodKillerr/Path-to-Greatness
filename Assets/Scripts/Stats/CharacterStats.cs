using UnityEngine;
using UnityEngine.Events;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] private string CharacterName;

    [SerializeField] private int armor;
    [SerializeField] private int damage;

    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;

    private bool isInvincible = false;

    public int Armor { get => armor; set => armor = value; }
    public int Damage { get => damage; set => damage = value; }
    public int CurrentHealth { get => currentHealth; set => currentHealth = value; }
    public int MaxHealth { get => maxHealth; set => maxHealth = value; }
    public bool IsInvincible { get => isInvincible; set => isInvincible = value; }

    public UnityEvent<int, int> HPChanged = new UnityEvent<int, int>();

    private void Awake()
    {
        currentHealth = maxHealth;
        HPChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible)
        {
            return;
        }

        int net = (damage * 100) / (100 + armor);
        net = Mathf.Clamp(net, 0, int.MaxValue);

        currentHealth = Mathf.Max(currentHealth - net, 0);

        HPChanged?.Invoke(currentHealth, maxHealth);

        CheckHealth();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HPChanged?.Invoke(currentHealth, maxHealth);
    }

    public void CheckHealth()
    {
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        //Die
    }
}
