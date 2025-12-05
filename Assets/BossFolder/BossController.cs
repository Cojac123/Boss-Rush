using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
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
    public float ultimateCooldown = 25f;
    private float ultimateTimer = 0f;
    public GameObject shockwavePrefab;
    public Transform shockwaveSpawnPoint;
    public float ultimateTelegraphTime = 1f;
    private bool isDoingUltimate = false;

    [Header("Phases")]
    public int phase2Threshold = 50;
    public bool inPhase2 = false;
    public int phase3Threshold = 20;
    public bool inPhase3 = false;

    [Header("Hurt Reaction")]
    public float knockbackForce = 8f;
    public float stunDuration = 0.3f;
    private bool isStunned = false;

    private GameManager gm;

    void Start()
    {
        currentHealth = maxHealth;
        ultimateTimer = ultimateCooldown;

        gm = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        cooldownTimer -= Time.deltaTime;
        projectileTimer -= Time.deltaTime;

        if (inPhase3)
        {
            HandlePhase3Behavior(distance);
            return;
        }

        if (inPhase2)
        {
            HandlePhase2Behavior(distance);
            return;
        }

        switch (currentState)
        {
            case BossState.Idle: HandleIdle(distance); break;
            case BossState.Chase: HandleChase(distance); break;
            case BossState.Attack: HandleAttack(distance); break;
            case BossState.Hurt: HandleHurt(); break;
        }
    }

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
    // PHASE 2
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
        transform.LookAt(player.position);
        ultimateTimer -= Time.deltaTime;

        if (!isDoingUltimate && ultimateTimer <= 0f)
        {
            StartCoroutine(DoUltimateAttack());
            ultimateTimer = ultimateCooldown;
            return;
        }

        if (projectileTimer <= 0f)
        {
            ShootProjectile();
            ShootProjectile();
            projectileTimer = projectileCooldown * 0.4f;
        }

        if (distance < attackRange + 1f)
            AttemptAttack();

        if (distance > 5f)
        {
            Vector3 toward = (player.position - transform.position).normalized;
            toward.y = 0;
            transform.position += toward * (moveSpeed * 2f) * Time.deltaTime;
        }
    }

    // -----------------------------
    // ⭐ ULTIMATE ATTACK ⭐
    // -----------------------------
    IEnumerator DoUltimateAttack()
    {
        isDoingUltimate = true;

        yield return new WaitForSeconds(ultimateTelegraphTime);

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

        if (shockwavePrefab != null && shockwaveSpawnPoint != null)
        {
            Instantiate(shockwavePrefab, shockwaveSpawnPoint.position, shockwaveSpawnPoint.rotation);
        }

        yield return new WaitForSeconds(0.5f);

        isDoingUltimate = false;
    }

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
            if (anim != null) anim.SetTrigger("swing");
        }

        if (bossHitbox != null)
            bossHitbox.EnableHitbox();

        yield return new WaitForSeconds(0.25f);

        if (bossHitbox != null)
            bossHitbox.DisableHitbox();

        if (sword != null)
            sword.SetActive(false);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (!inPhase3 && currentHealth <= phase3Threshold)
            inPhase3 = true;
        else if (!inPhase2 && currentHealth <= phase2Threshold)
            inPhase2 = true;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

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
        //Tell game manager to go to next level
        FindObjectOfType<GameManager>().GoToNextLevel();
        gm.GoToNextLevel();
        Destroy(gameObject);
    }
}
