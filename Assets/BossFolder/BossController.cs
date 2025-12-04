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
    float cooldownTimer = 0f;

    [Header("Sword")]
    [SerializeField] GameObject sword;
    [SerializeField] Animator swordAnimator;

    // -----------------------------
    // PHASE SYSTEM
    // -----------------------------
    [Header("Phases")]
    public int phase2Threshold = 50;
    public bool inPhase2 = false;

    public int phase3Threshold = 20;
    public bool inPhase3 = false;

    // -----------------------------
    // PROJECTILES (for Phase 2 / normal ranged)
    // -----------------------------
    [Header("Projectile Attack")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileCooldown = 3f;
    float projectileTimer = 0f;

    // -----------------------------
    // ULTIMATE (Phase 3)
    // -----------------------------
    [Header("Ultimate Attack (Phase 3)")]
    public float ultimateCooldown = 25f;      // time between ultimates
    public float ultimateMinDistance = 4f;    // only ult if boss is not too close
    public float ultimateChargeTime = 1.5f;   // telegraph duration
    public GameObject shockwavePrefab;        // ring that comes out
    public Transform shockwaveSpawnPoint;     // where ring spawns (near feet)

    float ultimateTimer = 0f;                 // counts down to next ultimate
    bool isDoingUltimate = false;             // so we don’t stack ultimates

    // -----------------------------
    // HURT / STUN
    // -----------------------------
    [Header("Hurt Reaction")]
    public float knockbackForce = 8f;
    public float stunDuration = 0.3f;
    bool isStunned = false;

    // -----------------------------
    // START
    // -----------------------------
    void Start()
    {
        currentHealth = maxHealth;

        // so you see the first ultimate relatively soon in Phase 3
        ultimateTimer = 5f; // first ult ~5 seconds after entering Phase 3
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
        ultimateTimer -= Time.deltaTime;

        // -----------------------------
        // PHASE 3 — FINAL FORM
        // -----------------------------
        if (inPhase3)
        {
            HandlePhase3Behavior(distance);
            return;
        }

        // -----------------------------
        // PHASE 2 — RANGED / KEEP AWAY
        // -----------------------------
        if (inPhase2)
        {
            HandlePhase2Behavior(distance);
            return;
        }

        // -----------------------------
        // PHASE 1 — NORMAL FSM (MELEE)
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
    // PHASE 1 FUNCTIONS (MELEE)
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

        // Back away if too close
        if (distance < desiredRange)
        {
            Vector3 away = (transform.position - player.position).normalized;
            away.y = 0;
            transform.position += away * (moveSpeed * 1.5f) * Time.deltaTime;
        }
        else
        {
            // At good range → face player and shoot
            transform.LookAt(player.position);
        }

        if (projectileTimer <= 0f)
        {
            ShootProjectile();
            projectileTimer = projectileCooldown * 0.7f;
        }
    }

    // -----------------------------
    // PHASE 3 — MIXED + ULTIMATE
    // -----------------------------
    void HandlePhase3Behavior(float distance)
    {
        // If currently mid-ultimate, let the coroutine drive behavior
        if (isDoingUltimate)
            return;

        // 1) Basic “final form” behavior:
        //    - closes distance a bit more aggressively
        //    - still allowed to melee and maybe occasional shots

        transform.LookAt(player.position);

        // Occasional projectile even in Phase 3 (optional)
        if (projectilePrefab != null && projectileTimer <= 0f)
        {
            ShootProjectile();
            projectileTimer = projectileCooldown; // normal rate again
        }

        // Melee if close
        if (distance < attackRange + 0.5f)
        {
            AttemptAttack();
        }
        else
        {
            // Chase more aggressively than Phase 1
            Vector3 toward = (player.position - transform.position).normalized;
            toward.y = 0;
            transform.position += toward * (moveSpeed * 1.5f) * Time.deltaTime;
        }

        // 2) Check if we can do ULTIMATE
        if (ultimateTimer <= 0f && distance > ultimateMinDistance)
        {
            StartCoroutine(DoUltimateAttack());
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
        // Normal melee (Phase 1 / 2 / 3 light slash)
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
    // ⭐ ULTIMATE ATTACK COROUTINE ⭐
    // -----------------------------
    IEnumerator DoUltimateAttack()
    {
        isDoingUltimate = true;

        // 1) TELEGRAPH — glow / animation / warning
        // -----------------------------------------
        // face the player
        transform.LookAt(player.position);

        // TODO: trigger an "UltimateCharge" animation if you have one
        if (swordAnimator != null)
        {
            swordAnimator.SetTrigger("ultimate_charge");
        }

        // TODO: here later you can:
        // - play hum SFX
        // - start screen vignette
        // - slow down movement / root motion

        yield return new WaitForSeconds(ultimateChargeTime); // charging time

        // 2) HEAVY MELEE SLASH
        // -----------------------------------------
        if (sword != null)
        {
            sword.SetActive(true);
            Animator anim = swordAnimator != null ? swordAnimator : sword.GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("ultimate_slash");  // heavier anim
        }

        if (bossHitbox != null)
            bossHitbox.EnableHitbox();  // big damage handled by damage system

        // keep the hitbox active a bit longer than normal melee
        yield return new WaitForSeconds(0.5f);

        if (bossHitbox != null)
            bossHitbox.DisableHitbox();

        if (sword != null)
            sword.SetActive(false);

        // 3) SHOCKWAVE SPAWN
        // -----------------------------------------
        if (shockwavePrefab != null && shockwaveSpawnPoint != null)
        {
            Instantiate(shockwavePrefab, shockwaveSpawnPoint.position, shockwaveSpawnPoint.rotation);
        }

        // 4) RESET
        // -----------------------------------------
        ultimateTimer = ultimateCooldown;  // wait 25–30s before next ultimate
        isDoingUltimate = false;
    }

    // -----------------------------
    // DAMAGE + PHASE SWITCH
    // -----------------------------
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Phase checks in order: Phase 3 first
        if (!inPhase3 && currentHealth <= phase3Threshold)
        {
            inPhase3 = true;
            Debug.Log("Boss entered Phase 3 (final form)!");
        }
        else if (!inPhase2 && currentHealth <= phase2Threshold)
        {
            inPhase2 = true;
            Debug.Log("Boss entered Phase 2 (ranged)!");
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Apply knockback + stun (same in all phases)
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
