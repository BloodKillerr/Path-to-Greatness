using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Enemy : MonoBehaviour, IEnemyDeathListener
{
    private EnemyStats enemyStats;

    protected NavMeshAgent agent;

    protected EnemyAnimationHandler animationHandler;

    public LayerMask PlayerLayer;

    [Header("Attack")]
    [SerializeField] protected float timeBetweenAttacks = 1.2f;
    protected bool attacked;

    [SerializeField] protected float attackRange = 2.0f;
    protected bool inAttackRange;

    [SerializeField] protected float rotationSpeed = 25f;

    [Header("Detection")]
    [SerializeField] protected float detectionRadius = 12f;

    [SerializeField] protected float lostRadius = 18f;

    protected bool playerDetected = false;

    [Header("Patrol")]
    [SerializeField] protected Transform[] patrolPoints;

    [SerializeField] protected float patrolRadius = 8f;
    [SerializeField] protected float patrolPointReachedThreshold = 1f;
    [SerializeField] protected float waitAtPatrolPoint = 2f;

    private int currentPatrolIndex = 0;
    private Vector3 spawnPosition;
    private Vector3 randomPatrolTarget;
    private bool hasRandomPatrolTarget = false;
    private float waitTimer = 0f;

    [SerializeField] private DamageCollider damageCollider;

    private EnemyHealthUI healthUI;

    //public SoundType SoundType;

    public event Action OnDeath;

    public DamageCollider DamageCollider { get => damageCollider; set => damageCollider = value; }
    public EnemyStats EnemyStats { get => enemyStats; set => enemyStats = value; }

    private enum State 
    { 
        Idle, 
        Patrol, 
        Chasing, 
        Attacking 
    }
    private State currentState = State.Idle;

    protected virtual void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        agent = GetComponent<NavMeshAgent>();
        animationHandler = GetComponentInChildren<EnemyAnimationHandler>();
        agent.updateRotation = false;
        damageCollider = GetComponentInChildren<DamageCollider>();

        spawnPosition = transform.position;
    }

    protected virtual void Start()
    {
        healthUI = GetComponentInChildren<EnemyHealthUI>();
        if (healthUI != null && enemyStats != null)
        {
            healthUI.Initialize(enemyStats);
        }

        currentState = (patrolPoints != null && patrolPoints.Length > 0) || patrolRadius > 0.01f ? State.Patrol : State.Idle;
    }

    protected virtual void Update()
    {
        if (enemyStats != null && enemyStats.IsInvincible)
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

        if (Player.Instance == null)
        {
            return;
        }

        Vector3 playerPos = Player.Instance.transform.position;
        float sqrDist = (playerPos - transform.position).sqrMagnitude;
        float detectionSqr = detectionRadius * detectionRadius;
        float lostSqr = lostRadius * lostRadius;

        if (!playerDetected)
        {
            if (sqrDist <= detectionSqr)
            {
                playerDetected = true;
                currentState = State.Chasing;
            }
        }
        else
        {
            if (sqrDist > lostSqr)
            {
                playerDetected = false;
                attacked = false;

                agent.ResetPath();
                agent.isStopped = false;

                hasRandomPatrolTarget = false;
                waitTimer = 0f;

                SetMovingAnimation(false);

                currentState = (patrolPoints != null && patrolPoints.Length > 0 || patrolRadius > 0.01f) ? State.Patrol : State.Idle;
                return;
            }
        }

        switch (currentState)
        {
            case State.Idle:
                SetMovingAnimation(false);
                if (playerDetected)
                {
                    currentState = State.Chasing;
                }

                break;

            case State.Patrol:
                PatrolBehaviour();
                if (playerDetected)
                {
                    currentState = State.Chasing;
                }

                break;

            case State.Chasing:
                agent.SetDestination(Player.Instance.transform.position);
                inAttackRange = sqrDist <= (attackRange * attackRange);

                if (inAttackRange)
                {
                    currentState = State.Attacking;
                }
                else
                {
                    Chase();
                }
                break;

            case State.Attacking:
                agent.SetDestination(Player.Instance.transform.position);
                inAttackRange = sqrDist <= (attackRange * attackRange);

                if (!inAttackRange)
                {
                    currentState = State.Chasing;
                    break;
                }

                Attack();
                break;
        }
    }

    private void PatrolBehaviour()
    {
        if (enemyStats != null && enemyStats.CurrentHealth <= 0)
        {
            agent.ResetPath();
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null)
            {
                return;
            }

            agent.SetDestination(targetPoint.position);

            float dist = Vector3.Distance(transform.position, targetPoint.position);
            if (dist <= patrolPointReachedThreshold)
            {
                waitTimer += Time.deltaTime;
                SetMovingAnimation(false);
                if (waitTimer >= waitAtPatrolPoint)
                {
                    waitTimer = 0f;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
            }
            else
            {
                SetMovingAnimation(true);
                FaceMovementDirection();
            }
        }
        else
        {
            if (!hasRandomPatrolTarget)
            {
                randomPatrolTarget = RandomNavSphere(spawnPosition, patrolRadius, -1);
                hasRandomPatrolTarget = true;
                agent.SetDestination(randomPatrolTarget);
                SetMovingAnimation(true);
            }
            else
            {
                float dist = Vector3.Distance(transform.position, randomPatrolTarget);
                if (dist <= patrolPointReachedThreshold)
                {
                    hasRandomPatrolTarget = false;
                    waitTimer = 0f;
                    agent.ResetPath();
                    SetMovingAnimation(false);
                }
                else
                {
                    SetMovingAnimation(true);
                    FaceMovementDirection();
                }
            }
        }
    }

    private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layerMask)
    {
        Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }

    private void Chase()
    {
        if (enemyStats != null && enemyStats.CurrentHealth <= 0)
        {
            agent.ResetPath();
            return;
        }

        SetMovingAnimation(true);

        FaceMovementDirection();
    }

    private void Attack()
    {
        if (enemyStats != null && enemyStats.CurrentHealth <= 0)
        {
            agent.ResetPath();
            return;
        }

        SetMovingAnimation(true, 0.8f);

        FacePlayerDirection();

        if (!attacked)
        {
            AttackAction();

            attacked = true;
            Invoke(nameof(ResetAtack), timeBetweenAttacks);
        }
    }

    private void ResetAtack()
    {
        attacked = false;
    }

    private void FaceMovementDirection()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0;
        if (vel.sqrMagnitude > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(vel);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desired,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void FacePlayerDirection()
    {
        if (Player.Instance == null)
        {
            return;
        }

        Vector3 lookPos = Player.Instance.transform.position;
        lookPos.y = transform.position.y;
        Vector3 dir = (lookPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion desired = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * Time.deltaTime);
    }

    private void SetMovingAnimation(bool moving, float blend = 1f)
    {
        if (animationHandler == null || animationHandler.Animator == null)
        {
            return;
        }

        float value = moving ? blend : 0f;
        animationHandler.Animator.SetFloat("Vertical", value, 0.1f, Time.deltaTime);
    }

    public virtual void AttackAction()
    {
        if (animationHandler != null && animationHandler.Animator != null)
        {
            animationHandler.Animator.CrossFade("Attack", 0.15f);
        }
    }

    public virtual void Die()
    {
        //Actions on death
        OnDeath?.Invoke();
    }
}