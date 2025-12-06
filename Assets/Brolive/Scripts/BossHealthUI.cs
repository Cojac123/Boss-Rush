using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    public BossController boss;   // drag Boss root
    public Bar healthBar;         // drag UI Bar object

    void Start()
    {
        // Set bar maximum ONCE
        healthBar.SetMax(boss.maxHealth);

        // Subscribe to event
        boss.OnBossHealthChanged += UpdateBar;
    }

    void UpdateBar(int current, int max)
    {
        healthBar.UpdateBar(0, current);
    }
}
