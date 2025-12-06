using System.Collections;
using UnityEngine;

namespace CoryBoss
{
    public class BossController : MonoBehaviour
    {
        public enum BossState { Idle, Chase, Attack, Hurt, Dead }

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
        float attackTimer = 0f;

        [Header("Sword")]
        public GameObject sword;
        public Animator swordAnimator;
        public BossDamageHitbox bossHitbox;

        [Header("Projectile Attack")]
        public GameObject projectilePrefab;
        public Transform projectileSpawnPoint;
        public float projectileCooldown = 3f;
        float projectileTimer = 0f;

        [Header("Ultimate (Laser Beam)")]
        public GameObject laserPrefab;
        public Transform laserSpawnPoint;
        public float ultimateCooldown = 12f;
        public float ultimateTelegraphTime = 1f;
        float ultimateTimer = 0f;
        bool isDoingUltimate = false;

        [Header("Phases")]
        public int phase2Threshold = 50;
        public int phase3Threshold = 20;
        public bool inPhase2 = false;
        public bool inPhase3 = false;

        [Header("Hurt Reaction")]
        public float knockbackForce = 8f;
        public float stunDuration = 0.3f;
        bool isStunned = false;

        [Header("Arena Bounds")]
        public float minX = -3f;
        public float maxX = 3f;
        public float minZ = -3f;
        public float maxZ = 3f;


        GameManager gm;

        public System.Action<int, int> OnBossHealthChanged;

        void Start()
        {
            gm = FindObjectOfType<GameManager>();
            currentHealth = maxHealth;

            ultimateTimer = ultimateCooldown;

            Debug.Log("BOSS STARTED | HP: " + currentHealth);
        }

        void Update()
        {
            if (player == null)
            {
                Debug.LogWarning("Boss has NO PLAYER reference!");
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);

            attackTimer -= Time.deltaTime;
            projectileTimer -= Time.deltaTime;
            ultimateTimer -= Time.deltaTime;

            Debug.Log($"[BOSS] HP={currentHealth} | Phase2={inPhase2} | Phase3={inPhase3} | UltTimer={ultimateTimer}");

            // PRIORITY: Dead → Hurt → Ultimate → Phases → P1
            if (inPhase3) { HandlePhase3(distance); return; }
            if (inPhase2) { HandlePhase2(distance); return; }

            // Phase 1 Logic
            switch (currentState)
            {
                case BossState.Idle: HandleIdle(distance); break;
                case BossState.Chase: HandleChase(distance); break;
                case BossState.Attack: HandleAttack(distance); break;
                case BossState.Hurt: HandleHurt(); break;
            }
        }

        // -----------------------------------------
        // PHASE 1 — BASIC CHASE + MELEE
        // -----------------------------------------

        void HandleIdle(float distance)
        {
            if (distance < 10f)
            {
                Debug.Log("[Phase1] Boss detected player → CHASE");
                currentState = BossState.Chase;
            }
        }

        void HandleChase(float distance)
        {
            // Move toward player
            Vector3 direction = player.position - transform.position;
            direction.y = 0;
            direction.Normalize();

            transform.position += direction * moveSpeed * Time.deltaTime;
            ClampToArena();

            transform.forward = direction;

            if (distance <= attackRange)
            {
                Debug.Log("[Phase1] Boss entering ATTACK");
                currentState = BossState.Attack;
            }
        }

        void HandleAttack(float distance)
        {
            // Rotate during melee attack
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            transform.forward = lookDir.normalized;

            AttemptAttack();

            if (distance > attackRange)
            {
                Debug.Log("[Phase1] Boss leaving ATTACK → CHASE");
                currentState = BossState.Chase;
            }
        }

        void HandleHurt()
        {
            // After stun is done
            if (!isStunned)
                currentState = BossState.Chase;
        }

        // -----------------------------------------
        // PHASE 2 — KEEP DISTANCE + SHOOT
        // -----------------------------------------
        void HandlePhase2(float distance)
        {
            Debug.Log("[PHASE 2] Active");

            float desiredRange = 7f;

            // Move away if the player is too close
            if (distance < desiredRange)
            {
                Vector3 away = transform.position - player.position;
                away.y = 0;
                transform.position += away * (moveSpeed * 1.5f) * Time.deltaTime;
                ClampToArena();

            }
            else
            {
                transform.LookAt(player);
            }

            if (projectileTimer <= 0f)
            {
                ShootProjectile();
                projectileTimer = projectileCooldown * 0.7f;
                Debug.Log("[PHASE 2] Projectile fired");
            }
        }

        // -----------------------------------------
        // PHASE 3 — LASER + RAPID FIRE
        // -----------------------------------------
        void HandlePhase3(float distance)
        {
            Debug.Log("[PHASE 3] Active");
            transform.LookAt(player);

            // ULTIMATE LASER TAKES PRIORITY
            if (!isDoingUltimate && ultimateTimer <= 0f)
            {
                Debug.Log("[PHASE 3] Ultimate triggered!");
                StartCoroutine(DoUltimate());
                ultimateTimer = ultimateCooldown;
                return;
            }

            // No other actions while ulting
            if (isDoingUltimate)
                return;

            // Rapid Fire
            if (projectileTimer <= 0f)
            {
                ShootProjectile();
                ShootProjectile();
                projectileTimer = projectileCooldown * 0.4f;
                Debug.Log("[PHASE 3] Rapid Fire");
            }

            // Melee close
            if (distance < attackRange + 1f)
            {
                Vector3 look = player.position - transform.position;
                look.y = 0;
                transform.forward = look.normalized;

                AttemptAttack();
            }

            // Aggressive chase
            if (distance > 5f)
            {
                Vector3 toward = player.position - transform.position;
                toward.y = 0;
                transform.position += toward * (moveSpeed * 2f) * Time.deltaTime;
                ClampToArena();
            }
        }

        // -----------------------------------------
        // ULTIMATE LASER ATTACK
        // -----------------------------------------
        IEnumerator DoUltimate()
        {
            isDoingUltimate = true;

            Debug.Log("[ULTIMATE] Telegraphing...");
            yield return new WaitForSeconds(ultimateTelegraphTime);

            if (laserPrefab != null && laserSpawnPoint != null)
            {
                Instantiate(laserPrefab, laserSpawnPoint.position, laserSpawnPoint.rotation);
                Debug.Log("[ULTIMATE] LASER FIRED!");
            }
            else
            {
                Debug.LogError("[ULTIMATE] Laser PREFAB or SPAWNPOINT missing!");
            }

            yield return new WaitForSeconds(1f);
            isDoingUltimate = false;
        }

        // -----------------------------------------
        // ATTACK FUNCTIONS
        // -----------------------------------------
        void AttemptAttack()
        {
            if (attackTimer <= 0f)
            {
                Debug.Log("[MELEE] Boss swinging!");
                StartCoroutine(DoAttackTiming());
                attackTimer = attackCooldown;
            }
        }

        IEnumerator DoAttackTiming()
        {
            yield return new WaitForSeconds(0.2f);

            if (bossHitbox != null)
                bossHitbox.EnableHitbox();

            yield return new WaitForSeconds(0.25f);

            if (bossHitbox != null)
                bossHitbox.DisableHitbox();
        }

        void ShootProjectile()
        {
            if (projectilePrefab != null && projectileSpawnPoint != null)
            {
                Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                Debug.Log("[PROJECTILE] Fired!");
            }
        }

        // -----------------------------------------
        // DAMAGE + PHASE CHANGES
        // -----------------------------------------
        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            Debug.Log("[DAMAGE] Boss HP now = " + currentHealth);

            OnBossHealthChanged?.Invoke(currentHealth, maxHealth);

            if (!inPhase3 && currentHealth <= phase3Threshold)
            {
                Debug.Log(">>> ENTERING PHASE 3 <<<");
                inPhase3 = true;
            }
            else if (!inPhase2 && currentHealth <= phase2Threshold)
            {
                Debug.Log(">>> ENTERING PHASE 2 <<<");
                inPhase2 = true;
            }

            if (currentHealth <= 0)
            {
                Die();
                return;
            }
        }

        void Die()
        {
            Debug.Log("BOSS DIED");
            currentState = BossState.Dead;
            gm.GoToNextLevel();
            Destroy(gameObject);
        }
        void ClampToArena()
        {
            Vector3 pos = transform.position;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

            transform.position = pos;
        }


    }
}