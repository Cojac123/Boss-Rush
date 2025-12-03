using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    // -----------------------------
    // 1. VARIABLES (memory)
    // -----------------------------
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        Hurt,
        Dead
    }
    public BossDamageHitbox bossHitbox;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public Transform player;

    public BossState currentState = BossState.Idle;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float cooldownTimer = 0f;

    [Header("Sword")]
    [SerializeField] GameObject sword;        // boss sword object
    [SerializeField] Animator swordAnimator;  // boss sword animator (optional)

    [Header("Phases")]

    public int phase2Threshold = 50;
    public bool inPhase2 = false;

    [Header("Projectile Attack")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileCooldown = 3f;
    private float projectileTimer = 0f;

    // -----------------------------
    // 2. START (setup)
    // -----------------------------
    void Start()
    {
        currentHealth = maxHealth;
    }

    // -----------------------------
    // 3. UPDATE (heartbeat)
    // -----------------------------
    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        cooldownTimer -= Time.deltaTime;

        //  REQUIRED — counts down the projectile cooldown
        projectileTimer -= Time.deltaTime;

        switch (currentState)
        {
            case BossState.Idle: HandleIdle(distance); break;
            case BossState.Chase: HandleChase(distance); break;
            case BossState.Attack: HandleAttack(distance); break;
            case BossState.Hurt: HandleHurt(); break;
            case BossState.Dead: break;
        }

        // Phase 2 projectile logic
        if (inPhase2)
        {
            if (projectileTimer <= 0f)
            {
                ShootProjectile();
                projectileTimer = projectileCooldown;
            }
        }
    }

    // -----------------------------
    //  STATE HANDLERS
    // -----------------------------
    void HandleIdle(float distance)
    {
        if (distance < 10f)
            currentState = BossState.Chase;
    }

    void HandleChase(float distance)
    {
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            currentState = BossState.Attack;
        }
    }

    void HandleAttack(float distance)
    {
        AttemptAttack();

        if (distance > attackRange)
            currentState = BossState.Chase;
    }

    void HandleHurt()
    {
        currentState = BossState.Chase;
    }

    // -----------------------------
    //  ACTIONS
    // -----------------------------
    void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        transform.forward = lookDirection;
    }

    void AttemptAttack()
    {
        if (cooldownTimer <= 0f)
        {
            Debug.Log("Boss Attacks!");

            StartCoroutine(DoAttackTiming());
            cooldownTimer = attackCooldown;
        }
    }
    void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning("Projectile prefab or spawn point not assigned!");
            return;
        }

        // Make projectile
        GameObject p = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);

        Debug.Log("Boss shoots a projectile!");

    }

    IEnumerator DoAttackTiming()
    {
        // 1. Wind-up before attack
        yield return new WaitForSeconds(0.2f);

        // 2. Turn sword on and play swing animation
        if (sword != null)
        {
            sword.SetActive(true);

            Animator anim = swordAnimator != null
                ? swordAnimator
                : sword.GetComponent<Animator>();

            if (anim != null)
            {
                anim.SetTrigger("swing");
            }
        }

        // 3. Enable hitbox so it can actually do damage
        if (bossHitbox != null)
        {
            bossHitbox.EnableHitbox();
        }

        // 4. Attack is “active” for a short window
        yield return new WaitForSeconds(0.25f);

        // 5. Turn hitbox off
        if (bossHitbox != null)
        {
            bossHitbox.DisableHitbox();
        }

        // 6. Hide sword again
        if (sword != null)
        {
            sword.SetActive(false);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        Debug.Log("Boss takes " + amount + " damage! Health: " + currentHealth);

        if (currentHealth <= 0)
            Die();
        if(!inPhase2 && currentHealth <= phase2Threshold)
        {
            inPhase2 = true;
            Debug.Log("Boss has entered phase 2!");
        }
    }

    void Die()
    {
        Debug.Log("Boss has been defeated!");
        currentState = BossState.Dead;
        Destroy(gameObject);
    }
}
