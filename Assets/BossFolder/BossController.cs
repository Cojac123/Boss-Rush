using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    // -----------------------------
    // VARIABLES
    // -----------------------------
    public enum BossState { Idle, Chase, Attack, Hurt, Dead }
    public BossDamageHitbox bossHitbox;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public Transform player;
    public BossState currentState = BossState.Idle;

    [Header("Melee Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float cooldownTimer = 0f;

    [Header("Sword")]
    [SerializeField] GameObject sword;
    [SerializeField] Animator swordAnimator;

    [Header("Projectile Attack")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileCooldown = 3f;
    private float projectileTimer = 0f;

    [Header("Ultimate Attack")]
    public float ultimateCooldown = 25f;      // how often the ult is allowed
    private float ultimateTimer = 0f;         // counts down every frame
    public GameObject shockwavePrefab;        // prefab for the shockwave
    public Transform shockwaveSpawnPoint;     // where the wave starts
    public float ultimateTelegraphTime = 1f;  // boss charges glow time
    private bool isDoingUltimate = false;     // prevents overlapping attacks
    
    [Header("Phases")]
    public int phase2Threshold = 50;
    public bool inPhase2 = false;
    public int phase3Threshold = 20;
    public bool inPhase3 = false;

    [Header("Hurt Reaction")]
    public float knockbackForce = 8f;
    public float stunDuration = 0.3f;
    private bool isStunned = false;


    // -----------------------------
    // START
    // -----------------------------
    void Start()
    {
        currentHealth = maxHealth;
    }

    // -----------------------------
    // UPDATE
    // -----------------------------
    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        cooldownTimer -= Time.deltaTime;
        projectileTimer -= Time.deltaTime;

        // -----------------------------
        // PHASE 3 — ALWAYS OVERRIDES
        // -----------------------------
        if (inPhase3)
        {
            HandlePhase3Behavior(distance);
            return;
        }

        // -----------------------------
        // PHASE 2 — RANGED ONLY
        // -----------------------------
        if (inPhase2)
        {
            HandlePhase2Behavior(distance);
            return;
        }

        // -----------------------------
        // PHASE 1 — NORMAL FSM
        // -----------------------------
        switch (currentState)
        {
            case BossState.Idle: HandleIdle(distance); break;
            case BossState.Chase: HandleChase(distance); break;
            case BossState.Attack: HandleAttack(distance); break;
            case BossState.Hurt: HandleHurt(); break;
            case BossState.Dead: break;
        }
    }

    // -----------------------------
    // PHASE 1 FUNCTIONS
    // -----------------------------
    void HandleIdle(float distance)
    {
        if (distance < 10f)
            currentState = BossState.Chase;
    }

    void HandleChase(float distance)
    {
        if (distance > attackRange)
            MoveTowardsPlayer();
        else
            currentState = BossState.Attack;
    }

    void HandleAttack(float distance)
    {
        AttemptAttack();
        if (distance > attackRange)
            currentState = BossState.Chase;
    }

    void HandleHurt()
    {
        if (!isStunned)
            currentState = BossState.Chase;
    }

    // -----------------------------
    // PHASE 2 — RANGED MOVEMENT
    // -----------------------------
    void HandlePhase2Behavior(float distance)
    {
        float desiredRange = 7f;

        if (distance < desiredRange)
        {
            Vector3 away = (transform.position - player.position).normalized;
            transform.position += away * (moveSpeed * 1.5f) * Time.deltaTime;
        }
        else
        {
            transform.LookAt(player.position);
        }

        if (projectileTimer <= 0f)
        {
            ShootProjectile();
            projectileTimer = projectileCooldown * 0.7f;
        }
    }

    // -----------------------------
    // ⭐ PHASE 3 — FINAL FORM ⭐
    // -----------------------------
    void HandlePhase3Behavior(float distance)
    {
        if (ultimateTimer > 0f)
            ultimateTimer -= Time.deltaTime;

        transform.LookAt(player.position);

        if (!isDoingUltimate && ultimateTimer <= 0f && distance > 6f)
        {
            StartCoroutine(DoUltimateAttack());
            ultimateTimer = ultimateCooldown;
            return; // stop normal Phase 3 actions while ulting
        }


        // Ultimate rapid-fire
        if (projectileTimer <= 0f)
        {
            ShootProjectile();
            ShootProjectile();
            projectileTimer = projectileCooldown * 0.4f;
        }

        // Melee if close
        if (distance < attackRange + 1f)
            AttemptAttack();

        // Aggressive chase at distance
        if (distance > 5f)
        {
            Vector3 toward = (player.position - transform.position).normalized;
            toward.y = 0;
            transform.position += toward * (moveSpeed * 2f) * Time.deltaTime;
        }
        // ⭐ ULTIMATE ATTACK FUNCTION ⭐
        IEnumerator DoUltimateAttack()
        {
            isDoingUltimate = true;

            // 1. TELEGRAPH
            Debug.Log("Boss begins ULTIMATE telegraph!");
            yield return new WaitForSeconds(ultimateTelegraphTime);

            // 2. BIG SLASH
            if (sword != null)
            {
                sword.SetActive(true);
                Animator anim = swordAnimator != null ? swordAnimator : sword.GetComponent<Animator>();
                if (anim != null) anim.SetTrigger("swing");
            }

            if (bossHitbox != null)
            {
                bossHitbox.damageAmount = 40;
                bossHitbox.EnableHitbox();
            }

            yield return new WaitForSeconds(0.3f);

            if (bossHitbox != null) bossHitbox.DisableHitbox();
            if (sword != null) sword.SetActive(false);

            // 3. SHOCKWAVE
            if (shockwavePrefab != null && shockwaveSpawnPoint != null)
            {
                Instantiate(shockwavePrefab, shockwaveSpawnPoint.position, shockwaveSpawnPoint.rotation);
                Debug.Log("Shockwave released!");
            }

            // 4. Post-Ult Pause
            yield return new WaitForSeconds(0.5f);

            isDoingUltimate = false;
        }

    }

    // -----------------------------
    // ACTIONS
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
            StartCoroutine(DoAttackTiming());
            cooldownTimer = attackCooldown;
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
            return;

        Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
    }

    IEnumerator DoAttackTiming()
    {
        yield return new WaitForSeconds(0.2f);

        if (sword != null)
        {
            sword.SetActive(true);
            Animator anim = swordAnimator != null ? swordAnimator : sword.GetComponent<Animator>();
            if (anim) anim.SetTrigger("swing");
        }

        if (bossHitbox != null)
            bossHitbox.EnableHitbox();

        yield return new WaitForSeconds(0.25f);

        if (bossHitbox != null)
            bossHitbox.DisableHitbox();

        if (sword != null)
            sword.SetActive(false);
    }

    // -----------------------------
    // DAMAGE + PHASE SWITCH
    // -----------------------------
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Phase checks
        if (!inPhase3 && currentHealth <= phase3Threshold)
            inPhase3 = true;
        else if (!inPhase2 && currentHealth <= phase2Threshold)
            inPhase2 = true;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Apply knockback + stun
        StartCoroutine(DoKnockbackAndStun());
    }

    IEnumerator DoKnockbackAndStun()
    {
        isStunned = true;
        currentState = BossState.Hurt;

        Vector3 direction = (transform.position - player.position).normalized;
        direction.y = 0;

        float timer = 0f;
        while (timer < stunDuration)
        {
            transform.position += direction * knockbackForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        isStunned = false;
    }

    public void Die()
    {
        currentState = BossState.Dead;
        Destroy(gameObject);
    }
}
