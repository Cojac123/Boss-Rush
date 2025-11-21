using UnityEngine;

public class BossDamageHitbox : MonoBehaviour
{
    public int damageAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        Damageable dmgTarget = other.GetComponent<Damageable>();

        if (dmgTarget != null)
        {
            Debug.Log("Boss hit the player!");

            // PlayerLogic already has a damage system
            Damage dmg = new Damage();
            dmg.amount = damageAmount;
            dmg.knockbackForce = 10;
            dmg.direction = (other.transform.position - transform.position).normalized;

            dmgTarget.Hit(dmg);
        }
    }
}
