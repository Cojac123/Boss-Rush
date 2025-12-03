using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//  Boss behavior states
public enum BossState
{
    Idle,       // Waiting for player
    Pursue,     // Move toward player
    Melee,      // Swing weapon at player
    Dead        // Done
}

public class BossAI : MonoBehaviour
{
    [Header("Boss Settings")]
    public float speed = 3f;
    public Transform player;

    [Header("Components")]
    Navigator navigator;      // Handles pathfinding
    Rigidbody rb;             // Movement physics
    Damageable health;        // Boss health system

    [Header("Combat")]
    public GameObject meleeWeapon;
    bool inMeleeRange = false;

    BossState state = BossState.Idle;
    float stateTimer = 0;
    int pathIndex = 0;

    Vector3 targetVelocity;
    float currentStateElapsed = 0;

    // ---------------------------
    // Setup
    // ---------------------------
    void Start()
    {
        navigator = GetComponent<Navigator>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Damageable>();

        player = FindObjectOfType<PlayerLogic>().transform;

        meleeWeapon.SetActive(false); // hide sword until attack
    }

    // ---------------------------
    // Main Loop
    // ---------------------------
    void Update()
    {
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case BossState.Idle:
                HandleIdle();
                break;

            case BossState.Pursue:
                HandlePursue();
                break;

            case BossState.Melee:
                HandleMelee();
                break;

            case BossState.Dead:
                //HandleDead();
                break;
        }
    }

    // ---------------------------
    // IDLE — Wait until player is close
    // ---------------------------
    void HandleIdle()
    {
        if (Vector3.Distance(transform.position, player.position) < 10f)
        {
            BeginPursue();
        }
    }

    void BeginPursue()
    {
        if (navigator.CalculatePathToPosition(player.position))
        {
            state = BossState.Pursue;
            stateTimer = 0;
            pathIndex = 0;
        }
    }

    // ---------------------------
    // PURSUE — Move along path nodes
    // ---------------------------
    void HandlePursue()
    {
        if (inMeleeRange)
        {
            BeginMelee();
            return;
        }

        if (navigator.PathNodes.Count == 0)
        {
            BeginPursue();
            return;
        }

        Vector3 targetNode = navigator.PathNodes[pathIndex];
        Vector3 moveDir = targetNode - transform.position;
        moveDir.y = 0;
        moveDir.Normalize();

        transform.forward = moveDir;
        rb.linearVelocity = moveDir * speed;

        if (Vector3.Distance(transform.position, targetNode) < 1f)
        {
            pathIndex++;

            if (pathIndex >= navigator.PathNodes.Count)
            {
                BeginPursue(); // recalc path
                return;
            }
        }
    }

    // ---------------------------
    // MELEE — Swing sword using Damager
    // ---------------------------
    void BeginMelee()
    {
        rb.linearVelocity = Vector3.zero;
        transform.forward = (player.position - transform.position);

        meleeWeapon.SetActive(true);
        state = BossState.Melee;
        stateTimer = 0;
    }

    void EnterMelee()
    {
        //Debug.Log("Enter melee");
        // animator.setTrigger("Melee");
        var dirToPlayer = (player.transform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        transform.forward = dirToPlayer;
        targetVelocity = Vector3.zero;
        //state = EnemyStates.melee;
        currentStateElapsed = 0;

        StartCoroutine(HandleMelee());
    }

    IEnumerator HandleMelee()
    {
        // Sword stays active briefly
        if (stateTimer >= 0.4f)
        {
            meleeWeapon.SetActive(true);
            meleeWeapon.GetComponent<Animator>().SetTrigger("swing");
            yield return new WaitForSeconds(0.25f);
            meleeWeapon.SetActive(false);
        }
    }


        // ---------------------------
        // DEATH STATE
        // ---------------------------
        void HandleDead()
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Called by Damageable via UnityEvent
        public void OnDeath()
        {
            state = BossState.Dead;
        }

        // Sensor calls this when player enters melee zone
        public void SetMeleeRange(bool val)
        {
            inMeleeRange = val;
        }
    
}
