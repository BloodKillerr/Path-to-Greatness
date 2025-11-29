using UnityEngine;

public class EnemyAnimationHandler : MonoBehaviour
{
    private EnemyStats enemyStats;

    private Animator animator;

    private Enemy enemy;

    public bool playSoundOnAttack = false;

    public Animator Animator { get => animator; set => animator = value; }

    private void Start()
    {
        enemyStats = GetComponentInParent<EnemyStats>();
        animator = GetComponent<Animator>();
        enemy = GetComponentInParent<Enemy>();
    }

    public void Spawn()
    {
        if (enemyStats != null)
        {
            enemyStats.IsInvincible = true;
        }
    }

    public void AfterSpawn()
    {
        if (enemyStats != null)
        {
            enemyStats.IsInvincible = false;
        }
    }

    public void Die()
    {
        enemy?.Die();
        Destroy(enemy != null ? enemy.gameObject : transform.root.gameObject);
    }

    public void EnableDamageCollider()
    {
        enemy?.DamageCollider?.EnableDamageCollider();
        if (playSoundOnAttack && enemy != null)
        {
            SoundManager.PlaySound(enemy.SoundType, GetComponentInParent<AudioSource>(), 1);
        }
    }

    // Called from animation events
    public void DisableDamageCollider()
    {
        enemy?.DamageCollider?.DisableDamageCollider();
        if (enemyStats != null)
        {
            enemyStats.IsInvincible = false;
        }
    }
}
