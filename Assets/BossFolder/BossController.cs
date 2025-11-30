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

        switch (currentState)
        {
            case BossState.Idle:
                HandleIdle(distance);
                break;

            case BossState.Chase:
                HandleChase(distance);
                break;

            case BossState.Attack:
                HandleAttack(distance);
                break;

            case BossState.Hurt:
                HandleHurt();
                break;

            case BossState.Dead:
                break;
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

    IEnumerator DoAttackTiming()
    {
        //1. Wind-up (optional)
        yield return new WaitForSeconds(0.2f);

        //2. Enable HItbox
        bossHitbox.EnableHitbox();

        //3. Active attack window
        yield return new WaitForSeconds(0.3f);

        //4.Disable Hitbox
        bossHitbox.DisableHitbox();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        Debug.Log("Boss takes " + amount + " damage! Health: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Boss has been defeated!");
        currentState = BossState.Dead;
        Destroy(gameObject);
    }
}
