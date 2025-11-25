using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    protected Collider damageCollider;

    private void Awake()
    {
        damageCollider = GetComponent<Collider>();
        damageCollider.gameObject.SetActive(true);
        damageCollider.enabled = false;
    }

    public void EnableDamageCollider()
    {
        damageCollider.enabled = true;
    }

    public void DisableDamageCollider()
    {
        damageCollider.enabled = false;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!damageCollider.enabled)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            EnemyStats ownStats = gameObject.GetComponentInParent<EnemyStats>();

            if (stats != null && ownStats != null)
            {
                stats.TakeDamage(ownStats.Damage);
            }
        }
    }
}