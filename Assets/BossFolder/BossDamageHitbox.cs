using UnityEngine;

public class BossDamageHitbox : MonoBehaviour
{
    public int damageAmount = 10;
    BoxCollider hitbox;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider>();
        hitbox.enabled = false; // Start disabled
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
        Debug.Log("Hitbox ENABLED");
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        Debug.Log("Hitbox DISABLED");
    }

    private void OnTriggerEnter(Collider other)
    {
        Damageable dmgTarget = other.GetComponent<Damageable>();

        if (dmgTarget != null)
        {
            Debug.Log("Boss hit the player!");

            Damage dmg = new Damage();
            dmg.amount = damageAmount;
            dmg.knockbackForce = 10;
            dmg.direction = (other.transform.position - transform.position).normalized;

            dmgTarget.Hit(dmg);
        }
    }
}
