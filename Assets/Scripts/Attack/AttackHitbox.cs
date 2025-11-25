using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int Damage = 10;

    [Header("Lifetime")]
    public float LifeTime = 2f;

    [Header("Collision")]
    public LayerMask hitLayers = ~0;

    private HashSet<int> hitEnemyIds = new HashSet<int>();

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        Damage = Player.Instance.GetComponent<PlayerStats>().Damage;
        if (LifeTime > 0f)
        {
            Destroy(gameObject, LifeTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
        {
            return;
        }

        EnemyStats enemy = other.GetComponent<EnemyStats>();
        if (enemy == null)
        {
            return;
        }
        
        int id = enemy.GetInstanceID();
        if (hitEnemyIds.Contains(id))
        {
            return;
        }

        hitEnemyIds.Add(id);

        enemy.TakeDamage(Damage);
    }
}
